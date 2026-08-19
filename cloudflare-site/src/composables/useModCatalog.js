import { computed, ref } from "vue";
import { MOD_META, resolveStatsId } from "../data/mods.js";
import { MOD_MATRIX_URL, STATS_CACHE_URL } from "../data/urls.js";

const mods = ref([]);
const siteTotal = ref({
  thunderstore: null,
  nexus_total: null,
  nexus_unique: null,
  grand_total: null,
});
const lastFetched = ref(null);
const loading = ref(false);
const error = ref("");
let loadPromise = null;

async function loadCatalog() {
  if (mods.value.length && !error.value) return;
  if (loadPromise) return loadPromise;

  loading.value = true;
  error.value = "";
  loadPromise = (async () => {
    try {
      const [matrixRes, statsRes] = await Promise.all([
        fetch(MOD_MATRIX_URL, { cache: "no-cache" }),
        fetch(STATS_CACHE_URL, { cache: "no-cache" }),
      ]);
      if (!matrixRes.ok) throw new Error(`mod-matrix: HTTP ${matrixRes.status}`);
      if (!statsRes.ok) throw new Error(`stats-cache: HTTP ${statsRes.status}`);

      const matrix = await matrixRes.json();
      const stats = await statsRes.json();
      if (!Array.isArray(matrix)) throw new Error("mod-matrix.json was not an array");

      lastFetched.value = stats?.lastFetched ?? null;
      const site = stats?.site_total || {};
      siteTotal.value = {
        thunderstore: site.thunderstore ?? null,
        nexus_total: site.nexus_total ?? null,
        nexus_unique: site.nexus_unique ?? null,
        grand_total: site.grand_total ?? null,
      };

      const byId = stats?.mods || {};
      mods.value = matrix
        .filter((row) => row && row.modKey && row.thunderstoreName)
        .map((row) => {
          const statsId = resolveStatsId(row.modKey);
          const mod = byId[statsId] || {};
          const ts = mod.thunderstore?.total_downloads;
          const nx = mod.nexus?.total_downloads;
          const combined = mod.combined_total;
          return {
            modKey: row.modKey,
            name: row.indexDataName || row.modDir || row.modKey,
            thunderstoreName: row.thunderstoreName,
            docsPagePath: row.docsPagePath || null,
            jsonKey: row.jsonKey || null,
            statsId,
            icon: MOD_META[row.modKey]?.icon || "✨",
            lane: MOD_META[row.modKey]?.lane || "QoL",
            ts: ts != null ? Number(ts) : null,
            nx: nx != null ? Number(nx) : null,
            nxUnique:
              mod.nexus?.unique_downloads != null
                ? Number(mod.nexus.unique_downloads)
                : null,
            combined: combined != null ? Number(combined) : null,
          };
        });
    } catch (e) {
      error.value = e instanceof Error ? e.message : String(e);
      mods.value = [];
    } finally {
      loading.value = false;
      loadPromise = null;
    }
  })();

  return loadPromise;
}

export function useModCatalog() {
  const rankedMods = computed(() =>
    [...mods.value].sort(
      (a, b) => Number(b.combined ?? -1) - Number(a.combined ?? -1)
    )
  );

  return {
    mods,
    siteTotal,
    lastFetched,
    loading,
    error,
    rankedMods,
    loadCatalog,
  };
}
