import path from "path"
import tailwindcss from "@tailwindcss/vite"
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { visualizer } from "rollup-plugin-visualizer";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss(), visualizer({
    open: false, // Automatically opens the report in your browser
    filename: "bundle-stats.html", // Name of the output file
    gzipSize: true, // Show gzip size
    brotliSize: true, // Show brotli size
  }),
  ],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    proxy:
      process.env.NODE_ENV === "production"
        ? undefined
        : {
          "/api": {
            target: "http://localhost:5139",
            changeOrigin: true,
            secure: false,
          },
        },
  },
  // Set base path for production build
  base: process.env.NODE_ENV === "production" ? "/app" : "/",
})
