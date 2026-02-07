"use client";

// import { useEffect, useState } from "react";

export type BuildStatus = "pending" | "running" | "completed" | "failed";

interface BuildStatusPanelProps {
    status: BuildStatus;
    buildId?: string;
    logs?: string[];
    componentName?: string;
    onDownload?: () => void;
}

export default function BuildStatusPanel({
    status,
    buildId,
    logs = [],
    componentName,
    onDownload
}: BuildStatusPanelProps) {

    if (status === "pending") return null;

    return (
        <div className="bg-white border border-border rounded-lg shadow-sm overflow-hidden mt-6">
            <div className="bg-muted/50 px-4 py-3 border-b border-border flex justify-between items-center">
                <h3 className="font-medium text-sm text-foreground flex items-center gap-2">
                    {status === "running" && <span className="animate-spin h-3 w-3 border-2 border-primary border-t-transparent rounded-full"></span>}
                    {status === "completed" && <span className="text-green-600">✓</span>}
                    {status === "failed" && <span className="text-red-600">✗</span>}

                    Build Status: <span className="capitalize">{status}</span>
                </h3>
                {buildId && <span className="text-xs font-mono text-muted-foreground">ID: {buildId}</span>}
            </div>

            <div className="p-4">
                {status === "running" && (
                    <div className="space-y-3">
                        <div className="h-1.5 w-full bg-muted rounded-full overflow-hidden">
                            <div className="h-full bg-accent animate-pulse w-2/3"></div>
                        </div>
                        <p className="text-sm text-muted-foreground">AI is generating your component specs and code...</p>
                    </div>
                )}

                {status === "completed" && (
                    <div className="text-center py-6">
                        <div className="h-12 w-12 bg-green-100 text-green-600 rounded-full flex items-center justify-center mx-auto mb-3">
                            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polyline points="20 6 9 17 4 12" /></svg>
                        </div>
                        <h4 className="text-lg font-medium text-foreground mb-1">Build Successful!</h4>
                        <p className="text-muted-foreground text-sm mb-4">
                            Your component <strong>{componentName || "Untitled"}</strong> is ready.
                        </p>
                        <button
                            onClick={onDownload}
                            className="bg-primary text-primary-foreground hover:bg-primary/90 px-4 py-2 rounded-md text-sm font-medium transition-colors inline-flex items-center gap-2"
                        >
                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" /><polyline points="7 10 12 15 17 10" /><line x1="12" x2="12" y1="15" y2="3" /></svg>
                            Download ZIP
                        </button>
                    </div>
                )}

                {status === "failed" && (
                    <div className="text-center py-6">
                        <div className="h-12 w-12 bg-red-100 text-red-600 rounded-full flex items-center justify-center mx-auto mb-3">
                            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10" /><line x1="12" x2="12" y1="8" y2="12" /><line x1="12" x2="12.01" y1="16" y2="16" /></svg>
                        </div>
                        <h4 className="text-lg font-medium text-foreground mb-1">Build Failed</h4>
                        <p className="text-muted-foreground text-sm">
                            Something went wrong during the generation process. Please try again.
                        </p>
                    </div>
                )}

                {/* Logs Console (Optional, good for "Advanced" feel without being too technical) */}
                {logs.length > 0 && (
                    <div className="mt-6 bg-slate-950 rounded-md p-3 font-mono text-xs text-slate-300 max-h-32 overflow-y-auto">
                        {logs.map((log, i) => (
                            <div key={i} className="mb-1 border-l-2 border-slate-700 pl-2">
                                <span className="text-slate-500 mr-2">{new Date().toLocaleTimeString()}</span>
                                {log}
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}
