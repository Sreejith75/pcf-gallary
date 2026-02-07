import Sidebar from "@/components/Sidebar";
import PageHeader from "@/components/PageHeader";
import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "AppWeaver - PCF Gallery",
  description: "Bytestrone PCF Component Gallery",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased bg-slate-50 text-foreground min-h-screen`}
      >
        <Sidebar />
        <div className="pl-64 flex flex-col min-h-screen">
          <PageHeader />
          <main className="flex-1 px-8 pb-8">
            {children}
          </main>
        </div>
      </body>
    </html>
  );
}
