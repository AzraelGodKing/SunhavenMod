<script setup>
import { computed, onMounted, ref } from "vue";
import { RouterLink } from "vue-router";
import AmbientLayer from "../components/AmbientLayer.vue";
import SiteTotals from "../components/SiteTotals.vue";
import { formatInt, laneClass } from "../composables/format.js";
import { useModCatalog } from "../composables/useModCatalog.js";

const { mods, siteTotal, loading, error, loadCatalog } = useModCatalog();

const query = ref("");
const sortKey = ref("combined");
const selectedLane = ref("all");

const laneOptions = computed(() => {
  const lanes = Array.from(new Set(mods.value.map((m) => m.lane).filter(Boolean))).sort();
  return ["all", ...lanes];
});

const filteredMods = computed(() => {
  const q = query.value.trim().toLowerCase();
  let rows = mods.value;
  if (selectedLane.value !== "all") {
    rows = rows.filter((m) => m.lane === selectedLane.value);
  }
  if (q) {
    rows = rows.filter((m) => {
      const hay = `${m.name} ${m.modKey} ${m.statsId}`.toLowerCase();
      return hay.includes(q);
    });
  }
  const key = sortKey.value;
  const score = (m) => {
    if (key === "name") return m.name || m.modKey;
    if (key === "thunderstore") return m.ts ?? -1;
    if (key === "nexus") return m.nx ?? -1;
    return m.combined ?? -1;
  };
  return [...rows].sort((a, b) => {
    const sa = score(a);
    const sb = score(b);
    if (typeof sa === "string" && typeof sb === "string") return sa.localeCompare(sb);
    return Number(sb) - Number(sa);
  });
});

onMounted(() => {
  loadCatalog();
});
</script>

<template>
  <div class="page page-downloads">
    <AmbientLayer variant="downloads" />

    <header class="page-header">
      <div class="shell">
        <p class="brand-mark">SunhavenMod</p>
        <h1>Downloads command board</h1>
        <p class="lead">
          Sort, search, and jump into themed profiles. Live totals share the same cache as
          the docs hub.
        </p>
        <SiteTotals
          v-if="!loading && !error"
          :site-total="siteTotal"
          :total-mods="mods.length"
        />
      </div>
    </header>

    <main id="main" class="shell stack">
      <p v-if="loading" class="state" role="status">Loading stats…</p>
      <p v-else-if="error" class="state error" role="alert">Could not load stats: {{ error }}</p>

      <template v-else>
        <div class="toolbar">
          <label class="field">
            <span>Search</span>
            <input
              v-model.trim="query"
              type="search"
              placeholder="Filter by mod name or key…"
            />
          </label>
          <label class="field">
            <span>Lane</span>
            <select v-model="selectedLane">
              <option v-for="lane in laneOptions" :key="lane" :value="lane">
                {{ lane === "all" ? "All lanes" : lane }}
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

        <div class="mod-board">
          <article
            v-for="m in filteredMods"
            :key="m.statsId"
            class="mod-tile"
            :class="laneClass(m.lane)"
          >
            <div class="mod-tile-head">
              <div>
                <h2>
                  <RouterLink :to="`/mods/${m.modKey}`"
                    >{{ m.icon }} {{ m.name }}</RouterLink
                  >
                </h2>
                <p class="meta">{{ m.lane }} lane · {{ m.statsId }}</p>
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
                <dd>
                  {{ formatInt(m.nx) }}
                  <small>({{ formatInt(m.nxUnique) }})</small>
                </dd>
              </div>
            </dl>
          </article>
        </div>
        <p class="results-meta">
          Showing {{ filteredMods.length }} mod{{ filteredMods.length === 1 ? "" : "s" }}
        </p>
      </template>
    </main>
  </div>
</template>
