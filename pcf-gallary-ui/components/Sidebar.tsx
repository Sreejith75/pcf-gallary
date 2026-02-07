"use client";

import Link from "next/link";
import Image from "next/image";
import { usePathname } from "next/navigation";

export default function Sidebar() {
    const pathname = usePathname();

    const isActive = (path: string) => pathname === path;

    return (
        <aside className="w-64 bg-sidebar border-r border-sidebar-border h-screen flex flex-col fixed left-0 top-0 overflow-y-auto z-10 transition-colors">
            {/* Branding Section */}
            <div className="p-6 pb-4">
                <div className="flex items-center gap-3 mb-1">
                    {/* Logo - mixed mode: Bottom Layer (Color) + Top Layer (White Text Clipped) */}
                    <div className="relative h-10 w-full max-w-[180px]">
                        {/* Bottom: Original Color Logo (Background) - Shows the Icon */}
                        <Image
                            src="/logo/bytestrone.png"
                            alt="Bytestrone Logo"
                            fill
                            className="object-contain object-left"
                            priority
                        />
                        {/* Top: White Text Overlay - Clipped to show text only, hiding the white icon */}
                        <Image
                            src="/logo/bytestrone.png"
                            alt=""
                            fill
                            className="object-contain object-left brightness-0 invert"
                            style={{ clipPath: "polygon(0 0, 84% 0, 84% 100%, 0 100%)" }}
                            priority
                        />
                    </div>
                </div>
            </div>

            {/* Navigation Links */}
            <nav className="flex-1 px-3 py-6 space-y-2">
                <div>
                    <h2 className="px-3 mb-3 text-[11px] font-bold text-sidebar-foreground/50 uppercase tracking-widest">
                        Platform
                    </h2>
                    <div className="space-y-1">
                        <Link
                            href="/gallery"
                            className={`flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-all duration-200 ${isActive("/gallery")
                                ? "bg-sidebar-active text-sidebar-active-foreground shadow-sm"
                                : "text-sidebar-foreground hover:bg-sidebar-foreground/10 hover:text-white"
                                }`}
                        >
                            {/* Grid/Gallery Icon */}
                            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="opacity-90">
                                <rect width="7" height="7" x="3" y="3" rx="1" />
                                <rect width="7" height="7" x="14" y="3" rx="1" />
                                <rect width="7" height="7" x="14" y="14" rx="1" />
                                <rect width="7" height="7" x="3" y="14" rx="1" />
                            </svg>
                            Component Gallery
                        </Link>

                        <Link
                            href="/ai-builder"
                            className={`flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-all duration-200 ${isActive("/ai-builder")
                                ? "bg-sidebar-active text-sidebar-active-foreground shadow-sm"
                                : "text-sidebar-foreground hover:bg-sidebar-foreground/10 hover:text-white"
                                }`}
                        >
                            {/* Sparkles/Stars Icon */}
                            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="opacity-90">
                                <path d="m12 3-1.912 5.813a2 2 0 0 1-1.275 1.275L3 12l5.813 1.912a2 2 0 0 1 1.275 1.275L12 21l1.912-5.813a2 2 0 0 1 1.275-1.275L21 12l-5.813-1.912a2 2 0 0 1-1.275-1.275L12 3Z" />
                            </svg>
                            AI Component Builder
                        </Link>
                    </div>
                </div>
            </nav>

            {/* Footer User Profile */}
            <div className="p-4 border-t border-sidebar-border mt-auto">
                <div className="flex items-center gap-3 px-2">
                    <div className="h-8 w-8 rounded-full bg-sidebar-active flex items-center justify-center text-xs font-bold text-white ring-2 ring-sidebar-bg">
                        JD
                    </div>
                    <div className="flex flex-col">
                        <span className="text-sm font-medium text-white">John Doe</span>
                        <span className="text-[10px] text-sidebar-foreground">Admin Workspace</span>
                    </div>
                </div>
            </div>
        </aside>
    );
}
