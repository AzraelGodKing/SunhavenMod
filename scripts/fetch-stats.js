const fs = require("fs");
const path = require("path");

require("dotenv").config();

const ROOT = path.resolve(__dirname, "..");
const CACHE_PATH = path.join(ROOT, "data", "stats-cache.json");
const TMP_PATH = `${CACHE_PATH}.tmp`;

const THUNDERSTORE_SUN_HAVEN_COMMUNITY = "sun-haven";

const MOD_ROSTER = [
  { id: "senpais-chest", name: "SenpaisChest", thunderstore: { namespace: "AzraelGodKing", name: "SenpaisChest" }, nexus: { game: "sunhaven", modId: null } },
  { id: "havens-todo", name: "Sun Haven Todo", thunderstore: { namespace: "AzraelGodKing", name: "SunHavenTodo" }, nexus: { game: "sunhaven", modId: null } },
  { id: "the-vault", name: "TheVault", thunderstore: { namespace: "AzraelGodKing", name: "TheVault" }, nexus: { game: "sunhaven", modId: null } },
  { id: "havens-birthright", name: "HavensBirthright", thunderstore: { namespace: "AzraelGodKing", name: "HavensBirthright" }, nexus: { game: "sunhaven", modId: null } },
  { id: "museum-utility-tracker", name: "Museum Utility Tracker", thunderstore: { namespace: "AzraelGodKing", name: "SMUT" }, nexus: { game: "sunhaven", modId: null } },
  { id: "haven-dev-tools", name: "HavenDevTools", thunderstore: { namespace: "AzraelGodKing", name: "HavenDevTools" }, nexus: { game: "sunhaven", modId: null } },
  { id: "squirrels-birthday-reminder", name: "SquirrelsBirthdayReminder", thunderstore: { namespace: "AzraelGodKing", name: "SquirrelsBirthdayReminder" }, nexus: { game: "sunhaven", modId: null } },
  { id: "havens-almanac", name: "HavensAlmanac", thunderstore: { namespace: "AzraelGodKing", name: "HavensAlmanac" }, nexus: { game: "sunhaven", modId: null } },
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

  const apiKey = process.env.NEXUS_API_KEY;
  if (!apiKey) {
    throw new Error("NEXUS_API_KEY missing");
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

async function fetchModStats(mod, cache, packageIndex) {
  const previous = getPreviousMod(cache, mod);

  const [tsResult, nxResult] = await Promise.allSettled([
    fetchThunderstore(mod, packageIndex),
    fetchNexus(mod),
  ]);

  const thunderstore =
    tsResult.status === "fulfilled"
      ? tsResult.value
      : previous.thunderstore || { total_downloads: 0, versions: {} };

  if (tsResult.status === "rejected") {
    console.warn(`[stats] Thunderstore fetch failed for ${mod.id}, preserving cached value: ${tsResult.reason?.message || tsResult.reason}`);
  }

  let nexus = null;
  if (mod?.nexus?.modId == null) {
    nexus = previous.nexus ?? null;
  } else if (nxResult.status === "fulfilled") {
    nexus = nxResult.value;
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

async function main() {
  const cache = readCache();
  const now = new Date();
  if (cache?.lastFetched) {
    const last = new Date(cache.lastFetched);
    if (!Number.isNaN(last.valueOf()) && utcDateString(last) === utcDateString(now)) {
      console.log("Stats already up to date");
      return;
    }
  }

  const packageIndex = await loadSunHavenThunderstoreIndex();
  const modsEntries = await Promise.all(
    MOD_ROSTER.map(async (mod) => [mod.id, await fetchModStats(mod, cache, packageIndex)])
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
