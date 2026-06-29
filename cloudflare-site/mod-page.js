import { createApp, nextTick } from "https://esm.sh/vue@3.5.13/dist/vue.esm-browser.prod.js";
import { animate, stagger } from "https://esm.sh/motion@12.23.24";

const STATS_CACHE_URL =
  "https://azraelgodking.github.io/SunhavenMod/data/stats-cache.json";
const MOD_MATRIX_URL =
  "https://raw.githubusercontent.com/AzraelGodKing/SunhavenMod/main/scripts/mod-matrix.json";
const VERSIONS_URL =
  "https://raw.githubusercontent.com/AzraelGodKing/SunhavenMod/main/docs/versions.json";

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
  giftingassistant: "gifting-assistant",
};

const MOD_PRESENTATION = {
  senpaischest: {
    icon: "📦",
    status: "Stable",
    tags: ["QoL", "Storage", "Automation"],
    related: ["sunhavenmuseumutilitytracker", "sunhaventodo", "thevault"],
  },
  havensbirthright: {
    icon: "⚔️",
    status: "Actively Maintained",
    tags: ["QoL", "Combat", "Races"],
    related: ["fasterraces", "havensrespec", "havensalmanac"],
  },
  sunhavenmuseumutilitytracker: {
    icon: "🏛️",
    status: "Actively Maintained",
    tags: ["Tracking", "Museum", "UI"],
    related: ["senpaischest", "trinketfortune", "havensalmanac"],
  },
  squirrelsbirthdayreminder: {
    icon: "🎂",
    status: "Stable",
    tags: ["Social", "Reminder", "QoL"],
    related: ["sunhaventodo", "havensalmanac", "senpaischest"],
  },
  sunhaventodo: {
    icon: "📝",
    status: "Stable",
    tags: ["UI", "Tracking", "Planning"],
    related: ["squirrelsbirthdayreminder", "sunhavenmuseumutilitytracker", "havensalmanac"],
  },
  thevault: {
    icon: "🔒",
    status: "Stable",
    tags: ["Storage", "Currency", "QoL"],
    related: ["senpaischest", "sunhavenmuseumutilitytracker", "havensalmanac"],
  },
  havendevtools: {
    icon: "🛠️",
    status: "Stable",
    tags: ["Dev", "Debug", "Utilities"],
    related: ["havensalmanac", "cropoptimizer", "thevault"],
  },
  havensalmanac: {
    icon: "📖",
    status: "Stable",
    tags: ["UI", "Dashboard", "Tracking"],
    related: ["sunhaventodo", "cropoptimizer", "sunhavenmuseumutilitytracker"],
  },
  fasterraces: {
    icon: "🏃",
    status: "Stable",
    tags: ["QoL", "Movement", "Races"],
    related: ["havensbirthright", "havensrespec", "havensalmanac"],
  },
  trinketfortune: {
    icon: "🎣",
    status: "Stable",
    tags: ["Fishing", "Tracking", "QoL"],
    related: ["sunhavenmuseumutilitytracker", "havensalmanac", "cropoptimizer"],
  },
  cropoptimizer: {
    icon: "🌱",
    status: "Stable",
    tags: ["Farming", "Forecasting", "UI"],
    related: ["havensalmanac", "sunhaventodo", "trinketfortune"],
  },
  havensrespec: {
    icon: "📜",
    status: "Stable",
    tags: ["Skills", "QoL", "UI"],
    related: ["havensbirthright", "fasterraces", "havensalmanac"],
  },
  giftingassistant: {
    icon: "🎁",
    status: "New",
    tags: ["Social", "QoL", "Planning"],
    related: ["squirrelsbirthdayreminder", "sunhaventodo", "havensalmanac"],
  },
};

const MOD_SCENES = {
  senpaischest: ["Sort chaos into labeled systems", "Build chest rules by category", "Keep inventory clean between sessions"],
  havensbirthright: ["Pick identity-defining perks", "Balance race utility with combat", "Shape long-run character expression"],
  sunhavenmuseumutilitytracker: ["Track remaining donations quickly", "Read section completion at a glance", "Avoid duplicate museum grind"],
  squirrelsbirthdayreminder: ["Catch birthdays before they pass", "Keep gift planning lightweight", "Reduce social progression misses"],
  sunhaventodo: ["Plan daily runs by priority", "Track completion with clarity", "Keep repeat tasks visible"],
  thevault: ["Centralize token/key value", "Spend directly from secured reserves", "Keep your bag focused on play items"],
  havendevtools: ["Inspect runtime state fast", "Debug interactions in live sessions", "Accelerate iteration loops"],
  havensalmanac: ["Read all key signals in one place", "Get morning briefing context", "Reduce tab-hopping between mods"],
  fasterraces: ["Tune movement for your pace", "Keep races feeling distinct", "Stay compatible with broader loadouts"],
  trinketfortune: ["Bias rewards toward unmet goals", "Pair chance with museum progress", "Make fishing outcomes feel fairer"],
  cropoptimizer: ["Forecast value before harvest", "Prioritize plots by projected yield", "Act on ETA and quality context"],
  havensrespec: ["Rebuild trees with confidence", "Preview cost before commitment", "Keep resets safe and controlled"],
  giftingassistant: ["Plan the day's gift run", "See loved/liked gifts with icons", "Mark gifted and stay in sync"],
};

const MOD_LAYOUTS = {
  senpaischest: "checklist",
  sunhaventodo: "checklist",
  sunhavenmuseumutilitytracker: "timeline",
  squirrelsbirthdayreminder: "timeline",
  havendevtools: "console",
  havensalmanac: "dashboard",
  cropoptimizer: "dashboard",
  havensbirthright: "spotlight",
  fasterraces: "spotlight",
  trinketfortune: "spotlight",
  havensrespec: "spotlight",
  giftingassistant: "checklist",
  thevault: "vault",
};

const MOD_PROFILES = {
  senpaischest: {
    themeClass: "theme-amber",
    tagline: "Rule-driven sorting that keeps your storage clean without busywork.",
    motif: "Archive Wing",
    context:
      "Senpai's Chest is a storage automation mod focused on reducing inventory friction through chest rules and smart sorting behavior.",
    bestFor: "Players managing many chest categories and frequent loot drops.",
    synergy: "Pairs well with S.M.U.T. and Todo for museum-driven organization loops.",
    story:
      "Built for players with growing inventories, this page uses an archive-inspired style and organizational language to mirror smart sorting.",
    panelTitle: "Sorting highlights",
    highlights: ["Smart chest rules", "Automated sorting passes", "Inventory quality-of-life"],
  },
  havensbirthright: {
    themeClass: "theme-cobalt",
    tagline: "Profession-inspired powers tuned for smooth, readable gameplay.",
    motif: "Forged Paths",
    context:
      "Haven's Birthright introduces race-based gameplay identity with configurable bonuses and abilities for long-run character variety.",
    bestFor: "Players who want race choice to have stronger gameplay impact.",
    synergy: "Works well beside Faster Races and Haven's Respec for build experimentation.",
    story:
      "A bold, progression-focused profile centered on powers and profession flexibility with high readability for moment-to-moment decisions.",
    panelTitle: "Ability highlights",
    highlights: ["Ability enhancements", "Balanced stat flows", "In-run utility upgrades"],
  },
  sunhavenmuseumutilitytracker: {
    themeClass: "theme-slate",
    tagline: "Museum progress clarity so collecting goals stay obvious.",
    motif: "Curator Desk",
    context:
      "S.M.U.T. tracks museum donation progress with visibility improvements so collection goals stay actionable.",
    bestFor: "Completionists and players pushing Hall/Aquarium progress.",
    synergy: "Strong synergy with Senpai's Chest and Trinket Fortune collection loops.",
    story:
      "Curator-like presentation with clean status emphasis, helping collectors quickly read progress and identify what still needs attention.",
    panelTitle: "Collection highlights",
    highlights: ["Donation tracking", "Progress visibility", "Collector-focused workflows"],
  },
  squirrelsbirthdayreminder: {
    themeClass: "theme-teal",
    tagline: "Never miss birthdays again with lightweight reminder support.",
    motif: "Calendar Nook",
    context:
      "Birthday Reminder keeps social progression smooth by surfacing birthday timing and gift context at the right moments.",
    bestFor: "Relationship-focused players who do not want to miss social deadlines.",
    synergy: "Pairs naturally with Todo for auto-generated reminder workflows.",
    story:
      "Friendly reminder-oriented layout focused on clarity and low-noise signals that mimic a social planner board.",
    panelTitle: "Reminder highlights",
    highlights: ["Calendar reminders", "Low-noise alerts", "Friendly HUD prompts"],
  },
  sunhaventodo: {
    themeClass: "theme-violet",
    tagline: "Task tracking for focused days and cleaner farm planning.",
    motif: "Task Command",
    context:
      "Sun Haven Todo brings in-game planning structure for daily priorities, recurring chores, and cross-mod task tracking.",
    bestFor: "Players who like explicit goals and visible progress.",
    synergy: "Integrates smoothly with Birthday Reminder, Gifting Assistant, S.M.U.T., and Almanac flows.",
    story:
      "Checklist-forward design language mirrors the mod's planning flow, with explicit labels and strong visual hierarchy.",
    panelTitle: "Todo highlights",
    highlights: ["Daily objectives", "Completion states", "Simple progress views"],
  },
  thevault: {
    themeClass: "theme-vault",
    tagline: "Centralized vaulting built for fast deposits and inventory calm.",
    motif: "Grand Bank",
    context:
      "The Vault centralizes key/token currency handling so spending and storage happen seamlessly without manual juggling.",
    bestFor: "Players tired of carrying utility currencies in inventory slots.",
    synergy: "Complements storage and dashboard mods like Senpai's Chest and Almanac.",
    story:
      "Bank-inspired styling with a ledger feel, metallic accents, and monetary terminology to reinforce secure storage and value flow.",
    panelTitle: "Vault banking features",
    flavorTitle: "Vault prestige",
    flavorText:
      "From the entrance arch to the ledger cards, this profile leans into a grand-bank identity that celebrates control, order, and value.",
    highlights: ["Bulk storage flow", "Sweep actions", "Vault HUD totals"],
  },
  havendevtools: {
    themeClass: "theme-indigo",
    tagline: "Debug and inspection tools that speed up mod iteration loops.",
    motif: "Control Console",
    context:
      "HavenDevTools is a debugging utility layer for inspecting game state and accelerating mod development workflows.",
    bestFor: "Mod developers and testers doing runtime validation.",
    synergy: "Useful alongside any mod when debugging integration behavior.",
    story:
      "Engineering-console vibe for developer workflows, emphasizing diagnostics, traceability, and quick iteration loops.",
    panelTitle: "Dev highlights",
    highlights: ["Developer tooling", "Runtime introspection", "Faster troubleshooting"],
  },
  havensalmanac: {
    themeClass: "theme-sky",
    tagline: "Daily briefing surfaces what matters at a glance.",
    motif: "Morning Briefing",
    context:
      "Haven's Almanac aggregates multi-mod signals into digestible summaries, HUD views, and daily decision support.",
    bestFor: "Players running multiple mods who want one command center.",
    synergy: "Designed to integrate with tracking-heavy mods like Todo and Crop Optimizer.",
    story:
      "Report-like presentation with digest semantics to match how the mod summarizes key daily context for players.",
    panelTitle: "Briefing highlights",
    highlights: ["Briefing overlays", "Status summaries", "Clean information density"],
  },
  fasterraces: {
    themeClass: "theme-greenblue",
    tagline: "Race pacing refinements for a snappier feel during play.",
    motif: "Speed Track",
    context:
      "Faster Races focuses on movement pacing and responsiveness with controlled tuning for smoother traversal.",
    bestFor: "Players who want a snappier movement baseline.",
    synergy: "Can be combined with Birthright and Respec-centered builds.",
    story:
      "Motion-centric page character focused on pacing and responsiveness, while maintaining contrast-safe readability.",
    panelTitle: "Speed highlights",
    highlights: ["Movement tuning", "Smoother progression", "Configurable speed behavior"],
  },
  trinketfortune: {
    themeClass: "theme-orchid",
    tagline: "Smarter reward biasing to make trinket and museum outcomes feel better.",
    motif: "Lucky Atelier",
    context:
      "Trinket Fortune biases outcomes toward collection progression, making late-stage trinket completion less punishing.",
    bestFor: "Fishing-focused players chasing missing museum/trinket entries.",
    synergy: "Most effective when paired with S.M.U.T. tracking visibility.",
    story:
      "Treasure-hunt inspired profile emphasizing chance tuning and curated outcomes without sacrificing clear metric readability.",
    panelTitle: "Fortune highlights",
    highlights: ["Bias-aware selection", "Museum-aware outcomes", "Config-based control"],
  },
  cropoptimizer: {
    themeClass: "theme-gold",
    tagline: "Crop intelligence that helps prioritize profit and timing.",
    motif: "Field Analyst",
    context:
      "Crop Optimizer provides crop-level timing and value forecasting for better planting and harvest decisions.",
    bestFor: "Players optimizing farm profit and scheduling windows.",
    synergy: "Pairs well with Almanac and Todo planning loops.",
    story:
      "Yield-and-timing framing with pragmatic, planner-oriented language to match farming optimization workflows.",
    panelTitle: "Optimization highlights",
    highlights: ["Growth insight", "Profit forecasting", "Priority recommendations"],
  },
  havensrespec: {
    themeClass: "theme-rose",
    tagline: "Flexible profession resets with clearer cost feedback.",
    motif: "Reset Atelier",
    context:
      "Haven's Respec enables controlled profession reset workflows with clearer cost and safety feedback.",
    bestFor: "Players iterating builds without losing confidence in reset outcomes.",
    synergy: "Pairs with Birthright/Faster Races for broad build experimentation.",
    story:
      "A reset-and-rebuild narrative style with explicit cost visibility, reinforcing confidence before large progression changes.",
    panelTitle: "Respec highlights",
    highlights: ["Respec controls", "Cost previews", "Safer reset flow"],
  },
  giftingassistant: {
    themeClass: "theme-orchid",
    tagline: "A daily gift routine that keeps friendships on track.",
    motif: "Gift Desk",
    context:
      "Gifting Assistant turns daily gift-giving into a per-character routine with loved/liked suggestions, item icons, priorities, and gifted-today tracking.",
    bestFor: "Relationship-focused players who want a repeatable daily gift plan.",
    synergy: "Pairs with Sun Haven Todo (daily gift tasks), Birthday Reminder, and Almanac progress.",
    story:
      "Checklist-forward profile built around a daily roster, mirroring how the mod plans, prioritizes, and checks off each gift.",
    panelTitle: "Gifting highlights",
    highlights: ["Daily gift roster", "Loved/liked suggestions", "Todo auto-complete sync"],
  },
};

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

function resolveStatsId(modKey) {
  return STATS_ID_BY_MOD_KEY[modKey] || modKey;
}

createApp({
  data() {
    return {
      loading: true,
      error: "",
      ambientEnabled: true,
      lastFetched: null,
      modKey: document.documentElement.dataset.modKey || "",
      mod: null,
      matrixRows: [],
      profile: {
        themeClass: "theme-cobalt",
        tagline: "Focused Sun Haven quality-of-life improvement.",
        motif: "Mod Profile",
        context: "Practical quality-of-life enhancement for smoother day-to-day gameplay.",
        bestFor: "Players seeking less friction in routine gameplay loops.",
        synergy: "Works alongside other utility mods in the SunhavenMod ecosystem.",
        story: "Readable, accessible profile built around key mod outcomes.",
        panelTitle: "Highlights",
        flavorTitle: "",
        flavorText: "",
        highlights: ["Quality-of-life tuning", "Live telemetry", "Mod-friendly workflow"],
      },
      stats: {
        combined: null,
        thunderstore: null,
        nexusTotal: null,
        nexusUnique: null,
      },
      releaseMeta: null,
      nexusUrl: "",
    };
  },
  computed: {
    pageClass() {
      return this.profile.themeClass || "theme-cobalt";
    },
    thunderstoreUrl() {
      if (!this.mod?.thunderstoreName) return "https://thunderstore.io/c/sun-haven/";
      return `https://thunderstore.io/c/sun-haven/p/AzraelGodKing/${this.mod.thunderstoreName}/`;
    },
    docsUrl() {
      const pagePath = this.mod?.docsPagePath;
      if (!pagePath) return "https://azraelgodking.github.io/SunhavenMod/";
      return `https://azraelgodking.github.io/SunhavenMod/${pagePath}`;
    },
    fallbackNexusSearchUrl() {
      const q = encodeURIComponent(this.mod?.indexDataName || this.mod?.modKey || "Sunhaven mod");
      return `https://www.nexusmods.com/sunhaven/search/?gsearch=${q}&gsearchtype=mods`;
    },
    presentation() {
      return (
        MOD_PRESENTATION[this.modKey] || {
          icon: "✨",
          status: "Stable",
          tags: ["QoL"],
          related: [],
        }
      );
    },
    relatedMods() {
      if (!this.matrixRows.length) return [];
      return (this.presentation.related || [])
        .map((key) => this.matrixRows.find((m) => m.modKey === key))
        .filter(Boolean)
        .slice(0, 3);
    },
    githubIssueUrl() {
      const title = encodeURIComponent(`[${this.mod?.indexDataName || this.modKey}] Issue report`);
      return `https://github.com/AzraelGodKing/SunhavenMod/issues/new?title=${title}`;
    },
    githubFeatureUrl() {
      const title = encodeURIComponent(`[${this.mod?.indexDataName || this.modKey}] Feature request`);
      return `https://github.com/AzraelGodKing/SunhavenMod/issues/new?title=${title}`;
    },
    sceneSteps() {
      return MOD_SCENES[this.modKey] || ["Focused quality-of-life improvements.", "Readable telemetry-backed context.", "Reliable integration-ready workflow."];
    },
    layoutType() {
      return MOD_LAYOUTS[this.modKey] || "spotlight";
    },
    reducedMotion() {
      return typeof window !== "undefined" &&
        typeof window.matchMedia === "function" &&
        window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    },
  },
  async mounted() {
    try {
      const saved = localStorage.getItem("sunhavenmod-ambient-enabled");
      if (saved === "0") this.ambientEnabled = false;
    } catch {
      // Ignore storage failures.
    }
    try {
      const [matrixRes, statsRes, versionsRes] = await Promise.all([
        fetch(MOD_MATRIX_URL, { cache: "no-cache" }),
        fetch(STATS_CACHE_URL, { cache: "no-cache" }),
        fetch(VERSIONS_URL, { cache: "no-cache" }),
      ]);
      if (!matrixRes.ok) throw new Error(`mod-matrix: HTTP ${matrixRes.status}`);
      if (!statsRes.ok) throw new Error(`stats-cache: HTTP ${statsRes.status}`);
      if (!versionsRes.ok) throw new Error(`versions.json: HTTP ${versionsRes.status}`);

      const matrix = await matrixRes.json();
      const stats = await statsRes.json();
      const versions = await versionsRes.json();
      this.lastFetched = stats?.lastFetched ?? null;

      this.matrixRows = Array.isArray(matrix) ? matrix : [];
      const mod = this.matrixRows.find((m) => m.modKey === this.modKey);
      if (!mod) throw new Error(`Unknown mod key '${this.modKey}'`);
      this.mod = mod;
      this.profile = MOD_PROFILES[this.modKey] || this.profile;
      this.releaseMeta = versions?.[mod.jsonKey] || null;
      this.nexusUrl = this.releaseMeta?.nexus || "";

      const statsId = resolveStatsId(this.modKey);
      const entry = stats?.mods?.[statsId] || {};
      this.stats = {
        combined: entry.combined_total ?? null,
        thunderstore: entry.thunderstore?.total_downloads ?? null,
        nexusTotal: entry.nexus?.total_downloads ?? null,
        nexusUnique: entry.nexus?.unique_downloads ?? null,
      };
    } catch (e) {
      this.error = e instanceof Error ? e.message : String(e);
    } finally {
      this.loading = false;
      await nextTick();
      this.runModAnimations();
    }
  },
  methods: {
    formatInt,
    formatWhen,
    modHref(modKey) {
      return `./${modKey}.html`;
    },
    runModAnimations() {
      if (this.reducedMotion) return;
      animate(".mod-hero", { opacity: [0.12, 1], y: [12, 0] }, { duration: 0.45, easing: "ease-out" });
      animate(".mod-stats .stat-card", { opacity: [0.1, 1], y: [10, 0] }, { delay: stagger(0.05), duration: 0.34 });
      animate(".mod-grid .panel, .context-grid .panel, .install-strip, .experience-strip", { opacity: [0.05, 1], y: [12, 0] }, { delay: stagger(0.05), duration: 0.32 });
      if (this.mod?.modKey === "thevault") {
        animate(".vault-showcase .panel", { opacity: [0.1, 1], scale: [0.985, 1] }, { delay: stagger(0.06), duration: 0.35 });
      }
    },
    toggleAmbient() {
      this.ambientEnabled = !this.ambientEnabled;
      try {
        localStorage.setItem("sunhavenmod-ambient-enabled", this.ambientEnabled ? "1" : "0");
      } catch {
        // Ignore storage failures.
      }
      if (this.ambientEnabled) this.runModAnimations();
    },
  },
  template: `
    <div class="mod-page" :class="[pageClass, 'layout-' + layoutType, { 'no-ambient': !ambientEnabled }]">
      <nav class="site-nav">
        <div class="container site-nav-inner">
          <a class="site-nav-brand" href="../index.html">SunhavenMod</a>
          <div class="site-nav-links">
            <a href="../index.html">Home</a>
            <a href="../downloads.html">Downloads</a>
            <button class="ambient-toggle" type="button" @click="toggleAmbient">
              {{ ambientEnabled ? 'Ambient: On' : 'Ambient: Off' }}
            </button>
          </div>
        </div>
      </nav>

      <div class="mod-page-anim" :class="'anim-' + layoutType" aria-hidden="true">
        <span></span><span></span><span></span>
      </div>

      <main class="container mod-main">
        <p v-if="loading" class="state">Loading mod profile…</p>
        <p v-else-if="error" class="state error">Could not load mod profile: {{ error }}</p>
        <template v-else>
          <nav class="related-mods-strip" aria-label="Related mods" v-if="relatedMods.length">
            <span class="related-mods-label">Also see</span>
            <a v-for="r in relatedMods" :key="r.modKey" :href="modHref(r.modKey)">
              {{ r.indexDataName || r.modKey }}
            </a>
          </nav>

          <header class="mod-hero">
            <p class="eyebrow">Mod profile</p>
            <div class="hero-inline">
              <span class="hero-icon" aria-hidden="true">{{ presentation.icon }}</span>
              <p class="motif">{{ profile.motif }}</p>
            </div>
            <h1>{{ mod.indexDataName || mod.modKey }}</h1>
            <p class="lead">{{ profile.tagline }}</p>
            <p class="story">{{ profile.story }}</p>
            <div class="hero-meta-row">
              <span class="version-badge">{{ presentation.status }}</span>
              <span class="hero-meta-item" v-if="releaseMeta?.version">v{{ releaseMeta.version }}</span>
              <span class="hero-meta-item" v-for="tag in presentation.tags" :key="tag">{{ tag }}</span>
            </div>
          </header>

          <section class="trust-strip panel">
            <p class="trust-item"><strong>Status:</strong> {{ presentation.status }}</p>
            <p class="trust-item"><strong>Primary source:</strong> Thunderstore + Nexus</p>
            <p class="trust-item"><strong>Profile:</strong> {{ mod.modKey }}</p>
          </section>

          <nav class="mod-quick-nav" aria-label="Quick jump">
            <a href="#stats">Stats</a>
            <a href="#highlights">Highlights</a>
            <a href="#downloads">Downloads</a>
          </nav>

          <section class="mod-stats" id="stats">
            <article class="stat-card">
              <p class="stat-label">Combined downloads</p>
              <p class="stat-value">{{ formatInt(stats.combined) }}</p>
            </article>
            <article class="stat-card">
              <p class="stat-label">Thunderstore downloads</p>
              <p class="stat-value">{{ formatInt(stats.thunderstore) }}</p>
            </article>
            <article class="stat-card">
              <p class="stat-label">Nexus total downloads</p>
              <p class="stat-value">{{ formatInt(stats.nexusTotal) }}</p>
            </article>
            <article class="stat-card">
              <p class="stat-label">Nexus unique downloads</p>
              <p class="stat-value">{{ formatInt(stats.nexusUnique) }}</p>
            </article>
          </section>

          <section class="mod-grid" id="highlights">
            <article class="panel">
              <h2>{{ profile.panelTitle }}</h2>
              <ul>
                <li v-for="item in profile.highlights" :key="item">{{ item }}</li>
              </ul>
            </article>
            <article class="panel" id="downloads">
              <h2>Where to get it</h2>
              <p>Each source is listed with plain text labels for accessibility.</p>
              <p class="cta-row">
                <a class="btn btn-primary" :href="thunderstoreUrl" target="_blank" rel="noopener">Thunderstore page</a>
                <a class="btn btn-ghost" :href="nexusUrl || fallbackNexusSearchUrl" target="_blank" rel="noopener">Nexus mod page</a>
                <a class="btn btn-ghost" :href="docsUrl" target="_blank" rel="noopener">Docs page</a>
              </p>
            </article>
          </section>

          <section class="context-grid">
            <article class="panel">
              <h2>Mod context</h2>
              <p><strong>Overview:</strong> {{ profile.context }}</p>
              <p><strong>Best for:</strong> {{ profile.bestFor }}</p>
              <p><strong>Synergy:</strong> {{ profile.synergy }}</p>
            </article>
          </section>

          <section class="install-strip panel">
            <h2>Install in 60 seconds</h2>
            <div class="install-steps">
              <article>
                <strong>1.</strong>
                <p>Install BepInEx 5.x for Sun Haven.</p>
              </article>
              <article>
                <strong>2.</strong>
                <p>Download from Thunderstore or Nexus.</p>
              </article>
              <article>
                <strong>3.</strong>
                <p>Place DLL in <code>BepInEx/plugins</code> and launch.</p>
              </article>
            </div>
            <p class="cta-row">
              <a class="btn btn-ghost" :href="githubIssueUrl" target="_blank" rel="noopener">Report issue</a>
              <a class="btn btn-ghost" :href="githubFeatureUrl" target="_blank" rel="noopener">Request feature</a>
            </p>
          </section>

          <section class="experience-strip panel">
            <h2 v-if="layoutType === 'timeline'">Progress timeline</h2>
            <h2 v-else-if="layoutType === 'checklist'">Loadout checklist</h2>
            <h2 v-else-if="layoutType === 'console'">Dev console routine</h2>
            <h2 v-else-if="layoutType === 'dashboard'">Command dashboard loop</h2>
            <h2 v-else>Play loop fit</h2>

            <div v-if="layoutType === 'timeline'" class="timeline-list">
              <article v-for="(step, idx) in sceneSteps" :key="step" class="timeline-item">
                <p class="experience-index">Phase {{ idx + 1 }}</p>
                <p>{{ step }}</p>
              </article>
            </div>

            <div v-else-if="layoutType === 'checklist'" class="checklist-list">
              <label v-for="(step, idx) in sceneSteps" :key="step" class="checklist-item">
                <input type="checkbox" :aria-label="'Checklist item ' + (idx + 1)" />
                <span>{{ step }}</span>
              </label>
            </div>

            <div v-else-if="layoutType === 'console'" class="console-list" role="log" aria-live="polite">
              <p v-for="(step, idx) in sceneSteps" :key="step">
                <span class="console-prefix">[{{ String(idx + 1).padStart(2, '0') }}]</span>
                {{ step }}
              </p>
            </div>

            <div v-else-if="layoutType === 'dashboard'" class="dashboard-pills">
              <article v-for="(step, idx) in sceneSteps" :key="step" class="dashboard-pill">
                <p class="experience-index">Widget {{ idx + 1 }}</p>
                <p>{{ step }}</p>
              </article>
            </div>

            <div v-else class="experience-steps">
              <article v-for="(step, idx) in sceneSteps" :key="step" class="experience-step">
                <p class="experience-index">Step {{ idx + 1 }}</p>
                <p>{{ step }}</p>
              </article>
            </div>
          </section>

          <section v-if="mod.modKey === 'thevault'" class="vault-showcase">
            <article class="vault-emblem panel">
              <h2>{{ profile.flavorTitle || 'Signature look' }}</h2>
              <p>{{ profile.flavorText }}</p>
              <div class="vault-lockup" aria-hidden="true">
                <span class="vault-ring"></span>
                <span class="vault-core">$</span>
              </div>
            </article>
            <article class="vault-ledger panel">
              <h2>Vault ledger snapshot</h2>
              <dl>
                <div>
                  <dt>Secured combined volume</dt>
                  <dd>{{ formatInt(stats.combined) }}</dd>
                </div>
                <div>
                  <dt>Network intake (Thunderstore)</dt>
                  <dd>{{ formatInt(stats.thunderstore) }}</dd>
                </div>
                <div>
                  <dt>Public branch intake (Nexus total)</dt>
                  <dd>{{ formatInt(stats.nexusTotal) }}</dd>
                </div>
              </dl>
            </article>
          </section>
        </template>
      </main>
    </div>
  `,
}).mount("#app");
