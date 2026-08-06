import { defineConfig } from "@lovable.dev/vite-tanstack-config";

export default defineConfig({
  nitro: {
    preset: "vercel",
    output: {
      dir: ".vercel/output",
      serverDir: ".vercel/output/functions/__server.func",
      publicDir: ".vercel/output/static",
    },
    routeRules: {
      "/api/**": {
        proxy: "https://po-approvalapp.onrender.com/api/**",
      },
    },
  },

  tanstackStart: {
    server: { entry: "server" },
  },

  vite: {
    server: {
      proxy: {
        "/api": {
         target: process.env.VITE_API_URL || "http://localhost:5115",
          changeOrigin: true,
          secure: false,
        },
      },
    },
  },
});