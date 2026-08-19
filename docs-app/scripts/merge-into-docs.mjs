/**
 * Build the Vue hub and merge into docs/ without wiping stable JSON or per-mod guides.
 *
 * Keeps: versions.json, mod-matrix.json, search-index.json, data/, per-mod folders,
 * shared.js / shared-styles.css / scripts/, favicon, og-card, 404, icon-comparison.
 *
 * Replaces: docs/index.html and docs/assets/hub-* (Vite hashed bundles).
 */
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const appRoot = path.resolve(__dirname, "..");
const docsRoot = path.resolve(appRoot, "../docs");
const distRoot = path.resolve(appRoot, "dist");

function rmrf(target) {
  if (fs.existsSync(target)) fs.rmSync(target, { recursive: true, force: true });
}

function copyFile(src, dest) {
  fs.mkdirSync(path.dirname(dest), { recursive: true });
  fs.copyFileSync(src, dest);
}

const build = spawnSync("npm", ["run", "build"], {
  cwd: appRoot,
  stdio: "inherit",
  shell: process.platform === "win32",
});
if (build.status !== 0) process.exit(build.status || 1);

if (!fs.existsSync(path.join(distRoot, "index.html"))) {
  console.error("docs-app build did not produce dist/index.html");
  process.exit(1);
}

// Remove previous Vite hub assets only
const assetsDir = path.join(docsRoot, "assets");
if (fs.existsSync(assetsDir)) {
  for (const name of fs.readdirSync(assetsDir)) {
    if (name.startsWith("index-") || name.startsWith("hub-")) {
      fs.unlinkSync(path.join(assetsDir, name));
    }
  }
}

// Copy new assets
const distAssets = path.join(distRoot, "assets");
if (fs.existsSync(distAssets)) {
  for (const name of fs.readdirSync(distAssets)) {
    copyFile(path.join(distAssets, name), path.join(assetsDir, name));
  }
}

copyFile(path.join(distRoot, "index.html"), path.join(docsRoot, "index.html"));

// Drop legacy hub-only stylesheets (mod pages still use shared-styles.css)
for (const legacy of ["index-style.css", "styles.css"]) {
  const p = path.join(docsRoot, legacy);
  if (fs.existsSync(p)) fs.unlinkSync(p);
}

console.log("Merged Vue hub into docs/ (stable JSON + mod guides preserved).");
