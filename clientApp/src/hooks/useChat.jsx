"use client";

import { createContext, useContext, useEffect, useState } from "react";
import { useDispatch } from "react-redux";
import { chat as chatThunk } from "@/redux-store/index";

const ChatContext = createContext();

export const ChatProvider = ({ children }) => {
    const dispatch = useDispatch();
    
    const chat = async (message) => {
        setLoading(true);
        try {
            const result = await dispatch(chatThunk(message));
            if (chatThunk.fulfilled.match(result)) {
                const resp = result.payload.messages;
                setMessages((messages) => [...messages, ...resp]);
            } else {
                console.error("Chat failed:", result.error);
            }
        } catch (error) {
            console.error("Chat error:", error);
        } finally {
            setLoading(false);
        }
    };
  
    const [messages, setMessages] = useState([]);
    const [message, setMessage] = useState();
    const [loading, setLoading] = useState(false);
    const [cameraZoomed, setCameraZoomed] = useState(true);
    const onMessagePlayed = () => {
        setMessages((messages) => messages.slice(1));
    };

    useEffect(() => {
        if (messages.length > 0) {
            setMessage(messages[0]);
        } else {
            setMessage(null);
        }
    }, [messages]);

    return (
        <ChatContext.Provider
            value={{
                chat,
                message,
                onMessagePlayed,
                loading,
                cameraZoomed,
                setCameraZoomed,
            }}
        >
            {children}
        </ChatContext.Provider>
    );
};

export const useChat = () => {
    const context = useContext(ChatContext);
    if (!context) {
        throw new Error("useChat must be used within a ChatProvider");
    }
    return context;
};
