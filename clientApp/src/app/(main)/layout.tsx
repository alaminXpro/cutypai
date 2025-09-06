"use client";

import { PropsWithChildren } from "react";
import MainProvider from "@/components/main/Provider";
import { SidebarNavigation } from "@/components/main/sidebar-navigation/sidebar";

export default function MainLayout({ children }: PropsWithChildren) {
    return (
        <MainProvider>
            <SidebarNavigation />
            {children}
        </MainProvider>
    );
}
