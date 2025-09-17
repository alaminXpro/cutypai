"use client";
import { ChatInput } from "./ChatInput";

export const UI = ({ hidden, ...props }) => {
  return <ChatInput hidden={hidden} {...props} />;
};
