import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import axios, { AxiosRequestConfig } from "axios";

const API_BASE = process.env.NEXT_PUBLIC_API_BASE;

// Global store reference for API client
let globalStore: { getState: () => { cutypai: CutypaiState }; dispatch: any } | null = null;

// Set global store reference
export const setGlobalStore = (store: { getState: () => { cutypai: CutypaiState }; dispatch: any }) => {
    globalStore = store;
};

// Modular API client with automatic token refresh
export const api = {
    get: async (url: string, config?: AxiosRequestConfig) => {
        return withTokenRefresh((token: string) => 
            axios.get(`${API_BASE}${url}`, {
                ...config,
                headers: { ...config?.headers, Authorization: `Bearer ${token}` }
            })
        )();
    },
    post: async (url: string, data?: any, config?: AxiosRequestConfig) => {
        return withTokenRefresh((token: string) => 
            axios.post(`${API_BASE}${url}`, data, {
                ...config,
                headers: { ...config?.headers, Authorization: `Bearer ${token}` }
            })
        )();
    },
    put: async (url: string, data?: any, config?: AxiosRequestConfig) => {
        return withTokenRefresh((token: string) => 
            axios.put(`${API_BASE}${url}`, data, {
                ...config,
                headers: { ...config?.headers, Authorization: `Bearer ${token}` }
            })
        )();
    },
    patch: async (url: string, data?: any, config?: AxiosRequestConfig) => {
        return withTokenRefresh((token: string) => 
            axios.patch(`${API_BASE}${url}`, data, {
                ...config,
                headers: { ...config?.headers, Authorization: `Bearer ${token}` }
            })
        )();
    },
    delete: async (url: string, config?: AxiosRequestConfig) => {
        return withTokenRefresh((token: string) => 
            axios.delete(`${API_BASE}${url}`, {
                ...config,
                headers: { ...config?.headers, Authorization: `Bearer ${token}` }
            })
        )();
    }
};

// Modular token refresh fallback system
export const withTokenRefresh = <T>(
    apiCall: (token: string) => Promise<T>
) => {
    return async (): Promise<T> => {
        if (!globalStore) {
            throw new Error("Global store not initialized. Call setGlobalStore() first.");
        }
        
        const { getState, dispatch } = globalStore;
        const state = getState();
        
        // Helper function to make the API call
        const makeApiCall = async (token: string) => {
            return await apiCall(token);
        };

        // If no access token, or the token is expired, refresh first
        if (
            !state.cutypai.accessToken ||
            (state.cutypai.expiresAt && new Date(state.cutypai.expiresAt).getTime() <= Date.now() - 15 * 60 * 1000)
        ) {
            const refreshResult = await dispatch(refreshToken());
            if (refreshToken.fulfilled.match(refreshResult)) {
                const newState = getState();
                return makeApiCall(newState.cutypai.accessToken!);
            } else {
                throw new Error("Failed to refresh token");
            }
        }

        try {
            // First attempt with current token
            return await makeApiCall(state.cutypai.accessToken);
        } catch (error: any) {
            // If unauthorized (401), try to refresh token and retry once
            if (error.response?.status === 401) {
                const refreshResult = await dispatch(refreshToken());
                if (refreshToken.fulfilled.match(refreshResult)) {
                    const newState = getState();
                    return await makeApiCall(newState.cutypai.accessToken!);
                } else {
                    throw new Error("Failed to refresh token after unauthorized response");
                }
            }
            throw error;
        }
    };
};

interface User {
    id: string;
    name: string;
    email: string;
    role: string;
    status: string;
    avatar_url: string;
    created_at: string;
    last_login: string;
    preferences: string;
}

interface CutypaiState {
    loading: boolean;
    error: string | null;
    accessToken: string | null;
    expiresAt: string | null;
    user: User | null;
}

const initialState: CutypaiState = {
    loading: false,
    error: null,
    accessToken: null,
    expiresAt: null,
    user: null,
};

export const cutypai = createSlice({
    name: "cutypai",
    initialState,
    reducers: {
        setAccessToken: (state, action) => {
            state.accessToken = action.payload;
        },
        setUser: (state, action) => {
            state.user = action.payload;
        },
        clearError: (state) => {
            state.error = null;
        },
    },
    extraReducers: (builder) => {
        builder
        // login
            .addCase(login.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(login.fulfilled, (state, action) => {
                state.loading = false;
                state.accessToken = action.payload.accessToken;
                state.expiresAt = action.payload.expiresAtUtc;
            })
            .addCase(login.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || "An error occurred";
            })

        // refresh token
            .addCase(refreshToken.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(refreshToken.fulfilled, (state, action) => {
                state.loading = false;
                state.accessToken = action.payload.accessToken;
                state.expiresAt = action.payload.expiresAtUtc;
            })
            .addCase(refreshToken.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || "An error occurred";
                state.accessToken = null;
                state.expiresAt = null;
                state.user = null;
            })

        // revoke current token
            .addCase(revokeCurrentToken.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(revokeCurrentToken.fulfilled, (state, action) => {
                state.loading = false;
                state.error = null;
                state.accessToken = null;
                state.expiresAt = null;
                state.user = null;
            })
            .addCase(revokeCurrentToken.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || "An error occurred";
                state.accessToken = null;
                state.expiresAt = null;
                state.user = null;
            })

        // me
            .addCase(me.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(me.fulfilled, (state, action) => {
                state.loading = false;
                state.error = null;
                state.user = action.payload;
            })
            .addCase(me.rejected, (state, action) => {
                state.loading = false;
                state.error = action.error.message || "An error occurred";
            });
    },
});

export const { setAccessToken, setUser, clearError } = cutypai.actions;

export const login = createAsyncThunk("data/login", async () => {
    const response = await axios.post(
        `${API_BASE}/auth/login`,
        {
            Email: "alamin.cse.20220104154@aust.edu",
            Password: "LxYL7vlF2O3WRbsJ!",
        },
        { withCredentials: true },
    );
    return response.data;
});

export const refreshToken = createAsyncThunk("data/refreshToken", async () => {

    const response = await axios.post(`${API_BASE}/auth/refresh`, {},
        { withCredentials: true },
    );
    return response.data;
});

export const revokeCurrentToken = createAsyncThunk("data/revokeCurrentToken", async () => {
    const response = await api.post("/auth/revoke", {},
        { withCredentials: true },
    );
    return response.data;
});

export const revokeAllTokens = createAsyncThunk("data/revokeAllTokens", async () => {
    const response = await api.post("/auth/revoke-all", {},
        { withCredentials: true },
    );
    return response.data;
});

export const me = createAsyncThunk("data/me", async () => {
    const response = await api.get("/auth/me");
    return response.data;
});

export default cutypai.reducer;
