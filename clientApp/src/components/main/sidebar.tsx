"use client";

import type { FC } from "react";
import {
    Archive,
    Bot,
    Brain,
    Camera,
    Clock,
    Heart,
    HelpCircle,
    Home,
    MessageCircle,
    MessageSquare,
    Mic,
    Music,
    Settings,
    Smile,
    Sparkles,
    Star,
    User,
    Video,
    Volume2,
    Zap,
} from "lucide-react";
import type { NavItemType } from "@/components/application/app-navigation/config";
import { SidebarNavigationSlim } from "@/components/application/app-navigation/sidebar-navigation/sidebar-slim";

const navItemsDualTier: (NavItemType & { icon: FC<{ className?: string }> })[] = [
    {
        label: "Chat",
        href: "/",
        icon: MessageCircle,
        items: [
            { label: "New Conversation", href: "/chat/new", icon: MessageSquare },
            { label: "Recent Chats", href: "/chat/recent", icon: Clock },
            { label: "Favorites", href: "/chat/favorites", icon: Star },
            { label: "Archived", href: "/chat/archived", icon: Archive },
        ],
    },
    {
        label: "Character",
        href: "/character",
        icon: Bot,
        items: [
            { label: "Personality", href: "/character/personality", icon: Brain },
            { label: "Appearance", href: "/character/appearance", icon: Smile },
            { label: "Voice Settings", href: "/character/voice", icon: Mic },
            { label: "Animations", href: "/character/animations", icon: Video },
            { label: "Mood Settings", href: "/character/mood", icon: Heart },
        ],
    },
    {
        label: "Media",
        href: "/media",
        icon: Camera,
        items: [
            { label: "3D Model", href: "/media/model", icon: Video },
            { label: "Animations", href: "/media/animations", icon: Music },
            { label: "Voice Clips", href: "/media/voice", icon: Volume2 },
            { label: "Screenshots", href: "/media/screenshots", icon: Camera },
        ],
    },
    {
        label: "AI Features",
        href: "/ai",
        icon: Sparkles,
        items: [
            { label: "Conversation AI", href: "/ai/chat", icon: Brain },
            { label: "Voice Synthesis", href: "/ai/voice", icon: Mic },
            { label: "Lip Sync", href: "/ai/lipsync", icon: Smile },
            { label: "Emotion AI", href: "/ai/emotion", icon: Heart },
        ],
    },
    {
        label: "Settings",
        href: "/settings",
        icon: Settings,
    },
    {
        label: "Profile",
        href: "/profile",
        icon: User,
    },
];

export const SidebarNavigation = () => (
    <SidebarNavigationSlim
        items={navItemsDualTier}
        // footerItems={[
        //     {
        //         label: "Support",
        //         href: "/support",
        //         icon: LifeBuoy01,
        //     },
        //     {
        //         label: "Settings",
        //         href: "/settings",
        //         icon: Settings01,
        //     },
        // ]}
    />
);
