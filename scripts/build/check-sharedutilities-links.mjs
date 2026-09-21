/**
 * Fail when any SharedUtilities/*.cs file is linked by zero mod projects (AZR-256 / AZR-241).
 * Run: node scripts/build/check-sharedutilities-links.mjs
 */
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../..");
const sharedDir = path.join(repoRoot, "SharedUtilities");

const sharedFiles = fs
  .readdirSync(sharedDir)
  .filter((f) => f.endsWith(".cs"))
  .sort();

const modDirs = fs
  .readdirSync(repoRoot, { withFileTypes: true })
  .filter((d) => d.isDirectory() && fs.existsSync(path.join(repoRoot, d.name, `${d.name}.csproj`) || path.join(repoRoot, d.name)))
  .map((d) => d.name);

// Find all *.csproj under repo (one level + known mods)
function findCsprojs(dir, depth = 0, out = []) {
  if (depth > 2) return out;
  for (const name of fs.readdirSync(dir)) {
    if (name === "node_modules" || name === ".git" || name === "bin" || name === "obj") continue;
    const p = path.join(dir, name);
    const st = fs.statSync(p);
    if (st.isDirectory()) findCsprojs(p, depth + 1, out);
    else if (name.endsWith(".csproj") && !name.includes(".Tests")) out.push(p);
  }
  return out;
}

const csprojs = findCsprojs(repoRoot);
const linkCounts = Object.fromEntries(sharedFiles.map((f) => [f, 0]));

for (const proj of csprojs) {
  const text = fs.readFileSync(proj, "utf8");
  for (const file of sharedFiles) {
    if (text.includes(`SharedUtilities\\${file}`) || text.includes(`SharedUtilities/${file}`) || text.includes(`$(MSBuildThisFileDirectory)${file}`)) {
      linkCounts[file] += 1;
    }
    // Baseline props import counts as a link for every file listed there
    if (text.includes("SharedUtilities.Baseline.props")) {
      const baseline = fs.readFileSync(path.join(sharedDir, "SharedUtilities.Baseline.props"), "utf8");
      if (baseline.includes(file)) linkCounts[file] += 1;
    }
  }
}

const orphans = Object.entries(linkCounts).filter(([, n]) => n === 0).map(([f]) => f);
if (orphans.length) {
  console.error("SharedUtilities files linked by zero projects:");
  for (const f of orphans) console.error(`  - ${f}`);
  process.exit(1);
}

console.log(`OK: ${sharedFiles.length} SharedUtilities .cs file(s) each linked by ≥1 project.`);
