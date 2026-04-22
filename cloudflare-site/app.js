import { createApp } from "https://esm.sh/vue@3.5.13/dist/vue.esm-browser.prod.js";

/** Same URLs as your public GitHub Pages + raw repo content (adjust if repo rename). */
const STATS_CACHE_URL =
  "https://azraelgodking.github.io/SunhavenMod/data/stats-cache.json";
const MOD_MATRIX_URL =
  "https://raw.githubusercontent.com/AzraelGodKing/SunhavenMod/main/scripts/mod-matrix.json";

function slugify(value) {
  return String(value || "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

/**
 * Stable cache key per mod — must stay aligned with `scripts/fetch-stats.js`
 * (`STATS_ID_BY_MOD_KEY` + `slugify` fallback).
 */
const STATS_ID_BY_MOD_KEY = {
  senpaischest: "senpais-chest",
  havensbirthright: "havens-birthright",
  sunhavenmuseumutilitytracker: "museum-utility-tracker",
  squirrelsbirthdayreminder: "squirrels-birthday-reminder",
  sunhaventodo: "havens-todo",
  thevault: "the-vault",
  havendevtools: "haven-dev-tools",
  havensalmanac: "havens-almanac",
  fasterraces: "faster-races",
  trinketfortune: "trinket-fortune",
  cropoptimizer: "crop-optimizer",
  havensrespec: "havens-respec",
};

function resolveStatsId(modKey) {
  const k = String(modKey || "");
  if (STATS_ID_BY_MOD_KEY[k]) return STATS_ID_BY_MOD_KEY[k];
  return slugify(k);
}

function formatInt(value) {
  if (value == null) return "—";
  const n = Number(value);
  if (!Number.isFinite(n)) return "—";
  return n.toLocaleString();
}

function formatWhen(iso) {
  if (!iso) return "unknown";
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return String(iso);
  }
}

createApp({
  data() {
    return {
      loading: true,
      error: "",
      lastFetched: null,
      siteTotal: {
        thunderstore: null,
        nexus_total: null,
        nexus_unique: null,
        grand_total: null,
      },
      mods: [],
      query: "",
      sortKey: "combined",
    };
  },
  computed: {
    filteredMods() {
      const q = this.query.trim().toLowerCase();
      let rows = this.mods;
      if (q) {
        rows = rows.filter((m) => {
          const hay = `${m.name} ${m.modKey} ${m.statsId}`.toLowerCase();
          return hay.includes(q);
        });
      }
      const key = this.sortKey;
      const score = (m) => {
        if (key === "name") return m.name || m.modKey;
        if (key === "thunderstore") return m.ts ?? -1;
        if (key === "nexus") return m.nx ?? -1;
        return m.combined ?? -1;
      };
      return [...rows].sort((a, b) => {
        const sa = score(a);
        const sb = score(b);
        if (typeof sa === "string" && typeof sb === "string") {
          return sa.localeCompare(sb);
        }
        return Number(sb) - Number(sa);
      });
    },
  },
  async mounted() {
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

      this.lastFetched = stats?.lastFetched ?? null;
      const site = stats?.site_total || {};
      this.siteTotal = {
        thunderstore: site.thunderstore ?? null,
        nexus_total: site.nexus_total ?? null,
        nexus_unique: site.nexus_unique ?? null,
        grand_total: site.grand_total ?? null,
      };

      const byId = stats?.mods || {};
      this.mods = matrix
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
            statsId,
            ts: ts != null ? Number(ts) : null,
            nx: nx != null ? Number(nx) : null,
            combined: combined != null ? Number(combined) : null,
          };
        });
    } catch (e) {
      this.error = e instanceof Error ? e.message : String(e);
    } finally {
      this.loading = false;
    }
  },
  template: `
    <div>
      <header class="shell-header">
        <div class="container">
          <div class="shell-top">
            <div>
              <p class="eyebrow">SunhavenMod</p>
              <h1>Download Pulse</h1>
              <p class="lead">
                Pulled from the same JSON cache as the docs hub
                (<code>docs/data/stats-cache.json</code> → GitHub Pages
                <code>/data/stats-cache.json</code>).
              </p>
            </div>
            <div class="pill" v-if="!loading && !error">
              <span class="pill-label">Cache refreshed</span>
              <span class="pill-value">{{ formatWhen(lastFetched) }}</span>
            </div>
          </div>

          <div class="totals" v-if="!loading && !error">
            <div class="total">
              <span class="total-label">Combined</span>
              <span class="total-value">{{ formatInt(siteTotal.grand_total) }}</span>
            </div>
            <div class="total">
              <span class="total-label">Thunderstore</span>
              <span class="total-value">{{ formatInt(siteTotal.thunderstore) }}</span>
            </div>
            <div class="total">
              <span class="total-label">Nexus (total)</span>
              <span class="total-value">{{ formatInt(siteTotal.nexus_total) }}</span>
            </div>
            <div class="total">
              <span class="total-label">Nexus (unique)</span>
              <span class="total-value">{{ formatInt(siteTotal.nexus_unique) }}</span>
            </div>
          </div>
        </div>
      </header>

      <main class="container dashboard">
        <p v-if="loading" class="state">Loading stats…</p>
        <p v-else-if="error" class="state error">Could not load stats: {{ error }}</p>

        <template v-else>
          <div class="toolbar card">
            <label class="field">
              <span>Search</span>
              <input v-model.trim="query" type="search" placeholder="Filter by mod name or key…" />
            </label>
            <label class="field">
              <span>Sort</span>
              <select v-model="sortKey">
                <option value="combined">Combined downloads</option>
                <option value="thunderstore">Thunderstore downloads</option>
                <option value="nexus">Nexus downloads</option>
                <option value="name">Name (A–Z)</option>
              </select>
            </label>
          </div>

          <div class="mod-grid">
            <article v-for="m in filteredMods" :key="m.statsId" class="mod-card">
              <div class="mod-head">
                <div>
                  <h2>{{ m.name }}</h2>
                  <p class="meta">{{ m.thunderstoreName }} · <code>{{ m.statsId }}</code></p>
                </div>
                <p class="big-number">{{ formatInt(m.combined) }}</p>
              </div>
              <dl class="metrics">
                <div>
                  <dt>Thunderstore</dt>
                  <dd>{{ formatInt(m.ts) }}</dd>
                </div>
                <div>
                  <dt>Nexus</dt>
                  <dd>{{ formatInt(m.nx) }}</dd>
                </div>
              </dl>
            </article>
          </div>
        </template>
      </main>
    </div>
  `,
  methods: { formatInt, formatWhen },
}).mount("#app");
