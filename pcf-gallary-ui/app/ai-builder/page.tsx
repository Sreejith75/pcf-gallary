"use client";

import { useState, useEffect } from "react";
import BuildStatusPanel, { BuildStatus } from "@/components/BuildStatusPanel";
import { createComponent, getBuildStatus, downloadArtifact } from "@/lib/api/componentApi";

export default function AIBuilderPage() {
    const [prompt, setPrompt] = useState("");
    const [isBuilding, setIsBuilding] = useState(false);
    const [status, setStatus] = useState<BuildStatus>("pending");
    const [buildId, setBuildId] = useState<string | undefined>(undefined);
    const [previewUrl, setPreviewUrl] = useState<string | undefined>(undefined);
    const [logs, setLogs] = useState<string[]>([]);

    // Poll for status
    useEffect(() => {
        let pollInterval: NodeJS.Timeout;

        if (isBuilding && buildId) {
            pollInterval = setInterval(async () => {
                try {
                    const buildStatus = await getBuildStatus(buildId);

                    // Map backend status to UI status
                    // Backend: "Running" | "Completed" | "Failed"
                    // UI: "pending" | "running" | "completed" | "failed"
                    let uiStatus: BuildStatus = "running";
                    if (buildStatus.status === "Completed") uiStatus = "completed";
                    if (buildStatus.status === "Failed") uiStatus = "failed";

                    setStatus(uiStatus);

                    // Set Preview URL if available
                    if (buildStatus.previewUrl) {
                        const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5001";
                        // Ensure no double slash
                        const cleanBase = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
                        const cleanPath = buildStatus.previewUrl.startsWith('/') ? buildStatus.previewUrl : `/${buildStatus.previewUrl}`;
                        setPreviewUrl(`${cleanBase}${cleanPath}`);
                    }

                    if (buildStatus.error) {
                        setLogs(prev => [...prev, `Error: ${buildStatus.error}`]);
                    } else if (uiStatus === "completed") {
                        setLogs(prev => {
                            if (!prev.includes("Build completed successfully.")) {
                                return [...prev, "Build completed successfully."];
                            }
                            return prev;
                        });
                    }

                    if (uiStatus === "completed" || uiStatus === "failed") {
                        setIsBuilding(false);
                        clearInterval(pollInterval);
                    }
                } catch (error) {
                    console.error("Polling error:", error);
                    // Don't fail immediately on network blip, but could count errors
                }
            }, 2000);
        }

        return () => {
            if (pollInterval) clearInterval(pollInterval);
        };
    }, [isBuilding, buildId]);

    const handleBuild = async () => {
        if (!prompt.trim()) return;

        setIsBuilding(true);
        setStatus("running");
        setLogs(["Initializing build request...", "Sending prompt to AI engine..."]);
        setBuildId(undefined);
        setPreviewUrl(undefined);

        try {
            const response = await createComponent(prompt);
            setBuildId(response.buildId);
            setLogs(prev => [...prev, `Build started (ID: ${response.buildId})...`]);
            // isBuilding remains true to trigger polling
        } catch (error: any) {
            console.error("Build failed:", error);
            setStatus("failed");
            setLogs(prev => [...prev, `Error: ${error.message || "Failed to start build."}`]);
            setIsBuilding(false);
        }
    };

    const handleDownload = async () => {
        if (!buildId) return;
        try {
            const blob = await downloadArtifact(buildId);
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `component-${buildId}.zip`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
        } catch (error) {
            console.error("Download failed:", error);
            alert("Failed to download artifact. Please try again.");
        }
    };

    return (
        <div className="max-w-4xl mx-auto space-y-8">
            <div>
                <h1 className="text-2xl font-bold tracking-tight text-slate-900">AI Component Builder</h1>
                <p className="text-slate-500 mt-1">Describe the functionality you need and let AI build the PCF component for you.</p>
            </div>

            <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm">
                <label htmlFor="prompt" className="block text-sm font-semibold text-slate-900 mb-2">
                    Component Requirements
                </label>
                <textarea
                    id="prompt"
                    rows={6}
                    className="w-full p-4 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 mb-4 bg-slate-50 resize-none placeholder:text-slate-400 text-slate-900"
                    placeholder="Describe the PCF component you want to build... (e.g., 'A read-only text input that highlights email addresses in blue')"
                    value={prompt}
                    onChange={(e) => setPrompt(e.target.value)}
                    disabled={isBuilding && status === 'running'}
                />

                <div className="flex justify-end">
                    <button
                        onClick={handleBuild}
                        disabled={(isBuilding && status === 'running') || !prompt.trim()}
                        className={`
              inline-flex items-center gap-2 px-6 py-2.5 rounded-lg text-sm font-semibold transition-all
              ${(isBuilding && status === 'running') || !prompt.trim()
                                ? "bg-slate-100 text-slate-400 cursor-not-allowed"
                                : "bg-slate-900 text-white hover:bg-blue-600 shadow-sm hover:shadow-blue-500/20"}
            `}
                    >
                        {isBuilding && status === 'running' ? (
                            <>
                                <span className="animate-spin h-4 w-4 border-2 border-current border-t-transparent rounded-full"></span>
                                Building...
                            </>
                        ) : (
                            <>
                                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m12 3-1.912 5.813a2 2 0 0 1-1.275 1.275L3 12l5.813 1.912a2 2 0 0 1 1.275 1.275L12 21l1.912-5.813a2 2 0 0 1 1.275-1.275L21 12l-5.813-1.912a2 2 0 0 1-1.275-1.275L12 3Z" /></svg>
                                Build Component
                            </>
                        )}
                    </button>
                </div>
            </div>

            <BuildStatusPanel
                status={status}
                buildId={buildId}
                logs={logs}
                componentName={prompt.length > 30 ? prompt.substring(0, 30) + "..." : prompt || "Custom Component"}
                previewUrl={previewUrl}
                onDownload={handleDownload}
            />
        </div>
    );
}
