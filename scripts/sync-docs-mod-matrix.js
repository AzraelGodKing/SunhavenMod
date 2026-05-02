/**
 * Single source of truth: scripts/mod-matrix.json → docs/mod-matrix.json (docs hub subset + statsDomId).
 * Run: npm run sync-mod-matrix  (from repo root)
 */
const fs = require("fs");
const path = require("path");

const ROOT = path.resolve(__dirname, "..");
const SRC = path.join(ROOT, "scripts", "mod-matrix.json");
const DEST = path.join(ROOT, "docs", "mod-matrix.json");

const PICK = [
  "modKey",
  "statsDomId",
  "jsonKey",
  "modDir",
  "thunderstoreName",
  "indexDataName",
  "docsPagePath",
  "docsNavNames",
  "docsCardNames",
];

function main() {
  const raw = fs.readFileSync(SRC, "utf8").replace(/^\uFEFF/, "");
  const rows = JSON.parse(raw);
  if (!Array.isArray(rows)) throw new Error("mod-matrix.json must be an array");

  const docs = rows.map((r) => {
    const o = {};
    for (const k of PICK) {
      if (Object.prototype.hasOwnProperty.call(r, k)) o[k] = r[k];
    }
    return o;
  });

  fs.mkdirSync(path.dirname(DEST), { recursive: true });
  fs.writeFileSync(DEST, JSON.stringify(docs, null, 2) + "\n", "utf8");
  console.log(`Wrote ${path.relative(ROOT, DEST)}`);
}

main();
