"use client";

import { PropsWithChildren, useEffect, useState } from "react";
import MainProvider from "@/components/main/Provider";
import { SidebarNavigation } from "@/components/main/sidebar-navigation/sidebar";
import { useAppDispatch, useAppSelector } from "@/hooks/use-redux";
import { me, refreshToken } from "@/redux-store";

function MainLayoutContent({ children }: PropsWithChildren) {
    const [isLoading, setIsLoading] = useState(true);
    const dispatch = useAppDispatch();
    const { accessToken, user, loading } = useAppSelector((state) => state.cutypai);

    useEffect(() => {
        const initializeApp = async () => {
            try {
                // Try to refresh token first to get a valid access token
                const refreshResult = await dispatch(refreshToken());

                // If refresh was successful, wait a bit and then get user data
                if (refreshToken.fulfilled.match(refreshResult)) {
                    await new Promise((resolve) => setTimeout(resolve, 500));
                    await dispatch(me());
                }
            } catch (error) {
                console.error("Failed to initialize app:", error);
                // Continue with loading even if auth fails - user might need to login
            }
        };

        // Start initialization immediately
        initializeApp();

        // Set minimum loading time of 4 seconds
        const timer = setTimeout(() => {
            setIsLoading(false);
        }, 4000);

        return () => clearTimeout(timer);
    }, [dispatch]);

    if (isLoading) {
        return (
            <div className="flex min-h-screen items-center justify-center">
                <div className="text-center">
                    <div className="mx-auto mb-4 h-12 w-12 animate-spin rounded-full border-b-2 border-blue-600"></div>
                    <p className="mb-2 text-gray-600">Initializing application...</p>
                    <p className="text-sm text-gray-500">{loading ? "Loading user data..." : "Setting up your workspace..."}</p>
                </div>
            </div>
        );
    }

    return (
        <>
            <SidebarNavigation />
            {children}
        </>
    );
}

export default function MainLayout({ children }: PropsWithChildren) {
    return (
        <MainProvider>
            <MainLayoutContent>{children}</MainLayoutContent>
        </MainProvider>
    );
}
