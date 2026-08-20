import { defineConfig } from "@lovable.dev/vite-tanstack-config";
import { loadEnv } from "vite";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const apiTarget = env.VITE_API_URL || "http://localhost:5115";

  return {
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
          target: apiTarget,
          changeOrigin: true,
          secure: false,
          // Stock Analysis SP can take several minutes
          timeout: 600_000,
          proxyTimeout: 600_000,
        },
      },
    },
  },
};
});
