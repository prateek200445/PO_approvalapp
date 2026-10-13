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
      // The .NET API, its build outputs and scratch probes live inside this folder;
      // watching them makes every `dotnet build` flood the dev server with file events.
      watch: {
        ignored: ["**/POApprovalAPI/**", "**/bin/**", "**/obj/**", "**/.vercel/**", "**/dist/**"],
      },
      proxy: {
        "/api": {
          target: process.env.VITE_API_URL || "http://localhost:5115",
          changeOrigin: true,
          secure: false,
          // Stock Analysis SP can take several minutes
          timeout: 600_000,
          proxyTimeout: 600_000,
        },
      },
    },
  },
});
