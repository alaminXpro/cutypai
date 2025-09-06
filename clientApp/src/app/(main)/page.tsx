"use client";

import { useAppDispatch, useAppSelector } from "@/hooks/use-redux";
import { login, me, revokeAllTokens, revokeCurrentToken } from "@/redux-store";

export default function MainPage() {
    const dispatch = useAppDispatch();
    const { accessToken, user, loading, error } = useAppSelector((state) => state.cutypai);

    return (
        <div className="flex h-screen flex-col items-center justify-center">
            <h1 className="text-2xl font-bold">Welcome to the Main Page</h1>
            <p className="mt-4 text-gray-600">This is the main page content.</p>
            <button
                className="mt-4 rounded-md bg-blue-500 px-4 py-2 text-white disabled:bg-gray-500"
                onClick={() => {
                    dispatch(login());
                }}
                disabled={loading}
            >
                {loading ? "Loading..." : "Login"}
            </button>
            <p className="mt-4 text-gray-600">{accessToken}</p>
            <p className="mt-4 text-gray-600">{error ? error : "No Error"}</p>

            <button
                className="mt-4 rounded-md bg-green-500 px-4 py-2 text-white disabled:bg-gray-500"
                onClick={() => {
                    dispatch(me());
                }}
                disabled={loading}
            >
                Profile
            </button>
            <p className="mt-4 text-gray-600">{user?.name || "No User"}</p>
            <p className="mt-4 text-gray-600">{user?.email || "No Email"}</p>
            <p className="mt-4 text-gray-600">{user?.role || "No Role"}</p>
            <p className="mt-4 text-gray-600">{user?.status || "No Status"}</p>
            <p className="mt-4 text-gray-600">{user?.avatar_url || "No Avatar"}</p>
            <p className="mt-4 text-gray-600">{user?.created_at || "No Created At"}</p>
            <p className="mt-4 text-gray-600">{user?.last_login || "No Last Login"}</p>
            <p className="mt-4 text-gray-600">{user?.preferences || "No Preferences"}</p>
            <button
                className="mt-4 rounded-md bg-red-500 px-4 py-2 text-white disabled:bg-gray-500"
                onClick={() => {
                    dispatch(revokeCurrentToken());
                }}
                disabled={loading}
            >
                Revoke Current Token
            </button>
            <button
                className="mt-4 rounded-md bg-red-500 px-4 py-2 text-white disabled:bg-gray-500"
                onClick={() => {
                    dispatch(revokeAllTokens());
                }}
                disabled={loading}
            >
                Revoke All Tokens
            </button>
        </div>
    );
}
