<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { RouterLink } from "vue-router";
import AmbientLayer from "../components/AmbientLayer.vue";
import {
  MOD_LAYOUTS,
  MOD_PRESENTATION,
  MOD_PROFILES,
  MOD_SCENES,
  resolveStatsId,
} from "../data/mods.js";
import {
  DOCS_HUB_URL,
  MOD_MATRIX_URL,
  STATS_CACHE_URL,
  THUNDERSTORE_COMMUNITY_URL,
  VERSIONS_URL,
} from "../data/urls.js";
import { formatInt, formatWhen } from "../composables/format.js";

const props = defineProps({
  modKey: { type: String, required: true },
});

const loading = ref(true);
const error = ref("");
const lastFetched = ref(null);
const mod = ref(null);
const matrixRows = ref([]);
const releaseMeta = ref(null);
const nexusUrl = ref("");
const stats = ref({
  combined: null,
  thunderstore: null,
  nexusTotal: null,
  nexusUnique: null,
});

const defaultProfile = {
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
};

const profile = computed(() => MOD_PROFILES[props.modKey] || defaultProfile);
const presentation = computed(
  () =>
    MOD_PRESENTATION[props.modKey] || {
      icon: "✨",
      status: "Stable",
      tags: ["QoL"],
      related: [],
    }
);
const layoutType = computed(() => MOD_LAYOUTS[props.modKey] || "spotlight");
const sceneSteps = computed(
  () =>
    MOD_SCENES[props.modKey] || [
      "Focused quality-of-life improvements.",
      "Readable telemetry-backed context.",
      "Reliable integration-ready workflow.",
    ]
);

const relatedMods = computed(() => {
  if (!matrixRows.value.length) return [];
  return (presentation.value.related || [])
    .map((key) => matrixRows.value.find((m) => m.modKey === key))
    .filter(Boolean)
    .slice(0, 3);
});

const thunderstoreUrl = computed(() => {
  if (!mod.value?.thunderstoreName) return THUNDERSTORE_COMMUNITY_URL;
  return `https://thunderstore.io/c/sun-haven/p/AzraelGodKing/${mod.value.thunderstoreName}/`;
});

const docsUrl = computed(() => {
  const pagePath = mod.value?.docsPagePath;
  if (!pagePath) return DOCS_HUB_URL;
  return `${DOCS_HUB_URL}${pagePath}`;
});

const fallbackNexusSearchUrl = computed(() => {
  const q = encodeURIComponent(
    mod.value?.indexDataName || mod.value?.modKey || "Sunhaven mod"
  );
  return `https://www.nexusmods.com/sunhaven/search/?gsearch=${q}&gsearchtype=mods`;
});

const githubIssueUrl = computed(() => {
  const title = encodeURIComponent(
    `[${mod.value?.indexDataName || props.modKey}] Issue report`
  );
  return `https://github.com/AzraelGodKing/SunhavenMod/issues/new?title=${title}`;
});

const githubFeatureUrl = computed(() => {
  const title = encodeURIComponent(
    `[${mod.value?.indexDataName || props.modKey}] Feature request`
  );
  return `https://github.com/AzraelGodKing/SunhavenMod/issues/new?title=${title}`;
});

async function loadMod() {
  loading.value = true;
  error.value = "";
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
    const statsJson = await statsRes.json();
    const versions = await versionsRes.json();
    lastFetched.value = statsJson?.lastFetched ?? null;
    matrixRows.value = Array.isArray(matrix) ? matrix : [];

    const found = matrixRows.value.find((m) => m.modKey === props.modKey);
    if (!found) throw new Error(`Unknown mod key '${props.modKey}'`);
    mod.value = found;
    releaseMeta.value = versions?.[found.jsonKey] || null;
    nexusUrl.value = releaseMeta.value?.nexus || "";

    const statsId = resolveStatsId(props.modKey);
    const entry = statsJson?.mods?.[statsId] || {};
    stats.value = {
      combined: entry.combined_total ?? null,
      thunderstore: entry.thunderstore?.total_downloads ?? null,
      nexusTotal: entry.nexus?.total_downloads ?? null,
      nexusUnique: entry.nexus?.unique_downloads ?? null,
    };

    document.title = `${found.indexDataName || props.modKey} · SunhavenMod`;
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
    mod.value = null;
  } finally {
    loading.value = false;
  }
}

onMounted(loadMod);
watch(() => props.modKey, loadMod);
</script>

<template>
  <div
    class="page page-mod"
    :class="[profile.themeClass, 'layout-' + layoutType]"
  >
    <AmbientLayer variant="mod" />

    <main id="main" class="shell mod-main stack">
      <p v-if="loading" class="state" role="status">Loading mod profile…</p>
      <p v-else-if="error" class="state error" role="alert">
        Could not load mod profile: {{ error }}
      </p>

      <template v-else>
        <nav v-if="relatedMods.length" class="related-strip" aria-label="Related mods">
          <span>Also see</span>
          <RouterLink
            v-for="r in relatedMods"
            :key="r.modKey"
            :to="`/mods/${r.modKey}`"
          >
            {{ r.indexDataName || r.modKey }}
          </RouterLink>
        </nav>

        <header class="mod-hero">
          <p class="brand-mark">SunhavenMod</p>
          <div class="hero-inline">
            <span class="hero-icon" aria-hidden="true">{{ presentation.icon }}</span>
            <p class="motif">{{ profile.motif }}</p>
          </div>
          <h1>{{ mod.indexDataName || mod.modKey }}</h1>
          <p class="lead">{{ profile.tagline }}</p>
          <p class="story">{{ profile.story }}</p>
          <div class="hero-meta-row">
            <span class="badge">{{ presentation.status }}</span>
            <span v-if="releaseMeta?.version" class="meta-chip">v{{ releaseMeta.version }}</span>
            <span v-for="tag in presentation.tags" :key="tag" class="meta-chip">{{
              tag
            }}</span>
          </div>
        </header>

        <section class="trust-strip">
          <p><strong>Status:</strong> {{ presentation.status }}</p>
          <p><strong>Sources:</strong> Thunderstore + Nexus</p>
          <p><strong>Profile:</strong> {{ mod.modKey }}</p>
          <p class="subtle">Stats refreshed: {{ formatWhen(lastFetched) }}</p>
        </section>

        <nav class="mod-quick-nav" aria-label="Quick jump">
          <a href="#stats">Stats</a>
          <a href="#highlights">Highlights</a>
          <a href="#downloads">Downloads</a>
        </nav>

        <section id="stats" class="stat-grid">
          <article>
            <p class="stat-label">Combined</p>
            <p class="stat-value">{{ formatInt(stats.combined) }}</p>
          </article>
          <article>
            <p class="stat-label">Thunderstore</p>
            <p class="stat-value">{{ formatInt(stats.thunderstore) }}</p>
          </article>
          <article>
            <p class="stat-label">Nexus total</p>
            <p class="stat-value">{{ formatInt(stats.nexusTotal) }}</p>
          </article>
          <article>
            <p class="stat-label">Nexus unique</p>
            <p class="stat-value">{{ formatInt(stats.nexusUnique) }}</p>
          </article>
        </section>

        <section id="highlights" class="split-grid">
          <article class="panel">
            <h2>{{ profile.panelTitle }}</h2>
            <ul>
              <li v-for="item in profile.highlights" :key="item">{{ item }}</li>
            </ul>
          </article>
          <article id="downloads" class="panel">
            <h2>Where to get it</h2>
            <p class="cta-row">
              <a class="btn btn-primary" :href="thunderstoreUrl" target="_blank" rel="noopener"
                >Thunderstore</a
              >
              <a
                class="btn btn-ghost"
                :href="nexusUrl || fallbackNexusSearchUrl"
                target="_blank"
                rel="noopener"
                >Nexus</a
              >
              <a class="btn btn-ghost" :href="docsUrl" target="_blank" rel="noopener">Docs</a>
            </p>
          </article>
        </section>

        <section class="panel">
          <h2>Mod context</h2>
          <p><strong>Overview:</strong> {{ profile.context }}</p>
          <p><strong>Best for:</strong> {{ profile.bestFor }}</p>
          <p><strong>Synergy:</strong> {{ profile.synergy }}</p>
        </section>

        <section class="panel">
          <h2>Install in 60 seconds</h2>
          <div class="install-steps">
            <article>
              <strong>1</strong>
              <p>Install BepInEx 5.x for Sun Haven.</p>
            </article>
            <article>
              <strong>2</strong>
              <p>Download from Thunderstore or Nexus.</p>
            </article>
            <article>
              <strong>3</strong>
              <p>Place the DLL in <code>BepInEx/plugins</code> and launch.</p>
            </article>
          </div>
          <p class="cta-row">
            <a class="btn btn-ghost" :href="githubIssueUrl" target="_blank" rel="noopener"
              >Report issue</a
            >
            <a class="btn btn-ghost" :href="githubFeatureUrl" target="_blank" rel="noopener"
              >Request feature</a
            >
          </p>
        </section>

        <section class="panel experience">
          <h2 v-if="layoutType === 'timeline'">Progress timeline</h2>
          <h2 v-else-if="layoutType === 'checklist'">Loadout checklist</h2>
          <h2 v-else-if="layoutType === 'console'">Dev console routine</h2>
          <h2 v-else-if="layoutType === 'dashboard'">Command dashboard loop</h2>
          <h2 v-else>Play loop fit</h2>

          <div v-if="layoutType === 'timeline'" class="timeline-list">
            <article v-for="(step, idx) in sceneSteps" :key="step">
              <p class="experience-index">Phase {{ idx + 1 }}</p>
              <p>{{ step }}</p>
            </article>
          </div>
          <div v-else-if="layoutType === 'checklist'" class="checklist-list">
            <label v-for="(step, idx) in sceneSteps" :key="step">
              <input type="checkbox" :aria-label="'Checklist item ' + (idx + 1)" />
              <span>{{ step }}</span>
            </label>
          </div>
          <div v-else-if="layoutType === 'console'" class="console-list" role="log">
            <p v-for="(step, idx) in sceneSteps" :key="step">
              <span class="console-prefix">[{{ String(idx + 1).padStart(2, "0") }}]</span>
              {{ step }}
            </p>
          </div>
          <div v-else-if="layoutType === 'dashboard'" class="dashboard-pills">
            <article v-for="(step, idx) in sceneSteps" :key="step">
              <p class="experience-index">Widget {{ idx + 1 }}</p>
              <p>{{ step }}</p>
            </article>
          </div>
          <div v-else class="experience-steps">
            <article v-for="(step, idx) in sceneSteps" :key="step">
              <p class="experience-index">Step {{ idx + 1 }}</p>
              <p>{{ step }}</p>
            </article>
          </div>
        </section>

        <section v-if="mod.modKey === 'thevault'" class="vault-showcase">
          <article class="panel">
            <h2>{{ profile.flavorTitle || "Signature look" }}</h2>
            <p>{{ profile.flavorText }}</p>
            <div class="vault-lockup" aria-hidden="true">
              <span class="vault-ring"></span>
              <span class="vault-core">$</span>
            </div>
          </article>
          <article class="panel">
            <h2>Vault ledger snapshot</h2>
            <dl class="metrics">
              <div>
                <dt>Secured combined volume</dt>
                <dd>{{ formatInt(stats.combined) }}</dd>
              </div>
              <div>
                <dt>Thunderstore intake</dt>
                <dd>{{ formatInt(stats.thunderstore) }}</dd>
              </div>
              <div>
                <dt>Nexus intake</dt>
                <dd>{{ formatInt(stats.nexusTotal) }}</dd>
              </div>
            </dl>
          </article>
        </section>
      </template>
    </main>
  </div>
</template>
