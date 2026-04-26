import { createApp, nextTick } from "https://esm.sh/vue@3.5.13/dist/vue.esm-browser.prod.js";
import { animate, stagger } from "https://esm.sh/motion@12.23.24";

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

const MOD_META = {
  senpaischest: { icon: "📦", lane: "Storage" },
  havensbirthright: { icon: "⚔️", lane: "Races" },
  sunhavenmuseumutilitytracker: { icon: "🏛️", lane: "Tracking" },
  squirrelsbirthdayreminder: { icon: "🎂", lane: "Social" },
  sunhaventodo: { icon: "📝", lane: "Planning" },
  thevault: { icon: "🔒", lane: "Currency" },
  havendevtools: { icon: "🛠️", lane: "Dev" },
  havensalmanac: { icon: "📖", lane: "Dashboard" },
  fasterraces: { icon: "🏃", lane: "Movement" },
  trinketfortune: { icon: "🎣", lane: "Fishing" },
  cropoptimizer: { icon: "🌱", lane: "Farming" },
  havensrespec: { icon: "📜", lane: "Skills" },
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
    const raw = document.documentElement.dataset.page || "landing";
    const page = raw === "downloads" || raw === "feedback" ? raw : "landing";
    return {
      page,
      loading: true,
      error: "",
      ambientEnabled: true,
      lastFetched: null,
      siteTotal: {
        thunderstore: null,
        nexus_unique: null,
        grand_total: null,
      },
      mods: [],
      showAllLandingMods: false,
      query: "",
      sortKey: "combined",
      selectedLane: "all",
      form: {
        type: "bug",
        name: "",
        title: "",
        description: "",
        website: "",
      },
      formStatus: "idle",
      formNotice: "",
    };
  },
  computed: {
    isLanding() {
      return this.page === "landing";
    },
    isDownloads() {
      return this.page === "downloads";
    },
    isFeedback() {
      return this.page === "feedback";
    },
    rankedMods() {
      return [...this.mods].sort((a, b) => Number(b.combined ?? -1) - Number(a.combined ?? -1));
    },
    featuredMods() {
      return this.rankedMods.slice(0, 4);
    },
    landingMods() {
      if (this.showAllLandingMods) return this.rankedMods;
      return this.rankedMods.slice(0, 8);
    },
    topCombinedValue() {
      if (this.rankedMods.length === 0) return 0;
      return Number(this.rankedMods[0].combined ?? 0);
    },
    topMovers() {
      return this.rankedMods.slice(0, 3);
    },
    totalMods() {
      return this.mods.length;
    },
    laneOptions() {
      const lanes = Array.from(new Set(this.mods.map((m) => m.lane).filter(Boolean))).sort();
      return ["all", ...lanes];
    },
    filteredMods() {
      const q = this.query.trim().toLowerCase();
      let rows = this.mods;
      if (this.selectedLane !== "all") {
        rows = rows.filter((m) => m.lane === this.selectedLane);
      }
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
    reducedMotion() {
      return typeof window !== "undefined" &&
        typeof window.matchMedia === "function" &&
        window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    },
  },
  async mounted() {
    if (this.isFeedback) {
      this.loading = false;
      return;
    }
    try {
      const saved = localStorage.getItem("sunhavenmod-ambient-enabled");
      if (saved === "0") this.ambientEnabled = false;
    } catch {
      // Ignore storage failures; default stays enabled.
    }
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
          const nx = mod.nexus?.unique_downloads;
          const combined = mod.combined_total;
          return {
            modKey: row.modKey,
            name: row.indexDataName || row.modDir || row.modKey,
            thunderstoreName: row.thunderstoreName,
            statsId,
            icon: MOD_META[row.modKey]?.icon || "✨",
            lane: MOD_META[row.modKey]?.lane || "QoL",
            ts: ts != null ? Number(ts) : null,
            nx: nx != null ? Number(nx) : null,
            combined: combined != null ? Number(combined) : null,
          };
        });
    } catch (e) {
      this.error = e instanceof Error ? e.message : String(e);
    } finally {
      this.loading = false;
      await nextTick();
      this.runPageAnimations();
    }
  },
  template: `
    <div :class="{ 'landing-theme': isLanding, 'downloads-theme': isDownloads || isFeedback, 'no-ambient': !ambientEnabled }">
      <nav class="site-nav">
        <div class="container site-nav-inner">
          <a class="site-nav-brand" href="./index.html">SunhavenMod</a>
          <div class="site-nav-links">
            <a href="./index.html" :class="{ active: isLanding }">Home</a>
            <a href="./downloads.html" :class="{ active: isDownloads }">Downloads</a>
            <a href="./feedback.html" :class="{ active: isFeedback }">Feedback</a>
            <button class="ambient-toggle" type="button" @click="toggleAmbient">
              {{ ambientEnabled ? 'Ambient: On' : 'Ambient: Off' }}
            </button>
          </div>
        </div>
      </nav>

      <div v-if="isLanding" class="page-anim page-anim-landing" aria-hidden="true">
        <span></span><span></span><span></span>
      </div>
      <div v-else class="page-anim page-anim-downloads" aria-hidden="true">
        <span></span><span></span><span></span>
      </div>

      <header class="shell-header">
        <div class="container">
          <div class="shell-top">
            <div>
              <p class="eyebrow">SunhavenMod</p>
              <h1 v-if="isLanding">The Guild Hall for your modded run</h1>
              <h1 v-else-if="isDownloads">Operations board: live download command</h1>
              <h1 v-else>Report a Bug / Request a Feature</h1>
              <p class="lead" v-if="isLanding">
                A handcrafted portal for every SunhavenMod project. Pick by playstyle, inspect live momentum,
                and jump into deeply themed per-mod pages.
              </p>
              <p class="lead" v-else-if="isDownloads">
                Fast comparisons, lane grouping, and ranking snapshots powered by shared cache data.
                Return to <a href="./index.html">Home</a> for curated discovery.
              </p>
              <p class="lead" v-else>
                Share feedback directly from the site. Submissions stay in-page and are sent
                securely to our tracker in the background.
              </p>
              <p class="cta-inline" v-if="isLanding">
                <a class="btn btn-primary" href="./downloads.html">Open command board</a>
                <a class="btn btn-ghost" href="https://thunderstore.io/c/sun-haven/" target="_blank" rel="noopener">Visit Thunderstore</a>
              </p>
            </div>
          </div>

          <div class="totals" v-if="!isFeedback && !loading && !error">
            <div class="total">
              <span class="total-label">Guild total</span>
              <span class="total-value">{{ formatInt(siteTotal.grand_total) }}</span>
            </div>
            <div class="total">
              <span class="total-label">Thunderstore</span>
              <span class="total-value">{{ formatInt(siteTotal.thunderstore) }}</span>
            </div>
            <div class="total">
              <span class="total-label">Nexus unique</span>
              <span class="total-value">{{ formatInt(siteTotal.nexus_unique) }}</span>
            </div>
            <div class="total">
              <span class="total-label">Published mods</span>
              <span class="total-value">{{ formatInt(totalMods) }}</span>
            </div>
          </div>
        </div>
      </header>

      <main class="container dashboard">
        <p v-if="loading" class="state">Loading stats…</p>
        <p v-else-if="error" class="state error">Could not load stats: {{ error }}</p>

        <template v-else-if="isLanding">
          <section class="landing-grid animate__animated animate__fadeIn">
            <article class="card spotlight">
              <p class="section-kicker">Guild notice</p>
              <h2>Build your loadout by lane, not guesswork.</h2>
              <p>
                Every mod page is themed to its gameplay role. Start with featured picks,
                then browse the full registry and open detail pages that match each mod's vibe.
              </p>
              <ul class="feature-list">
                <li>Unique per-mod profile pages with role-based visuals</li>
                <li>Colorblind-friendly contrast and explicit labels</li>
                <li>Shared telemetry source across docs and Cloudflare pages</li>
              </ul>
            </article>

            <article class="card spotlight">
              <p class="section-kicker">Hot board</p>
              <h2>Current top movers</h2>
              <div class="mover-list">
                <a v-for="m in topMovers" :key="'mover-' + m.statsId" class="mover-row card-link" :href="modPageHref(m.modKey)">
                  <span>{{ m.icon }} {{ m.name }}</span>
                  <strong>{{ formatInt(m.combined) }}</strong>
                </a>
              </div>
              <dl class="snapshot-list">
                <div>
                  <dt>Guild total</dt>
                  <dd>{{ formatInt(siteTotal.grand_total) }}</dd>
                </div>
                <div>
                  <dt>Thunderstore total</dt>
                  <dd>{{ formatInt(siteTotal.thunderstore) }}</dd>
                </div>
                <div>
                  <dt>Nexus unique total</dt>
                  <dd>{{ formatInt(siteTotal.nexus_unique) }}</dd>
                </div>
              </dl>
              <p class="subtle">Last refresh: {{ formatWhen(lastFetched) }}</p>
            </article>
          </section>

          <section class="loadout-showcase card">
            <div class="loadout-head">
              <p class="section-kicker">Starter bundles</p>
              <h2>Pick a ready-made playstyle stack</h2>
            </div>
            <div class="loadout-grid">
              <article class="loadout-card">
                <h3>Collector Stack</h3>
                <p class="meta">Museum and completion flow</p>
                <p class="loadout-links">
                  <a :href="modPageHref('sunhavenmuseumutilitytracker')">S.M.U.T.</a>
                  <a :href="modPageHref('trinketfortune')">Trinket Fortune</a>
                  <a :href="modPageHref('senpaischest')">Senpai's Chest</a>
                </p>
              </article>
              <article class="loadout-card">
                <h3>Farm Planner Stack</h3>
                <p class="meta">Planning and crop optimization</p>
                <p class="loadout-links">
                  <a :href="modPageHref('cropoptimizer')">Crop Optimizer</a>
                  <a :href="modPageHref('sunhaventodo')">Sun Haven Todo</a>
                  <a :href="modPageHref('havensalmanac')">Haven's Almanac</a>
                </p>
              </article>
              <article class="loadout-card">
                <h3>Power Tuning Stack</h3>
                <p class="meta">Build shaping and pace control</p>
                <p class="loadout-links">
                  <a :href="modPageHref('havensbirthright')">Haven's Birthright</a>
                  <a :href="modPageHref('fasterraces')">Faster Races</a>
                  <a :href="modPageHref('havensrespec')">Haven's Respec</a>
                </p>
              </article>
            </div>
          </section>

          <section class="featured-block animate__animated animate__fadeInUp">
            <div class="featured-head">
              <p class="section-kicker">Featured contracts</p>
              <h2>Curated high-traction projects</h2>
            </div>
            <div class="featured-grid">
              <article v-for="m in featuredMods" :key="m.statsId" class="featured-card" :class="laneClass(m.lane)">
                <div class="featured-top">
                  <h3><a class="card-link" :href="modPageHref(m.modKey)">{{ m.icon }} {{ m.name }}</a></h3>
                  <span class="featured-total">{{ formatInt(m.combined) }}</span>
                </div>
                <p class="meta">{{ m.lane }} lane · <code>{{ m.statsId }}</code></p>
                <div class="featured-meter">
                  <span class="featured-fill" :style="{ width: relativeWidth(m) }"></span>
                </div>
                <dl class="metrics compact">
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
          </section>

          <section class="all-mods-block">
            <div class="all-mods-head">
              <div>
                <p class="section-kicker">Registry</p>
                <h2>Browse all published mods from home base</h2>
              </div>
              <button class="btn btn-ghost" type="button" @click="showAllLandingMods = !showAllLandingMods">
                {{ showAllLandingMods ? 'Show fewer mods' : 'Show all mods' }}
              </button>
            </div>
            <div class="all-mods-grid">
              <article v-for="m in landingMods" :key="'landing-' + m.statsId" class="all-mods-card" :class="laneClass(m.lane)">
                <h3><a class="card-link" :href="modPageHref(m.modKey)">{{ m.icon }} {{ m.name }}</a></h3>
                <p class="meta">{{ m.lane }} lane</p>
                <p class="all-mods-total">{{ formatInt(m.combined) }}</p>
              </article>
            </div>
          </section>
        </template>

        <template v-else-if="isDownloads">
          <section class="downloads-hero card">
            <div>
              <p class="section-kicker">Command board</p>
              <h2>Compare every mod in one tactical view</h2>
              <p class="meta">Sort, search, and jump into themed profile pages from here.</p>
            </div>
            <div class="downloads-badges">
              <span>Visual-first cards</span>
              <span>Lane filtering</span>
              <span>Direct mod profiles</span>
            </div>
          </section>

          <div class="toolbar card">
            <label class="field">
              <span>Search</span>
              <input v-model.trim="query" type="search" placeholder="Filter by mod name or key…" />
            </label>
            <label class="field">
              <span>Lane</span>
              <select v-model="selectedLane">
                <option v-for="lane in laneOptions" :key="lane" :value="lane">
                  {{ lane === 'all' ? 'All lanes' : lane }}
                </option>
              </select>
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
            <article v-for="m in filteredMods" :key="m.statsId" class="mod-card" :class="laneClass(m.lane)">
              <div class="mod-head">
                <div>
                  <h2><a class="card-link" :href="modPageHref(m.modKey)">{{ m.icon }} {{ m.name }}</a></h2>
                  <p class="meta">{{ m.lane }} lane · <code>{{ m.statsId }}</code></p>
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
          <p class="results-meta">Showing {{ filteredMods.length }} mod{{ filteredMods.length === 1 ? '' : 's' }}</p>
        </template>

        <template v-else>
          <section class="downloads-hero card">
            <div>
              <p class="section-kicker">Feedback desk</p>
              <h2>Tell us what broke or what should exist next</h2>
              <p class="meta">No redirects. Submit inline and keep browsing.</p>
            </div>
          </section>
        </template>
      </main>

      <section id="feedback" class="container feedback-wrap" v-if="isFeedback">
        <div class="card feedback-card">
          <p class="section-kicker">Report a Bug</p>
          <h2>Send bug reports or feature requests without leaving the site</h2>
          <p class="meta">
            Submissions are sent in the background to our issue tracker.
            You stay on this page the whole time.
          </p>

          <form class="feedback-form" @submit.prevent="submitFeedback" novalidate>
            <label class="field">
              <span>Type</span>
              <select v-model="form.type" :disabled="formStatus === 'submitting'">
                <option value="bug">Bug</option>
                <option value="feature">Feature</option>
              </select>
            </label>

            <label class="field">
              <span>Name</span>
              <input v-model.trim="form.name" autocomplete="name" required maxlength="120" :disabled="formStatus === 'submitting'" />
            </label>

            <label class="field">
              <span>Title</span>
              <input v-model.trim="form.title" required maxlength="160" :disabled="formStatus === 'submitting'" />
            </label>

            <label class="field">
              <span>Description</span>
              <textarea v-model.trim="form.description" rows="4" required maxlength="4000" :disabled="formStatus === 'submitting'"></textarea>
            </label>

            <!-- Honeypot: should stay empty -->
            <label class="honeypot" aria-hidden="true">
              <span>Leave this field empty</span>
              <input v-model="form.website" tabindex="-1" autocomplete="off" />
            </label>

            <div class="feedback-actions">
              <button class="btn btn-primary" type="submit" :disabled="formStatus === 'submitting'">
                {{ formStatus === 'submitting' ? 'Submitting…' : 'Submit feedback' }}
              </button>
              <p class="feedback-inline" role="status" aria-live="polite" v-if="formNotice">
                {{ formNotice }}
              </p>
            </div>
          </form>
        </div>
      </section>
    </div>
  `,
  methods: {
    formatInt,
    formatWhen,
    relativeWidth(mod) {
      const top = Number(this.topCombinedValue || 0);
      const value = Number(mod?.combined || 0);
      if (!top || !Number.isFinite(top) || top <= 0) return "0%";
      const pct = Math.max(8, Math.min(100, (value / top) * 100));
      return `${pct}%`;
    },
    modPageHref(modKey) {
      return `./mods/${modKey}.html`;
    },
    laneClass(lane) {
      if (!lane) return "lane-generic";
      return `lane-${String(lane).toLowerCase().replace(/[^a-z0-9]+/g, "-")}`;
    },
    runPageAnimations() {
      if (this.reducedMotion) return;
      animate(".shell-header", { opacity: [0.84, 1], y: [10, 0] }, { duration: 0.45, easing: "ease-out" });
      animate(".totals .total", { opacity: [0.2, 1], y: [8, 0] }, { delay: stagger(0.05), duration: 0.35 });
      if (this.isLanding) {
        animate(".landing-grid > *", { opacity: [0.1, 1], y: [14, 0] }, { delay: stagger(0.07), duration: 0.4 });
        animate(".loadout-grid > *", { opacity: [0.1, 1], y: [12, 0] }, { delay: stagger(0.06), duration: 0.35 });
      } else {
        animate(".downloads-hero", { opacity: [0.15, 1], y: [14, 0] }, { duration: 0.45 });
        animate(".toolbar", { opacity: [0.2, 1], y: [10, 0] }, { duration: 0.35, delay: 0.06 });
        animate(".mod-grid .mod-card", { opacity: [0.05, 1], y: [12, 0] }, { delay: stagger(0.03), duration: 0.3 });
      }
    },
    toggleAmbient() {
      this.ambientEnabled = !this.ambientEnabled;
      try {
        localStorage.setItem("sunhavenmod-ambient-enabled", this.ambientEnabled ? "1" : "0");
      } catch {
        // Ignore storage failures.
      }
      if (this.ambientEnabled) this.runPageAnimations();
    },
    resetFeedbackForm() {
      this.form.name = "";
      this.form.title = "";
      this.form.description = "";
      this.form.website = "";
    },
    async submitFeedback() {
      this.formNotice = "";
      const required = [this.form.name, this.form.title, this.form.description];
      if (required.some((v) => !String(v || "").trim())) {
        this.formStatus = "error";
        this.formNotice = "Please complete name, title, and description.";
        return;
      }
      this.formStatus = "submitting";
      try {
        const res = await fetch("/api/feedback", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(this.form),
        });
        const payload = await res.json().catch(() => ({}));
        if (!res.ok) {
          throw new Error(payload?.error || `Request failed (${res.status})`);
        }
        this.formStatus = "success";
        this.formNotice = "Thanks! Your feedback was submitted successfully.";
        this.resetFeedbackForm();
      } catch (err) {
        this.formStatus = "error";
        this.formNotice = err instanceof Error ? err.message : "Could not submit feedback right now.";
      }
    },
  },
}).mount("#app");
