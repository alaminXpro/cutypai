/** @type {import('tailwindcss').Config} */
export default {
    content: [
        './Views/**/*.cshtml',
        './Areas/**/Views/**/*.cshtml',
        './wwwroot/js/**/*.js'
    ],
    theme: {
        extend: {
            colors: {
                primary: {
                    50: '#eff6ff',
                    500: '#3b82f6',
                    600: '#2563eb',
                    700: '#1d4ed8',
                },
                cream: {
                    DEFAULT: '#fff8f2',
                },
                olive: {
                    100: '#e6efe6',
                    300: '#9bbf9a',
                    700: '#3b5a40',
                    900: '#253527',
                },
                terracotta: {
                    400: '#d77a67',
                    600: '#b64b3a',
                }
            },
            keyframes: {
                float: { '0%': { transform: 'translateY(0)' }, '50%': { transform: 'translateY(-8px)' }, '100%': { transform: 'translateY(0)' } },
                marquee: { '0%': { transform: 'translateX(0%)' }, '100%': { transform: 'translateX(-50%)' } },
            },
            animation: {
                float: 'float 6s ease-in-out infinite',
                marquee: 'marquee 18s linear infinite'
            }
        },
    },
    plugins: [
        '@tailwindcss/forms',
        '@tailwindcss/typography',
    ]
}