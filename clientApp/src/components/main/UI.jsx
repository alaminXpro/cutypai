"use client";
import { useRef } from "react";
import { useChat } from "@/hooks/useChat";
import { Input } from "@/components/base/input/input";
import { Button } from "@/components/base/buttons/button";
import { ButtonUtility } from "@/components/base/buttons/button-utility";
import { VideoRecorder, ZoomIn, ZoomOut } from "@untitledui/icons";


export const UI = ({ hidden, ...props }) => {
  const input = useRef();
  const { chat, loading, cameraZoomed, setCameraZoomed, message } = useChat();

  const sendMessage = () => {
    const text = input.current.value;
    if (!loading && !message) {
      chat(text);
      input.current.value = "";
    }
  };
  if (hidden) {
    return null;
  }

  return (
    <>
      <div className="fixed top-0 left-0 right-0 bottom-0 z-50 flex justify-between p-4 flex-col pointer-events-none">
        {/* <div className="self-start backdrop-blur-md bg-white bg-opacity-50 p-4 rounded-lg">
          <h1 className="font-black text-xl">My Virtual GF</h1>
          <p>I will always love you ❤️</p>
        </div> */}
        <div className="w-full flex flex-col items-end justify-center gap-4">
          <ButtonUtility
            onClick={() => setCameraZoomed(!cameraZoomed)}
            icon={cameraZoomed ? ZoomOut : ZoomIn}
            size="md"
            color="secondary"
            tooltip={cameraZoomed ? "Zoom out" : "Zoom in"}
            className="pointer-events-auto hidden lg:flex"
          />
          {/* <ButtonUtility
            onClick={() => {
              const body = document.querySelector("body");
              if (body.classList.contains("greenScreen")) {
                body.classList.remove("greenScreen");
              } else {
                body.classList.add("greenScreen");
              }
            }}
            icon={VideoRecorder}
            size="md"
            color="secondary"
            tooltip="Toggle green screen"
            className="pointer-events-auto"
          /> */}
        </div>
        <div className="flex items-center gap-3 pointer-events-auto max-w-screen-sm w-full mx-auto">
          <div className="flex-1">
            <Input
              ref={input}
              placeholder="Type a message..."
              size="md"
              className="w-full"
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  sendMessage();
                }
              }}
            />
          </div>
          <Button
            onClick={sendMessage}
            isDisabled={loading || message}
            size="md"
            color="primary"
            className="pointer-events-auto"
          >
            {loading ? "Sending..." : "Send"}
          </Button>
        </div>
      </div>
    </>
  );
};
