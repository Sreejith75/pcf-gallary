// import Link from "next/link";

interface ComponentCardProps {
    name: string;
    description: string;
    tags: string[];
    version?: string;
    downloadUrl?: string; // Optional for now
}

export default function ComponentCard({ name, description, tags, version, downloadUrl }: ComponentCardProps) {
    // Use downloadUrl to suppress warning, or just keep it for future
    const _downloadAction = downloadUrl ? "download" : "view";

    return (
        <div className="group bg-white border border-border rounded-xl shadow-[0_2px_8px_rgba(0,0,0,0.04)] hover:shadow-[0_8px_24px_rgba(0,0,0,0.08)] hover:border-blue-200/60 transition-all duration-300 p-6 flex flex-col h-full relative overflow-hidden">
            {/* Top Gradient Decorative Line */}
            <div className="absolute top-0 left-0 w-full h-1 bg-gradient-to-r from-blue-500 to-indigo-600 opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>

            <div className="flex justify-between items-start mb-3">
                <h3 className="font-bold text-[17px] text-slate-900 leading-tight group-hover:text-blue-700 transition-colors">
                    {name}
                </h3>
                {version && (
                    <span className="text-[10px] font-mono bg-slate-100 text-slate-500 px-2 py-1 rounded-md border border-slate-200">
                        v{version}
                    </span>
                )}
            </div>

            <p className="text-sm text-slate-600 mb-5 flex-grow line-clamp-3 leading-relaxed">
                {description}
            </p>

            <div className="flex flex-wrap gap-2 mb-6">
                {tags.map((tag) => (
                    <span
                        key={tag}
                        className="text-[11px] font-medium px-2.5 py-1 rounded-full bg-slate-50 text-slate-600 border border-slate-200"
                    >
                        {tag}
                    </span>
                ))}
            </div>

            <div className="mt-auto pt-5 border-t border-slate-100 flex justify-between items-center">
                <button className="text-xs font-semibold text-slate-500 hover:text-blue-600 transition-colors uppercase tracking-wide">
                    View Details
                </button>

                <button className="bg-slate-900 text-white hover:bg-blue-600 px-4 py-2 rounded-lg text-xs font-semibold transition-all shadow-sm hover:shadow-blue-500/20 flex items-center gap-2 transform active:scale-95">
                    <svg
                        xmlns="http://www.w3.org/2000/svg"
                        width="14"
                        height="14"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                    >
                        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                        <polyline points="7 10 12 15 17 10" />
                        <line x1="12" x2="12" y1="15" y2="3" />
                    </svg>
                    Download
                </button>
            </div>
        </div>
    );
}
