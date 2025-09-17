"use client";

import { useEffect, useRef, useState } from "react";
import { ChevronDown } from "@untitledui/icons";

export interface MoodOption {
    value: string;
    label: string;
    icon: string;
    description: string;
    color: string;
}

const moodOptions: MoodOption[] = [
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

interface MoodSelectorProps {
    selectedMood: string | null;
    onMoodChange: (mood: string | null) => void;
    disabled?: boolean;
    className?: string;
}

export const MoodSelector = ({ selectedMood, onMoodChange, disabled = false, className = "" }: MoodSelectorProps) => {
    const [isOpen, setIsOpen] = useState(false);
    const [dropdownDirection, setDropdownDirection] = useState<"down" | "up">("down");
    const dropdownRef = useRef<HTMLDivElement>(null);
    const triggerRef = useRef<HTMLButtonElement>(null);

    const selectedOption = moodOptions.find((option) => option.value === selectedMood);

    // Calculate dropdown direction and close dropdown when clicking outside
    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
                setIsOpen(false);
            }
        };

        const calculateDropdownDirection = () => {
            if (triggerRef.current) {
                const triggerRect = triggerRef.current.getBoundingClientRect();
                const viewportHeight = window.innerHeight;
                const dropdownHeight = 240; // Approximate height of dropdown with all options
                const spaceBelow = viewportHeight - triggerRect.bottom;
                const spaceAbove = triggerRect.top;

                // If there's not enough space below but enough space above, show upward
                if (spaceBelow < dropdownHeight && spaceAbove > dropdownHeight) {
                    setDropdownDirection("up");
                } else {
                    setDropdownDirection("down");
                }
            }
        };

        if (isOpen) {
            calculateDropdownDirection();
        }

        // Recalculate on window resize
        window.addEventListener("resize", calculateDropdownDirection);

        document.addEventListener("mousedown", handleClickOutside);
        return () => {
            document.removeEventListener("mousedown", handleClickOutside);
            window.removeEventListener("resize", calculateDropdownDirection);
        };
    }, [isOpen]);

    const handleMoodSelect = (mood: string) => {
        if (mood === selectedMood) {
            // If clicking the same mood, deselect it
            onMoodChange(null);
        } else {
            onMoodChange(mood);
        }
        setIsOpen(false);
    };

    const getColorClasses = (color: string, isSelected: boolean = false) => {
        const baseClasses = "transition-colors duration-200";

        if (isSelected) {
            switch (color) {
                case "green":
                    return `${baseClasses} bg-green-50 border-green-200 text-green-700 dark:bg-green-900/20 dark:border-green-800 dark:text-green-300`;
                case "blue":
                    return `${baseClasses} bg-blue-50 border-blue-200 text-blue-700 dark:bg-blue-900/20 dark:border-blue-800 dark:text-blue-300`;
                case "yellow":
                    return `${baseClasses} bg-yellow-50 border-yellow-200 text-yellow-700 dark:bg-yellow-900/20 dark:border-yellow-800 dark:text-yellow-300`;
                case "pink":
                    return `${baseClasses} bg-pink-50 border-pink-200 text-pink-700 dark:bg-pink-900/20 dark:border-pink-800 dark:text-pink-300`;
                case "red":
                    return `${baseClasses} bg-red-50 border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300`;
                case "indigo":
                    return `${baseClasses} bg-indigo-50 border-indigo-200 text-indigo-700 dark:bg-indigo-900/20 dark:border-indigo-800 dark:text-indigo-300`;
                case "purple":
                    return `${baseClasses} bg-purple-50 border-purple-200 text-purple-700 dark:bg-purple-900/20 dark:border-purple-800 dark:text-purple-300`;
                case "orange":
                    return `${baseClasses} bg-orange-50 border-orange-200 text-orange-700 dark:bg-orange-900/20 dark:border-orange-800 dark:text-orange-300`;
                default:
                    return `${baseClasses} bg-gray-50 border-gray-200 text-gray-700 dark:bg-gray-900/20 dark:border-gray-800 dark:text-gray-300`;
            }
        } else {
            return `${baseClasses} bg-white border-gray-200 text-gray-700 hover:bg-gray-50 dark:bg-gray-800 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700`;
        }
    };

    return (
        <div className={`relative ${className}`} ref={dropdownRef}>
            {/* Trigger Button */}
            <button
                ref={triggerRef}
                type="button"
                onClick={() => !disabled && setIsOpen(!isOpen)}
                disabled={disabled}
                className={`relative w-full min-w-[120px] rounded-lg border border-gray-200 bg-white px-3 py-2 text-left shadow-sm focus:border-blue-500 focus:ring-2 focus:ring-blue-500 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50 dark:border-gray-600 dark:bg-gray-800 dark:text-white ${selectedOption ? getColorClasses(selectedOption.color, true) : "hover:bg-gray-50 dark:hover:bg-gray-700"} `}
            >
                <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-2">
                        {selectedOption ? (
                            <>
                                <span className="text-lg">{selectedOption.icon}</span>
                                <span className="text-sm font-medium">{selectedOption.label}</span>
                            </>
                        ) : (
                            <span className="text-sm text-gray-500 dark:text-gray-400">Select mood</span>
                        )}
                    </div>
                    <ChevronDown
                        className={`h-4 w-4 text-gray-400 transition-transform duration-200 ${
                            isOpen ? (dropdownDirection === "up" ? "rotate-0" : "rotate-180") : ""
                        }`}
                    />
                </div>
            </button>

            {/* Dropdown Menu */}
            {isOpen && (
                <div
                    className={`absolute z-50 w-full rounded-lg border border-gray-200 bg-white shadow-lg dark:border-gray-600 dark:bg-gray-800 ${
                        dropdownDirection === "up" ? "bottom-full mb-1" : "top-full mt-1"
                    }`}
                >
                    <div className="max-h-60 overflow-y-auto py-1">
                        {/* Clear Selection Option */}
                        <button
                            type="button"
                            onClick={() => handleMoodSelect("")}
                            className="w-full px-3 py-2 text-left text-sm text-gray-500 hover:bg-gray-50 dark:text-gray-400 dark:hover:bg-gray-700"
                        >
                            <div className="flex items-center space-x-2">
                                <span className="text-lg">🚫</span>
                                <span>No mood</span>
                            </div>
                        </button>

                        {/* Mood Options */}
                        {moodOptions.map((option) => {
                            const isSelected = selectedMood === option.value;
                            return (
                                <button
                                    key={option.value}
                                    type="button"
                                    onClick={() => handleMoodSelect(option.value)}
                                    className={`w-full px-3 py-2 text-left text-sm transition-colors duration-200 ${
                                        isSelected
                                            ? getColorClasses(option.color, true)
                                            : "text-gray-700 hover:bg-gray-50 dark:text-gray-300 dark:hover:bg-gray-700"
                                    } `}
                                >
                                    <div className="flex items-center space-x-3">
                                        <span className="text-lg">{option.icon}</span>
                                        <div className="flex-1">
                                            <div className="font-medium">{option.label}</div>
                                            <div className="text-xs opacity-75">{option.description}</div>
                                        </div>
                                        {isSelected && <div className="h-2 w-2 rounded-full bg-current opacity-60"></div>}
                                    </div>
                                </button>
                            );
                        })}
                    </div>
                </div>
            )}
        </div>
    );
};
