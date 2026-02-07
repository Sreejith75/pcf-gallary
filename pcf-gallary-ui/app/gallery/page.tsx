import ComponentCard from "@/components/ComponentCard";

// Mock data for display - normally fetched from API
const SAMPLE_COMPONENTS = [
    {
        id: "1",
        name: "Fluent Date Picker",
        description: "A comprehensive date picker control following Fluent UI design principles with support for range selection and localization.",
        tags: ["Input", "Fluent UI", "Date"],
        version: "1.2.0"
    },
    {
        id: "2",
        name: "Advanced Grid",
        description: "High-performance data grid with sorting, filtering, and editable cells. Supports large datasets.",
        tags: ["Display", "Grid", "Data"],
        version: "2.0.1"
    },
    {
        id: "3",
        name: "Linear Gauge",
        description: "Visualizes numeric values on a linear scale with color-coded ranges for easy status monitoring.",
        tags: ["Visualization", "Chart"],
        version: "1.0.5"
    },
    {
        id: "4",
        name: "Rich Text Editor",
        description: "WYSIWYG editor for PCF with support for basic formatting, images, and embedding.",
        tags: ["Input", "Editor"],
        version: "1.1.0"
    },
    {
        id: "5",
        name: "Barcode Scanner",
        description: "Uses device camera to scan barcodes and QR codes directly into fields.",
        tags: ["Device", "Input"],
        version: "1.0.0"
    },
    {
        id: "6",
        name: "Kanban Board",
        description: "Drag-and-drop Kanban board for managing tasks and status transitions.",
        tags: ["Layout", "DragDrop"],
        version: "0.9.5"
    }
];

export default function GalleryPage() {
    return (
        <div className="space-y-6">
            <div className="flex justify-between items-start">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight text-slate-900">Component Gallery</h1>
                    <p className="text-slate-500 mt-1">Browse and download production-ready PCF components.</p>
                </div>

                {/* Search Filter Component */}
                <div className="relative w-96">
                    <input
                        type="text"
                        placeholder="Search components..."
                        className="w-full pl-11 pr-4 py-2.5 bg-white border border-slate-200 rounded-lg text-sm shadow-[0_1px_2px_rgba(0,0,0,0.05)] focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all placeholder:text-slate-400 text-slate-700"
                    />
                    <svg
                        className="absolute left-3.5 top-3 text-slate-400"
                        xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
                    >
                        <circle cx="11" cy="11" r="8" /><line x1="21" x2="16.65" y1="21" y2="16.65" />
                    </svg>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
                {SAMPLE_COMPONENTS.map((comp) => (
                    <ComponentCard
                        key={comp.id}
                        name={comp.name}
                        description={comp.description}
                        tags={comp.tags}
                        version={comp.version}
                    />
                ))}
            </div>
        </div >
    );
}
