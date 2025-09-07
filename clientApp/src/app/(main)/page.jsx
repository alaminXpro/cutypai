"use client";
import { Loader } from "@react-three/drei";
import { Canvas } from "@react-three/fiber";
import { Leva } from "leva";
import { Experience } from "@/components/main/Experience";
import { UI } from "@/components/main/UI";
import { ChatProvider } from "@/hooks/useChat";

function MainPage() {
  return (
    <>
      <ChatProvider>
      <Loader />
      <Leva hidden />
      <UI />
      <Canvas 
        shadows 
        camera={{ position: [0, 0, 1], fov: 30 }}
        className="fixed inset-0 w-full h-full"
        style={{ position: 'fixed', top: 0, left: 0, width: '100vw', height: '100vh' }}
      >
          <Experience />
        </Canvas>
      </ChatProvider>
    </>
  );
}

export default MainPage;