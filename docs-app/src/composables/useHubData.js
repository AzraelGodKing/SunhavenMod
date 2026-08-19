import { ref } from "vue";

const stats = ref(null);
const versions = ref(null);
const loading = ref(false);
const error = ref("");
let loadPromise = null;

function assetUrl(path) {
  // Works with Vite base `/SunhavenMod/` in prod and `/` in local merge preview
  const base = import.meta.env.BASE_URL || "/";
  return `${base}${String(path).replace(/^\//, "")}`;
}

export function useHubData() {
  async function load() {
    if (stats.value && versions.value) return;
    if (loadPromise) return loadPromise;
    loading.value = true;
    error.value = "";
    loadPromise = (async () => {
      try {
        const [statsRes, versionsRes] = await Promise.all([
          fetch(assetUrl("data/stats-cache.json"), { cache: "no-cache" }),
          fetch(assetUrl("versions.json"), { cache: "no-cache" }),
        ]);
        if (!statsRes.ok) throw new Error(`stats-cache: HTTP ${statsRes.status}`);
        if (!versionsRes.ok) throw new Error(`versions: HTTP ${versionsRes.status}`);
        stats.value = await statsRes.json();
        versions.value = await versionsRes.json();
      } catch (e) {
        error.value = e instanceof Error ? e.message : String(e);
      } finally {
        loading.value = false;
        loadPromise = null;
      }
    })();
    return loadPromise;
  }

  return { stats, versions, loading, error, load, assetUrl };
}

export function formatInt(value) {
  if (value == null) return "—";
  const n = Number(value);
  if (!Number.isFinite(n)) return "—";
  return n.toLocaleString();
}

export function formatNexus(total, unique) {
  const t = formatInt(total);
  const u = formatInt(unique);
  if (t === "—" && u === "—") return "—";
  return `${t} (${u})`;
}
