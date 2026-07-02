/**
 * Shared repo paths for Node scripts under scripts/.
 */
const path = require("path");

const SCRIPTS_ROOT = path.resolve(__dirname, "..");
const REPO_ROOT = path.resolve(SCRIPTS_ROOT, "..");

const paths = {
  repoRoot: REPO_ROOT,
  scriptsRoot: SCRIPTS_ROOT,
  modMatrix: path.join(SCRIPTS_ROOT, "matrix", "mod-matrix.json"),
  versionsJson: path.join(REPO_ROOT, "docs", "versions.json"),
  docsModMatrix: path.join(REPO_ROOT, "docs", "mod-matrix.json"),
  statsCache: path.join(REPO_ROOT, "docs", "data", "stats-cache.json"),
  translationCache: path.join(SCRIPTS_ROOT, ".cache", "translation-cache.json"),
};

module.exports = paths;
