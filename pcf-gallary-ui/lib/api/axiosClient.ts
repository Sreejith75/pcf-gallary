import axios from "axios";

export const axiosClient = axios.create({
    baseURL: process.env.NEXT_PUBLIC_API_BASE_URL,
    headers: {
        "Content-Type": "application/json",
    },
    timeout: 60000,
});

axiosClient.interceptors.response.use(
    (response) => response,
    (error) => {
        // Normalize backend errors
        if (error.response) {
            // The backend might return a message in diverse formats, 
            // but we try to extract a standard message or fall back to status text.
            throw new Error(error.response.data?.message || error.response.data || error.message || "Server error");
        }
        throw new Error(error.message || "Network error");
    }
);
