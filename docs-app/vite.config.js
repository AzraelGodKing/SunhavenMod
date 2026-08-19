import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const docsRoot = path.resolve(__dirname, "../docs");

function serveDocsStatic() {
  return {
    name: "serve-docs-static",
    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        const url = req.url || "";
        const prefix = "/SunhavenMod/";
        if (!url.startsWith(prefix)) return next();
        const rel = decodeURIComponent(url.slice(prefix.length).split("?")[0]);
        if (!rel || rel.startsWith("src/") || rel.startsWith("@") || rel.startsWith("node_modules")) {
          return next();
        }
        const filePath = path.resolve(docsRoot, rel);
        if (!filePath.startsWith(docsRoot)) return next();
        if (!fs.existsSync(filePath) || fs.statSync(filePath).isDirectory()) return next();
        res.setHeader("Cache-Control", "no-cache");
        fs.createReadStream(filePath).pipe(res);
      });
    },
  };
}

export default defineConfig({
  base: "/SunhavenMod/",
  plugins: [vue(), serveDocsStatic()],
  build: {
    outDir: "dist",
    emptyOutDir: true,
    sourcemap: true,
  },
  server: {
    port: 5174,
  },
});
