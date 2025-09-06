"use client";

import { useEffect, useState } from "react";
import { Dialog, DialogTrigger, Modal, ModalOverlay } from "@/components/application/modals/modal";
import { useAppDispatch, useAppSelector } from "@/hooks/use-redux";
import { clearError, login, me, register, setModal } from "@/redux-store";
import { GoogleLogin } from "./GoogleLogin";

interface LoginFormProps {
    onClose?: () => void;
}

export const LoginForm = ({ onClose }: LoginFormProps) => {
    const dispatch = useAppDispatch();
    const { loading, error, modal } = useAppSelector((state) => state.cutypai);
    const [isRegister, setIsRegister] = useState(false);
    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [avatarUrl, setAvatarUrl] = useState("");
    const [showPassword, setShowPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);
    const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

    const validateForm = () => {
        const errors: Record<string, string> = {};

        if (isRegister) {
            if (!name.trim()) errors.name = "Name is required";
            if (!email.trim()) errors.email = "Email is required";
            else if (!/\S+@\S+\.\S+/.test(email)) errors.email = "Email is invalid";
            if (!password) errors.password = "Password is required";
            else if (password.length < 8) errors.password = "Password must be at least 8 characters";
            if (password !== confirmPassword) errors.confirmPassword = "Passwords do not match";
        } else {
            if (!email.trim()) errors.email = "Email is required";
            else if (!/\S+@\S+\.\S+/.test(email)) errors.email = "Email is invalid";
            if (!password) errors.password = "Password is required";
        }

        setValidationErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!validateForm()) return;

        try {
            await dispatch(login({ email, password })).then(() => {
                dispatch(me());
            });
        } catch (error) {
            console.error(error);
        }
    };

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!validateForm()) return;

        try {
            await dispatch(
                register({
                    name,
                    email,
                    password,
                    avatarUrl: avatarUrl || undefined,
                }),
            ).then(() => {
                dispatch(me());
            });
        } catch (error) {
            console.error(error);
        }
    };

    const handleGoogleSuccess = () => {
        console.log("Google login successful!");
        // Redirect or update UI as needed
    };

    const handleGoogleError = (error: string) => {
        console.error("Google login error:", error);
    };

    const handleClose = () => {
        dispatch(setModal(false));
        onClose?.();
    };

    const clearForm = () => {
        setName("");
        setEmail("");
        setPassword("");
        setConfirmPassword("");
        setAvatarUrl("");
        setValidationErrors({});
        dispatch(clearError());
    };

    // Clear error when component mounts
    useEffect(() => {
        dispatch(clearError());
    }, [dispatch]);

    // Clear error when user starts typing in any field
    useEffect(() => {
        if (error && (name || email || password || confirmPassword)) {
            dispatch(clearError());
        }
    }, [dispatch, error, name, email, password, confirmPassword]);

    const toggleMode = () => {
        setIsRegister(!isRegister);
        clearForm();
    };

    return (
        <ModalOverlay isOpen={modal} onOpenChange={handleClose}>
            <Modal className="w-full max-w-md">
                <Dialog className="flex min-h-screen w-full items-center justify-center p-2 sm:p-4">
                    <div
                        className="max-h-[90vh] w-full max-w-sm overflow-y-auto rounded-2xl border p-4 shadow-xl sm:p-6"
                        style={{
                            backgroundColor: "var(--color-bg-primary)",
                            borderColor: "var(--color-border-secondary)",
                        }}
                    >
                        {/* Close Button */}
                        <div className="flex justify-end">
                            <button
                                onClick={handleClose}
                                className="rounded-lg p-2 hover:opacity-80 focus:ring-2 focus:outline-none"
                                style={{
                                    color: "var(--color-text-quaternary)",
                                }}
                            >
                                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                </svg>
                            </button>
                        </div>

                        {/* Header */}
                        <div className="mb-4 text-center sm:mb-6">
                            <h1
                                className="font-semibold"
                                style={{
                                    color: "var(--color-text-primary)",
                                    fontSize: "var(--text-display-xs)",
                                    lineHeight: "var(--text-display-xs--line-height)",
                                }}
                            >
                                {isRegister ? "Create your account" : "Welcome back"}
                            </h1>
                            <p
                                className="mt-1 sm:mt-2"
                                style={{
                                    color: "var(--color-text-tertiary)",
                                    fontSize: "var(--text-sm)",
                                    lineHeight: "var(--text-sm--line-height)",
                                }}
                            >
                                {isRegister ? "Start your free trial today" : "Sign in to your account"}
                            </p>
                        </div>

                        {/* Google Login */}
                        <div className="mb-4 sm:mb-6">
                            <GoogleLogin onSuccess={handleGoogleSuccess} onError={handleGoogleError} className="w-full" />
                        </div>

                        {/* Divider */}
                        <div className="relative mb-4 sm:mb-6">
                            <div className="absolute inset-0 flex items-center">
                                <div className="w-full border-t border-gray-200" />
                            </div>
                            <div className="relative flex justify-center">
                                <span
                                    className="px-3"
                                    style={{
                                        backgroundColor: "var(--color-bg-primary)",
                                        color: "var(--color-text-tertiary)",
                                        fontSize: "var(--text-sm)",
                                        lineHeight: "var(--text-sm--line-height)",
                                    }}
                                >
                                    Or continue with email
                                </span>
                            </div>
                        </div>

                        {/* Form */}
                        <form onSubmit={isRegister ? handleRegister : handleLogin} className="space-y-3 sm:space-y-4">
                            {/* Name field - only for registration */}
                            {isRegister && (
                                <div>
                                    <label
                                        htmlFor="name"
                                        className="block font-semibold"
                                        style={{
                                            color: "var(--color-text-primary)",
                                            fontSize: "var(--text-sm)",
                                            lineHeight: "var(--text-sm--line-height)",
                                        }}
                                    >
                                        Full name
                                    </label>
                                    <div className="mt-1">
                                        <input
                                            type="text"
                                            id="name"
                                            value={name}
                                            onChange={(e) => setName(e.target.value)}
                                            className={`block w-full rounded-lg border px-3 py-2 shadow-sm transition-all focus:border-transparent focus:ring-2 focus:outline-none sm:py-2.5 ${
                                                validationErrors.name
                                                    ? "border-red-300 text-red-900 focus:ring-red-500 dark:text-red-100"
                                                    : "border-gray-300 bg-white text-gray-900 hover:border-gray-400 dark:bg-gray-800 dark:text-gray-100"
                                            }`}
                                            style={{
                                                color: "var(--color-text-primary)",
                                                backgroundColor: "var(--color-bg-primary)",
                                                borderColor: validationErrors.name ? "var(--color-border-error)" : "var(--color-border-primary)",
                                                fontSize: "var(--text-sm)",
                                                lineHeight: "var(--text-sm--line-height)",
                                            }}
                                            placeholder="Enter your full name"
                                        />
                                        {validationErrors.name && (
                                            <p
                                                className="mt-1"
                                                style={{
                                                    color: "var(--color-text-error-primary)",
                                                    fontSize: "var(--text-sm)",
                                                    lineHeight: "var(--text-sm--line-height)",
                                                }}
                                            >
                                                {validationErrors.name}
                                            </p>
                                        )}
                                    </div>
                                </div>
                            )}

                            {/* Avatar URL field - only for registration */}
                            {isRegister && (
                                <div>
                                    <label
                                        htmlFor="avatarUrl"
                                        className="block font-semibold"
                                        style={{
                                            color: "var(--color-text-primary)",
                                            fontSize: "var(--text-sm)",
                                            lineHeight: "var(--text-sm--line-height)",
                                        }}
                                    >
                                        Avatar URL{" "}
                                        <span className="font-normal" style={{ color: "var(--color-text-tertiary)" }}>
                                            (optional)
                                        </span>
                                    </label>
                                    <div className="mt-1">
                                        <input
                                            type="url"
                                            id="avatarUrl"
                                            value={avatarUrl}
                                            onChange={(e) => setAvatarUrl(e.target.value)}
                                            placeholder="https://example.com/avatar.jpg"
                                            className="block w-full rounded-lg border px-3 py-2 shadow-sm transition-all hover:border-gray-400 focus:border-transparent focus:ring-2 focus:ring-blue-500 focus:outline-none sm:py-2.5"
                                            style={{
                                                color: "var(--color-text-primary)",
                                                backgroundColor: "var(--color-bg-primary)",
                                                borderColor: "var(--color-border-primary)",
                                                fontSize: "var(--text-sm)",
                                                lineHeight: "var(--text-sm--line-height)",
                                            }}
                                        />
                                    </div>
                                </div>
                            )}

                            {/* Email field */}
                            <div>
                                <label
                                    htmlFor="email"
                                    className="block font-semibold"
                                    style={{
                                        color: "var(--color-text-primary)",
                                        fontSize: "var(--text-sm)",
                                        lineHeight: "var(--text-sm--line-height)",
                                    }}
                                >
                                    Email address
                                </label>
                                <div className="mt-1">
                                    <input
                                        type="email"
                                        id="email"
                                        value={email}
                                        onChange={(e) => setEmail(e.target.value)}
                                        className={`block w-full rounded-lg border px-3 py-2 shadow-sm transition-all focus:border-transparent focus:ring-2 focus:outline-none sm:py-2.5 ${
                                            validationErrors.email
                                                ? "border-red-300 text-red-900 focus:ring-red-500 dark:text-red-100"
                                                : "border-gray-300 bg-white text-gray-900 hover:border-gray-400 dark:bg-gray-800 dark:text-gray-100"
                                        }`}
                                        style={{
                                            color: "var(--color-text-primary)",
                                            backgroundColor: "var(--color-bg-primary)",
                                            borderColor: validationErrors.email ? "var(--color-border-error)" : "var(--color-border-primary)",
                                            fontSize: "var(--text-sm)",
                                            lineHeight: "var(--text-sm--line-height)",
                                        }}
                                        placeholder="Enter your email"
                                    />
                                    {validationErrors.email && (
                                        <p
                                            className="mt-1"
                                            style={{
                                                color: "var(--color-text-error-primary)",
                                                fontSize: "var(--text-sm)",
                                                lineHeight: "var(--text-sm--line-height)",
                                            }}
                                        >
                                            {validationErrors.email}
                                        </p>
                                    )}
                                </div>
                            </div>

                            {/* Password field */}
                            <div>
                                <label
                                    htmlFor="password"
                                    className="block font-semibold"
                                    style={{
                                        color: "var(--color-text-primary)",
                                        fontSize: "var(--text-sm)",
                                        lineHeight: "var(--text-sm--line-height)",
                                    }}
                                >
                                    Password
                                </label>
                                <div className="relative mt-1">
                                    <input
                                        type={showPassword ? "text" : "password"}
                                        id="password"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        className={`block w-full rounded-lg border px-3 py-2 pr-10 shadow-sm transition-all focus:border-transparent focus:ring-2 focus:outline-none sm:py-2.5 ${
                                            validationErrors.password
                                                ? "border-red-300 text-red-900 focus:ring-red-500 dark:text-red-100"
                                                : "border-gray-300 bg-white text-gray-900 hover:border-gray-400 dark:bg-gray-800 dark:text-gray-100"
                                        }`}
                                        style={{
                                            color: "var(--color-text-primary)",
                                            backgroundColor: "var(--color-bg-primary)",
                                            borderColor: validationErrors.password ? "var(--color-border-error)" : "var(--color-border-primary)",
                                            fontSize: "var(--text-sm)",
                                            lineHeight: "var(--text-sm--line-height)",
                                        }}
                                        placeholder="Enter your password"
                                    />
                                    <button
                                        type="button"
                                        onClick={() => setShowPassword(!showPassword)}
                                        className="absolute inset-y-0 right-0 z-10 flex items-center pr-3 focus:outline-none"
                                        style={{
                                            color: "var(--color-text-quaternary)",
                                            top: "50%",
                                            transform: "translateY(-50%)",
                                        }}
                                    >
                                        {showPassword ? (
                                            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                <path
                                                    strokeLinecap="round"
                                                    strokeLinejoin="round"
                                                    strokeWidth={1.5}
                                                    d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 11-4.243-4.243m4.242 4.242L9.88 9.88"
                                                />
                                            </svg>
                                        ) : (
                                            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                <path
                                                    strokeLinecap="round"
                                                    strokeLinejoin="round"
                                                    strokeWidth={1.5}
                                                    d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z"
                                                />
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                            </svg>
                                        )}
                                    </button>
                                    {validationErrors.password && (
                                        <p
                                            className="mt-1"
                                            style={{
                                                color: "var(--color-text-error-primary)",
                                                fontSize: "var(--text-sm)",
                                                lineHeight: "var(--text-sm--line-height)",
                                            }}
                                        >
                                            {validationErrors.password}
                                        </p>
                                    )}
                                </div>
                            </div>

                            {/* Confirm Password field - only for registration */}
                            {isRegister && (
                                <div>
                                    <label
                                        htmlFor="confirmPassword"
                                        className="block font-semibold"
                                        style={{
                                            color: "var(--color-text-primary)",
                                            fontSize: "var(--text-sm)",
                                            lineHeight: "var(--text-sm--line-height)",
                                        }}
                                    >
                                        Confirm password
                                    </label>
                                    <div className="relative mt-1">
                                        <input
                                            type={showConfirmPassword ? "text" : "password"}
                                            id="confirmPassword"
                                            value={confirmPassword}
                                            onChange={(e) => setConfirmPassword(e.target.value)}
                                            className={`block w-full rounded-lg border px-3 py-2 pr-10 shadow-sm transition-all focus:border-transparent focus:ring-2 focus:outline-none sm:py-2.5 ${
                                                validationErrors.confirmPassword
                                                    ? "border-red-300 text-red-900 focus:ring-red-500 dark:text-red-100"
                                                    : "border-gray-300 bg-white text-gray-900 hover:border-gray-400 dark:bg-gray-800 dark:text-gray-100"
                                            }`}
                                            style={{
                                                color: "var(--color-text-primary)",
                                                backgroundColor: "var(--color-bg-primary)",
                                                borderColor: validationErrors.confirmPassword ? "var(--color-border-error)" : "var(--color-border-primary)",
                                                fontSize: "var(--text-sm)",
                                                lineHeight: "var(--text-sm--line-height)",
                                            }}
                                            placeholder="Confirm your password"
                                        />
                                        <button
                                            type="button"
                                            onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                                            className="absolute inset-y-0 right-0 z-10 flex items-center pr-3 focus:outline-none"
                                            style={{
                                                color: "var(--color-text-quaternary)",
                                                top: "50%",
                                                transform: "translateY(-50%)",
                                            }}
                                        >
                                            {showConfirmPassword ? (
                                                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                    <path
                                                        strokeLinecap="round"
                                                        strokeLinejoin="round"
                                                        strokeWidth={1.5}
                                                        d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 11-4.243-4.243m4.242 4.242L9.88 9.88"
                                                    />
                                                </svg>
                                            ) : (
                                                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                    <path
                                                        strokeLinecap="round"
                                                        strokeLinejoin="round"
                                                        strokeWidth={1.5}
                                                        d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z"
                                                    />
                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                                </svg>
                                            )}
                                        </button>
                                        {validationErrors.confirmPassword && (
                                            <p
                                                className="mt-1"
                                                style={{
                                                    color: "var(--color-text-error-primary)",
                                                    fontSize: "var(--text-sm)",
                                                    lineHeight: "var(--text-sm--line-height)",
                                                }}
                                            >
                                                {validationErrors.confirmPassword}
                                            </p>
                                        )}
                                    </div>
                                </div>
                            )}

                            {/* Submit Button */}
                            <button
                                type="submit"
                                disabled={loading}
                                className="w-full rounded-lg px-4 py-2 font-semibold shadow-sm transition-all focus:ring-2 focus:ring-offset-2 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50 sm:py-2.5"
                                style={{
                                    backgroundColor: "var(--color-bg-brand-solid)",
                                    color: "var(--color-text-primary_on-brand)",
                                    fontSize: "var(--text-sm)",
                                    lineHeight: "var(--text-sm--line-height)",
                                }}
                            >
                                {loading ? (
                                    <div className="flex items-center justify-center">
                                        <svg className="mr-2 h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
                                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                            <path
                                                className="opacity-75"
                                                fill="currentColor"
                                                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                                            />
                                        </svg>
                                        {isRegister ? "Creating account..." : "Signing in..."}
                                    </div>
                                ) : isRegister ? (
                                    "Create account"
                                ) : (
                                    "Sign in"
                                )}
                            </button>
                        </form>

                        {/* Error Message */}
                        {error && (
                            <div className="mt-3 rounded-lg p-3" style={{ backgroundColor: "var(--color-bg-error-primary)" }}>
                                <div className="flex">
                                    <div className="flex-shrink-0">
                                        <svg
                                            className="h-5 w-5"
                                            fill="none"
                                            viewBox="0 0 24 24"
                                            stroke="currentColor"
                                            style={{ color: "var(--color-text-error-primary)" }}
                                        >
                                            <path
                                                strokeLinecap="round"
                                                strokeLinejoin="round"
                                                strokeWidth={1.5}
                                                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                                            />
                                        </svg>
                                    </div>
                                    <div className="ml-3">
                                        <p
                                            style={{
                                                color: "var(--color-text-error-primary)",
                                                fontSize: "var(--text-sm)",
                                                lineHeight: "var(--text-sm--line-height)",
                                            }}
                                        >
                                            {error}
                                        </p>
                                    </div>
                                </div>
                            </div>
                        )}

                        {/* Toggle between Login and Register */}
                        <div className="mt-4 text-center sm:mt-6">
                            <p
                                style={{
                                    color: "var(--color-text-tertiary)",
                                    fontSize: "var(--text-sm)",
                                    lineHeight: "var(--text-sm--line-height)",
                                }}
                            >
                                {isRegister ? "Already have an account?" : "Don't have an account?"}
                                <button
                                    type="button"
                                    onClick={toggleMode}
                                    className="ml-1 font-semibold hover:opacity-80 focus:underline focus:outline-none"
                                    style={{ color: "var(--color-brand-600)" }}
                                >
                                    {isRegister ? "Sign in" : "Sign up"}
                                </button>
                            </p>
                        </div>
                    </div>
                </Dialog>
            </Modal>
        </ModalOverlay>
    );
};
