"use client";
import { Loader } from "@react-three/drei";
import { Canvas } from "@react-three/fiber";
import { Leva } from "leva";
import { Experience } from "@/components/main/Experience";
import { UI } from "@/components/main/UI";
import { ChatProvider } from "@/hooks/useChat";
import { LoginForm } from "@/components/auth/LoginForm";

function MainPage() {
  return (
    <>
      <ChatProvider>
      <Loader />
      <Leva />
      <UI />
      <Canvas 
        shadows 
        camera={{ position: [0, 0, 1], fov: 30 }}
        style={{ top: 0, left: 0, width: '100vw', height: '100vh' }}
      >
          <Experience />
        </Canvas>
      </ChatProvider>
      <LoginForm />
    </>
  );
}

export default MainPage;