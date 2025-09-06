"use client";

import { GoogleLogin as GoogleLoginButton } from "@react-oauth/google";
import { useAppDispatch } from "@/hooks/use-redux";
import { googleLogin, me } from "../../redux-store";

interface GoogleLoginProps {
    onSuccess?: () => void;
    onError?: (error: string) => void;
    className?: string;
}

export const GoogleLogin = ({ onSuccess, onError, className }: GoogleLoginProps) => {
    const dispatch = useAppDispatch();

    const handleSuccess = async (credentialResponse: any) => {
        if (credentialResponse.credential) {
            try {
                const result = await dispatch(googleLogin(credentialResponse.credential));
                if (googleLogin.fulfilled.match(result)) {
                    // Fetch user data after successful login
                    await dispatch(me());
                    onSuccess?.();
                } else {
                    onError?.("Google login failed");
                }
            } catch (error) {
                onError?.("Google login failed");
            }
        }
    };

    const handleError = () => {
        onError?.("Google login was cancelled or failed");
    };

    return (
        <div className={className}>
            <GoogleLoginButton
                onSuccess={handleSuccess}
                onError={handleError}
                useOneTap
                theme="outline"
                size="large"
                text="continue_with"
                shape="rectangular"
                logo_alignment="left"
            />
        </div>
    );
};
