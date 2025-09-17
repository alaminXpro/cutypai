"use client";

import type { HTMLAttributes } from "react";
import { Hearts } from "@untitledui/icons";
import { motion } from "motion/react";
import { cx } from "@/utils/cx";

export const UntitledLogo = (props: HTMLAttributes<HTMLDivElement>) => {
    return (
        <motion.div
            className={cx("flex h-8 w-max items-center justify-start overflow-visible", props.className)}
            initial={{ opacity: 0, y: -10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, ease: "easeOut" }}
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
        >
            {/* Animated Hearts Icon */}
            <motion.div
                initial={{ scale: 0, rotate: -180 }}
                animate={{ scale: 1, rotate: 0 }}
                transition={{
                    duration: 0.8,
                    delay: 0.2,
                    type: "spring",
                    stiffness: 200,
                    damping: 15,
                }}
                whileHover={{
                    scale: 1.2,
                    rotate: [0, -10, 10, -10, 0],
                    transition: { duration: 0.5 },
                }}
                className="relative"
            >
                <Hearts className="aspect-square h-full w-auto shrink-0 text-pink-500 drop-shadow-lg" />
                {/* Pulsing glow effect */}
                <motion.div
                    className="absolute inset-0 rounded-full bg-pink-500/20 blur-sm"
                    animate={{
                        scale: [1, 1.3, 1],
                        opacity: [0.3, 0.6, 0.3],
                    }}
                    transition={{
                        duration: 2,
                        repeat: Infinity,
                        ease: "easeInOut",
                    }}
                />
            </motion.div>

            {/* Gap that adjusts to the height of the container */}
            <motion.div className="aspect-[0.3] h-full" initial={{ width: 0 }} animate={{ width: "auto" }} transition={{ duration: 0.5, delay: 0.4 }} />

            {/* Animated Text */}
            <motion.span
                className="text-lg font-bold tracking-wide text-pink-500 drop-shadow-md"
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ duration: 0.6, delay: 0.6, ease: "easeOut" }}
                whileHover={{
                    color: "#ec4899",
                    textShadow: "0 0 8px rgba(236, 72, 153, 0.5)",
                    transition: { duration: 0.2 },
                }}
            >
                <motion.span
                    animate={{
                        backgroundPosition: ["0% 50%", "100% 50%", "0% 50%"],
                    }}
                    transition={{
                        duration: 3,
                        repeat: Infinity,
                        ease: "linear",
                    }}
                    className="bg-gradient-to-r from-pink-500 via-purple-500 to-pink-500 bg-[length:200%_100%] bg-clip-text text-transparent"
                >
                    Your CutyPai
                </motion.span>
            </motion.span>
        </motion.div>
    );
};
