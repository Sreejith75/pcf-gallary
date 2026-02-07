import { axiosClient } from "./axiosClient";
import {
    CreateComponentResponse,
    BuildStatusResponse,
} from "./types";

export async function createComponent(prompt: string): Promise<CreateComponentResponse> {
    const response = await axiosClient.post("/api/components", { prompt });
    return response.data;
}

export async function getBuildStatus(buildId: string): Promise<BuildStatusResponse> {
    const response = await axiosClient.get(`/api/components/${buildId}`);
    return response.data;
}

export async function downloadArtifact(buildId: string): Promise<Blob> {
    const response = await axiosClient.get(
        `/api/components/${buildId}/download`,
        { responseType: "blob" }
    );
    return response.data;
}
