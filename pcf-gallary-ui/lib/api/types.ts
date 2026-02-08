export interface CreateComponentResponse {
    buildId: string;
    status: string;
    zipDownloadUrl?: string;
}

export interface BuildStatusResponse {
    buildId: string;
    status: string; // "Running" | "Completed" | "Failed"
    error?: string;
    zipPath?: string;
    previewUrl?: string;
}

export type BuildStatus = "Pending" | "Running" | "Completed" | "Failed";
