const fs = require("fs");
const path = require("path");

try {
  require("dotenv").config();
} catch (e) {
  if (e && e.code !== "MODULE_NOT_FOUND") throw e;
}

const ROOT = path.resolve(__dirname, "..");
/** Served as /data/stats-cache.json on GitHub Pages when publishing from /docs */
const CACHE_PATH = path.join(ROOT, "docs", "data", "stats-cache.json");
const TMP_PATH = `${CACHE_PATH}.tmp`;

const THUNDERSTORE_SUN_HAVEN_COMMUNITY = "sun-haven";

/** Nexus mod ids from `docs/versions.json` (`nexus` URL …/mods/{id}); used when `nexus.modId` is omitted in the roster. */
function loadNexusModIdByThunderstoreName() {
  const versionsPath = path.join(ROOT, "docs", "versions.json");
  const map = new Map();
  try {
    const text = fs.readFileSync(versionsPath, "utf8").replace(/^\uFEFF/, "");
    const j = JSON.parse(text);
    for (const entry of Object.values(j)) {
      if (!entry || typeof entry !== "object") continue;
      const url = entry.nexus;
      if (!url || typeof url !== "string") continue;
      const m = url.match(/\/mods\/(\d+)/);
      if (!m) continue;
      const ts = entry.thunderstoreName;
      if (ts) map.set(ts, Number(m[1]));
    }
  } catch (e) {
    console.warn(`[stats] Could not read Nexus mod ids from docs/versions.json: ${e.message}`);
  }
  return map;
}

const MOD_ROSTER = [
  { id: "senpais-chest", name: "SenpaisChest", thunderstore: { namespace: "AzraelGodKing", name: "SenpaisChest" }, nexus: { game: "sunhaven", modId: null } },
  { id: "havens-todo", name: "Sun Haven Todo", thunderstore: { namespace: "AzraelGodKing", name: "SunHavenTodo" }, nexus: { game: "sunhaven", modId: null } },
  { id: "the-vault", name: "TheVault", thunderstore: { namespace: "AzraelGodKing", name: "TheVault" }, nexus: { game: "sunhaven", modId: null } },
  { id: "havens-birthright", name: "HavensBirthright", thunderstore: { namespace: "AzraelGodKing", name: "HavensBirthright" }, nexus: { game: "sunhaven", modId: null } },
  { id: "museum-utility-tracker", name: "Museum Utility Tracker", thunderstore: { namespace: "AzraelGodKing", name: "SMUT" }, nexus: { game: "sunhaven", modId: null } },
  { id: "haven-dev-tools", name: "HavenDevTools", thunderstore: { namespace: "AzraelGodKing", name: "HavenDevTools" }, nexus: { game: "sunhaven", modId: null } },
  { id: "squirrels-birthday-reminder", name: "SquirrelsBirthdayReminder", thunderstore: { namespace: "AzraelGodKing", name: "SquirrelsBirthdayReminder" }, nexus: { game: "sunhaven", modId: null } },
  { id: "havens-almanac", name: "HavensAlmanac", thunderstore: { namespace: "AzraelGodKing", name: "HavensAlmanac" }, nexus: { game: "sunhaven", modId: null } },
  { id: "crop-optimizer", name: "Crop Optimizer", thunderstore: { namespace: "AzraelGodKing", name: "CropOptimizer" }, nexus: { game: "sunhaven", modId: null } },
  { id: "faster-races", name: "FasterRaces", thunderstore: { namespace: "AzraelGodKing", name: "FasterRaces" }, nexus: { game: "sunhaven", modId: null } },
  { id: "trinket-fortune", name: "TrinketFortune", thunderstore: { namespace: "AzraelGodKing", name: "TrinketFortune" }, nexus: { game: "sunhaven", modId: null } },
  { id: "havens-respec", name: "HavensRespec", thunderstore: { namespace: "AzraelGodKing", name: "HavensRespec" }, nexus: { game: "sunhaven", modId: null } },
];

function utcDateString(date) {
  return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, "0")}-${String(date.getUTCDate()).padStart(2, "0")}`;
}

function ensureParentDir(filePath) {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
}

function readCache() {
  if (!fs.existsSync(CACHE_PATH)) {
    return { lastFetched: null, mods: {}, site_total: { thunderstore: 0, nexus_total: 0, nexus_unique: 0, grand_total: 0 } };
  }

  try {
    return JSON.parse(fs.readFileSync(CACHE_PATH, "utf8"));
  } catch (err) {
    console.warn(`[stats] Failed to parse cache, starting fresh: ${err.message}`);
    return { lastFetched: null, mods: {}, site_total: { thunderstore: 0, nexus_total: 0, nexus_unique: 0, grand_total: 0 } };
  }
}

async function fetchJson(url, options = {}) {
  const res = await fetch(url, options);
  if (!res.ok) {
    throw new Error(`HTTP ${res.status} from ${url}`);
  }
  return res.json();
}

function packageKey(namespace, packageName) {
  return `${namespace}/${packageName}`;
}

/** @param {unknown[]} packages */
function buildSunHavenPackageIndex(packages) {
  const map = new Map();
  for (const pkg of packages) {
    if (!pkg || typeof pkg !== "object") continue;
    const owner = /** @type {{ owner?: string; name?: string }} */ (pkg).owner;
    const name = /** @type {{ owner?: string; name?: string }} */ (pkg).name;
    if (!owner || !name) continue;
    map.set(packageKey(owner, name), pkg);
  }
  return map;
}

function thunderstoreStatsFromPackage(pkg) {
  const versions = {};
  let total = 0;
  for (const version of pkg?.versions || []) {
    const versionNumber = version?.version_number;
    if (!versionNumber) continue;
    const n = Number(version?.downloads || 0);
    versions[versionNumber] = n;
    total += n;
  }
  return { total_downloads: total, versions };
}

async function loadSunHavenThunderstoreIndex() {
  const url = `https://thunderstore.io/c/${THUNDERSTORE_SUN_HAVEN_COMMUNITY}/api/v1/package/`;
  const list = await fetchJson(url);
  if (!Array.isArray(list)) {
    throw new Error("Sun Haven community package list is not an array");
  }
  return buildSunHavenPackageIndex(list);
}

async function fetchThunderstore(mod, packageIndex) {
  const key = packageKey(mod.thunderstore.namespace, mod.thunderstore.name);
  const payload = packageIndex.get(key);
  if (!payload) {
    throw new Error(`Package not in Sun Haven community listing: ${key}`);
  }
  return thunderstoreStatsFromPackage(payload);
}

async function fetchNexus(mod) {
  const modId = mod?.nexus?.modId;
  if (modId == null) return null;

  const apiKey = process.env.NEXUSMODS_API_KEY;
  if (!apiKey) {
    return null;
  }

  const url = `https://api.nexusmods.com/v1/games/${encodeURIComponent(mod.nexus.game)}/mods/${modId}.json`;
  const payload = await fetchJson(url, { headers: { apikey: apiKey } });
  return {
    total_downloads: Number(payload?.mod_downloads || 0),
    unique_downloads: Number(payload?.mod_unique_downloads || 0),
  };
}

function getPreviousMod(cache, mod) {
  return cache?.mods?.[mod.id] || {
    name: mod.name,
    thunderstore: { total_downloads: 0, versions: {} },
    nexus: null,
    combined_total: 0,
  };
}

function resolveNexusModId(mod, nexusIdLookup) {
  if (mod?.nexus?.modId != null) return mod.nexus.modId;
  const fromVersions = nexusIdLookup.get(mod.thunderstore.name);
  return fromVersions != null ? fromVersions : null;
}

async function fetchModStats(mod, cache, packageIndex, nexusIdLookup) {
  const previous = getPreviousMod(cache, mod);
  const modForNexus = {
    ...mod,
    nexus: { ...mod.nexus, modId: resolveNexusModId(mod, nexusIdLookup) },
  };

  const [tsResult, nxResult] = await Promise.allSettled([
    fetchThunderstore(mod, packageIndex),
    fetchNexus(modForNexus),
  ]);

  const thunderstore =
    tsResult.status === "fulfilled"
      ? tsResult.value
      : previous.thunderstore || { total_downloads: 0, versions: {} };

  if (tsResult.status === "rejected") {
    console.warn(`[stats] Thunderstore fetch failed for ${mod.id}, preserving cached value: ${tsResult.reason?.message || tsResult.reason}`);
  }

  let nexus = null;
  if (modForNexus?.nexus?.modId == null) {
    nexus = previous.nexus ?? null;
  } else if (nxResult.status === "fulfilled") {
    const v = nxResult.value;
    if (v == null && !process.env.NEXUSMODS_API_KEY) {
      nexus = previous.nexus ?? null;
    } else {
      nexus = v;
    }
  } else {
    nexus = previous.nexus || { total_downloads: 0, unique_downloads: 0 };
    console.warn(`[stats] Nexus fetch failed for ${mod.id}, preserving cached value: ${nxResult.reason?.message || nxResult.reason}`);
  }

  const combinedTotal = Number(thunderstore?.total_downloads || 0) + Number(nexus?.total_downloads || 0);

  return {
    name: mod.name,
    thunderstore,
    nexus,
    combined_total: combinedTotal,
  };
}

function buildSiteTotal(modsMap) {
  let thunderstore = 0;
  let nexusTotal = 0;
  let nexusUnique = 0;

  for (const modStats of Object.values(modsMap)) {
    thunderstore += Number(modStats?.thunderstore?.total_downloads || 0);
    nexusTotal += Number(modStats?.nexus?.total_downloads || 0);
    nexusUnique += Number(modStats?.nexus?.unique_downloads || 0);
  }

  return {
    thunderstore,
    nexus_total: nexusTotal,
    nexus_unique: nexusUnique,
    grand_total: thunderstore + nexusTotal,
  };
}

function writeCacheAtomic(data) {
  ensureParentDir(CACHE_PATH);
  fs.writeFileSync(TMP_PATH, `${JSON.stringify(data, null, 2)}\n`, "utf8");
  fs.renameSync(TMP_PATH, CACHE_PATH);
}

function isForceRefresh() {
  const argv = process.argv.slice(2);
  if (argv.includes("--force") || argv.includes("-f")) return true;
  const v = String(process.env.STATS_FORCE || "").trim().toLowerCase();
  return v === "1" || v === "true" || v === "yes";
}

async function main() {
  const force = isForceRefresh();
  const cache = readCache();
  const now = new Date();
  if (!force && cache?.lastFetched) {
    const last = new Date(cache.lastFetched);
    if (!Number.isNaN(last.valueOf()) && utcDateString(last) === utcDateString(now)) {
      console.log("Stats already up to date (same UTC day). Use --force or STATS_FORCE=1 to refresh anyway.");
      return;
    }
  }
  if (force) {
    console.log("[stats] Force refresh: bypassing same-day short-circuit");
  }

  const packageIndex = await loadSunHavenThunderstoreIndex();
  const nexusIdLookup = loadNexusModIdByThunderstoreName();
  if (nexusIdLookup.size > 0 && !process.env.NEXUSMODS_API_KEY) {
    console.warn(
      "[stats] NEXUSMODS_API_KEY is not set; Nexus totals will stay empty or unchanged until you run with the key (local .env or CI secret)."
    );
  }
  const modsEntries = await Promise.all(
    MOD_ROSTER.map(async (mod) => [mod.id, await fetchModStats(mod, cache, packageIndex, nexusIdLookup)])
  );
  const mods = Object.fromEntries(modsEntries);

  const next = {
    lastFetched: now.toISOString(),
    mods,
    site_total: buildSiteTotal(mods),
  };

  writeCacheAtomic(next);
  console.log(`[stats] Updated ${CACHE_PATH}`);
}

main().catch((err) => {
  console.error("[stats] Fatal error:", err);
  process.exitCode = 1;
});
