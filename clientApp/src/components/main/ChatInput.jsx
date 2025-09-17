"use client";
import { useRef, useEffect, useState } from "react";
import { useChat } from "@/hooks/useChat";
import { ButtonUtility } from "@/components/base/buttons/button-utility";
import { BadgeGroup } from "@/components/base/badges/badge-groups";
import { ZoomIn, ZoomOut, Send01, FaceSmile } from "@untitledui/icons";
import { cx } from "@/utils/cx";

const moodOptions = [
    {
        value: "default",
        label: "Default",
        icon: "😐",
        description: "Neutral mood",
        color: "gray",
    },
    {
        value: "happy",
        label: "Happy",
        icon: "😊",
        description: "Feeling cheerful and positive",
        color: "green",
    },
    {
        value: "sad",
        label: "Sad",
        icon: "😢",
        description: "Feeling down or melancholy",
        color: "blue",
    },
    {
        value: "excited",
        label: "Excited",
        icon: "🤩",
        description: "Feeling enthusiastic and energetic",
        color: "yellow",
    },
    {
        value: "romantic",
        label: "Romantic",
        icon: "😍",
        description: "Feeling loving and affectionate",
        color: "pink",
    },
    {
        value: "angry",
        label: "Angry",
        icon: "😠",
        description: "Feeling frustrated or upset",
        color: "red",
    },
    {
        value: "calm",
        label: "Calm",
        icon: "😌",
        description: "Feeling peaceful and relaxed",
        color: "indigo",
    },
    {
        value: "tired",
        label: "Tired",
        icon: "😴",
        description: "Feeling exhausted or sleepy",
        color: "purple",
    },
    {
        value: "confused",
        label: "Confused",
        icon: "😕",
        description: "Feeling uncertain or puzzled",
        color: "orange",
    },
];

export const ChatInput = ({ hidden, ...props }) => {
    const inputRef = useRef();
    const dropdownRef = useRef();
    const { chat, loading, cameraZoomed, setCameraZoomed, message } = useChat();
    const [selectedMood, setSelectedMood] = useState(null);
    const [isMoodOpen, setIsMoodOpen] = useState(false);

    // Debug: Log message structure
    useEffect(() => {
        if (message) {
            console.log("Current message:", message);
        }
    }, [message]);

    // Simple: always position dropdown upward

    // Close dropdown when clicking outside
    useEffect(() => {
        const handleClickOutside = (event) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                setIsMoodOpen(false);
            }
        };

        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    const sendMessage = () => {
        const text = inputRef.current.value;
        if (!loading && !message && text.trim()) {
            chat(text, selectedMood);
            inputRef.current.value = "";
            // setSelectedMood(null);
        }
    };

    const handleKeyDown = (e) => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    };

    const handleMoodSelect = (mood) => {
        if (mood === selectedMood) {
            setSelectedMood(null);
        } else {
            setSelectedMood(mood);
        }
        setIsMoodOpen(false);
    };

    const selectedMoodOption = moodOptions.find((option) => option.value === selectedMood);

    if (hidden) {
        return null;
    }

    return (
        <>
            <div className="fixed top-0 left-0 right-0 bottom-0 z-20 flex justify-between p-4 flex-col pointer-events-none">
                {/* Message display positioned below mobile menu */}
                <div className="self-center mt-15 max-w-md mx-auto">
                    {message ? (
                        <div className="animate-in slide-in-from-top-2 duration-300">
                            <BadgeGroup
                                addonText="Cutypai"
                                color="brand"
                                theme="light"
                                align="leading"
                                size="md"
                                className="w-full"
                                iconTrailing={null}
                            >
                                {message.text}
                            </BadgeGroup>
                        </div>
                    ) : loading ? (
                        <div className="animate-in slide-in-from-top-2 duration-300">
                            <BadgeGroup
                                addonText="Thinking"
                                color="gray"
                                theme="light"
                                align="leading"
                                size="md"
                                className="w-full"
                                iconTrailing={null}
                            >
                                <div className="flex items-center gap-2">
                                    <span className="text-primary text-sm">Cutypai is processing your request...</span>
                                </div>
                            </BadgeGroup>
                        </div>
                    ) : null}
                </div>

                {/* Camera controls */}
                <div className="w-full flex flex-col items-end justify-center gap-4">
                    <ButtonUtility
                        onClick={() => setCameraZoomed(!cameraZoomed)}
                        icon={cameraZoomed ? ZoomOut : ZoomIn}
                        size="md"
                        color="secondary"
                        tooltip={cameraZoomed ? "Zoom out" : "Zoom in"}
                        className="pointer-events-auto hidden lg:flex"
                    />
                </div>

                {/* Integrated Chat Input */}
                <div className="pointer-events-auto max-w-4xl w-full mx-auto">
                    <div className="relative">
                        {/* Main input container */}
                        <div className="relative flex items-center w-full bg-primary border border-primary rounded-2xl shadow-lg focus-within:ring-2 focus-within:ring-brand focus-within:border-brand transition-all duration-200">
                            {/* Mood selector button */}
                            <div className="relative" ref={dropdownRef}>
                                <button
                                    type="button"
                                    onClick={() => !loading && !message && setIsMoodOpen(!isMoodOpen)}
                                    disabled={loading || message}
                                    className={cx(
                                        "flex items-center justify-center w-10 h-10 mx-1 rounded-xl transition-all duration-200",
                                        selectedMoodOption
                                            ? "bg-brand-primary text-brand-primary"
                                            : "text-quaternary hover:text-tertiary hover:bg-secondary",
                                        (loading || message) && "opacity-50 cursor-not-allowed"
                                    )}
                                >
                                    {selectedMoodOption ? (
                                        <span className="text-lg">{selectedMoodOption.icon}</span>
                                    ) : (
                                        <FaceSmile className="w-5 h-5" />
                                    )}
                                </button>

                                {/* Mood dropdown */}
                                {isMoodOpen && (
                                    <div
                                        className="absolute z-50 w-80 bottom-full mb-2 rounded-xl border border-primary bg-primary shadow-lg"
                                    >
                                        <div className="max-h-60 overflow-y-auto py-2">
                                            {/* Clear selection */}
                                            <button
                                                type="button"
                                                onClick={() => handleMoodSelect("")}
                                                className="w-full px-4 py-3 text-left text-sm text-quaternary hover:bg-secondary flex items-center space-x-3"
                                            >
                                                <span className="text-lg">🚫</span>
                                                <span>No mood</span>
                                            </button>

                                            {/* Mood options */}
                                            {moodOptions.map((option) => {
                                                const isSelected = selectedMood === option.value;
                                                return (
                                                    <button
                                                        key={option.value}
                                                        type="button"
                                                        onClick={() => handleMoodSelect(option.value)}
                                                    className={cx(
                                                        "w-full px-4 py-3 text-left text-sm transition-colors duration-200 flex items-center space-x-3",
                                                        isSelected
                                                            ? "bg-brand-primary text-brand-primary"
                                                            : "text-primary hover:bg-secondary"
                                                    )}
                                                    >
                                                        <span className="text-lg">{option.icon}</span>
                                                        <div className="flex-1">
                                                            <div className="font-medium">{option.label}</div>
                                                            <div className="text-xs opacity-75">{option.description}</div>
                                                        </div>
                                                        {isSelected && (
                                                            <div className="w-2 h-2 rounded-full bg-brand-solid"></div>
                                                        )}
                                                    </button>
                                                );
                                            })}
                                        </div>
                                    </div>
                                )}
                            </div>

                            {/* Text input */}
                            <div className="flex-1 px-4 py-3">
                                <input
                                    ref={inputRef}
                                    type="text"
                                    placeholder="Ask anything..."
                                    disabled={loading || message}
                                    onKeyDown={handleKeyDown}
                                    className="w-full bg-transparent text-primary placeholder-placeholder outline-none text-base resize-none"
                                />
                            </div>

                            {/* Send button */}
                            <div className="p-2">
                                <button
                                    type="button"
                                    onClick={sendMessage}
                                    disabled={loading || message}
                                    className={cx(
                                        "flex items-center justify-center w-10 h-10 rounded-xl transition-all duration-200",
                                        loading || message
                                            ? "bg-disabled text-disabled cursor-not-allowed"
                                            : "bg-brand-solid hover:bg-brand-solid_hover text-primary_on-brand shadow-sm hover:shadow-md"
                                    )}
                                >
                                    {loading ? (
                                        <div className="w-4 h-4 border-2 border-disabled border-t-transparent rounded-full animate-spin"></div>
                                    ) : (
                                        <Send01 className="w-5 h-5" />
                                    )}
                                </button>
                            </div>
                        </div>

                        {/* Selected mood indicator */}
                        {/* {selectedMoodOption && (
                            <div className="absolute -top-10 left-4 bg-brand-primary text-brand-primary px-3 py-1 rounded-full text-sm font-medium flex items-center space-x-2">
                                <span className="text-lg">{selectedMoodOption.icon}</span>
                                <span>{selectedMoodOption.label}</span>
                            </div>
                        )} */}
                    </div>
                </div>
            </div>
        </>
    );
};
