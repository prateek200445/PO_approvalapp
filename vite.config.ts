import { defineConfig } from "@lovable.dev/vite-tanstack-config";

export default defineConfig({
  tanstackStart: {
    server: { entry: "server" },
  },

  vite: {
    server: {
      proxy: {
        "/api": {
          target: process.env.VITE_API_URL || "https://doing-cassette-decorating-parcel.trycloudflare.com",
          changeOrigin: true,
          secure: false,
        },
      },
    },
  },
});