"use client";

import { useState } from "react";
import { useAppDispatch } from "@/hooks/use-redux";
import { login } from "@/redux-store";
import { GoogleLogin } from "./GoogleLogin";

export const LoginForm = () => {
    const dispatch = useAppDispatch();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [isLoading, setIsLoading] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsLoading(true);
        try {
            await dispatch(login());
        } finally {
            setIsLoading(false);
        }
    };

    const handleGoogleSuccess = () => {
        console.log("Google login successful!");
        // Redirect or update UI as needed
    };

    const handleGoogleError = (error: string) => {
        console.error("Google login error:", error);
    };

    return (
        <div className="mx-auto mt-8 max-w-md rounded-lg bg-white p-6 shadow-md">
            <h2 className="mb-6 text-center text-2xl font-bold">Login</h2>

            {/* Google Login */}
            <div className="mb-6">
                <GoogleLogin onSuccess={handleGoogleSuccess} onError={handleGoogleError} className="w-full" />
            </div>

            {/* Divider */}
            <div className="relative mb-6">
                <div className="absolute inset-0 flex items-center">
                    <div className="w-full border-t border-gray-300" />
                </div>
                <div className="relative flex justify-center text-sm">
                    <span className="bg-white px-2 text-gray-500">Or continue with email</span>
                </div>
            </div>

            {/* Email/Password Form */}
            <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                    <label htmlFor="email" className="block text-sm font-medium text-gray-700">
                        Email
                    </label>
                    <input
                        type="email"
                        id="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 focus:outline-none"
                        required
                    />
                </div>

                <div>
                    <label htmlFor="password" className="block text-sm font-medium text-gray-700">
                        Password
                    </label>
                    <input
                        type="password"
                        id="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 focus:outline-none"
                        required
                    />
                </div>

                <button
                    type="submit"
                    disabled={isLoading}
                    className="flex w-full justify-center rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none disabled:opacity-50"
                >
                    {isLoading ? "Logging in..." : "Login"}
                </button>
            </form>
        </div>
    );
};
