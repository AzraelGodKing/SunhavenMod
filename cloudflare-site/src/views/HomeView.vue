<script setup>
import { computed, onMounted, ref } from "vue";
import { RouterLink } from "vue-router";
import AmbientLayer from "../components/AmbientLayer.vue";
import SiteTotals from "../components/SiteTotals.vue";
import { STARTER_BUNDLES } from "../data/mods.js";
import { THUNDERSTORE_COMMUNITY_URL } from "../data/urls.js";
import { formatInt, formatWhen, laneClass } from "../composables/format.js";
import { useModCatalog } from "../composables/useModCatalog.js";

const { mods, siteTotal, lastFetched, loading, error, rankedMods, loadCatalog } =
  useModCatalog();

const showAll = ref(false);

const featuredMods = computed(() => rankedMods.value.slice(0, 4));
const topMovers = computed(() => rankedMods.value.slice(0, 3));
const landingMods = computed(() =>
  showAll.value ? rankedMods.value : rankedMods.value.slice(0, 8)
);
const topCombined = computed(() => Number(rankedMods.value[0]?.combined ?? 0));

function relativeWidth(mod) {
  const top = topCombined.value;
  const value = Number(mod?.combined || 0);
  if (!top || top <= 0) return "0%";
  return `${Math.max(8, Math.min(100, (value / top) * 100))}%`;
}

function bundleLabel(modKey) {
  return mods.value.find((m) => m.modKey === modKey)?.name || modKey;
}

onMounted(() => {
  loadCatalog();
});
</script>

<template>
  <div class="page page-home">
    <AmbientLayer variant="landing" />

    <header class="hero hero-home">
      <div class="shell hero-copy">
        <p class="brand-mark">SunhavenMod</p>
        <h1>Your haven, tuned for the long run</h1>
        <p class="lead">
          A living portal for the AzraelGodKing Sun Haven suite — pick by playstyle,
          follow live download momentum, and open themed mod profiles.
        </p>
        <div class="cta-row">
          <RouterLink class="btn btn-primary" to="/downloads">Open downloads board</RouterLink>
          <a
            class="btn btn-ghost"
            :href="THUNDERSTORE_COMMUNITY_URL"
            target="_blank"
            rel="noopener"
            >Visit Thunderstore</a
          >
        </div>
      </div>
      <div class="hero-wash" aria-hidden="true"></div>
    </header>

    <main id="main" class="shell stack">
      <p v-if="loading" class="state" role="status">Loading suite stats…</p>
      <p v-else-if="error" class="state error" role="alert">Could not load stats: {{ error }}</p>

      <template v-else>
        <SiteTotals :site-total="siteTotal" :total-mods="mods.length" />

        <section class="section notice-row">
          <div class="notice-copy reveal">
            <p class="kicker">Guild notice</p>
            <h2>Build your loadout by lane, not guesswork</h2>
            <p>
              Every mod profile is shaped to its gameplay role. Start with featured
              picks, then browse the full registry.
            </p>
            <ul class="plain-list">
              <li>Role-themed per-mod pages</li>
              <li>Shared telemetry with the docs hub</li>
              <li>Color-safe labels and contrast</li>
            </ul>
          </div>
          <div class="movers reveal delay-1">
            <p class="kicker">Hot board</p>
            <h2>Top movers</h2>
            <RouterLink
              v-for="m in topMovers"
              :key="m.statsId"
              class="mover-row"
              :to="`/mods/${m.modKey}`"
            >
              <span>{{ m.icon }} {{ m.name }}</span>
              <strong>{{ formatInt(m.combined) }}</strong>
            </RouterLink>
            <p class="subtle">Last refresh: {{ formatWhen(lastFetched) }}</p>
          </div>
        </section>

        <section class="section">
          <div class="section-head">
            <p class="kicker">Starter bundles</p>
            <h2>Ready-made playstyle stacks</h2>
          </div>
          <div class="bundle-grid">
            <article v-for="bundle in STARTER_BUNDLES" :key="bundle.id" class="bundle reveal">
              <h3>{{ bundle.title }}</h3>
              <p class="meta">{{ bundle.meta }}</p>
              <p class="bundle-links">
                <RouterLink
                  v-for="key in bundle.mods"
                  :key="key"
                  :to="`/mods/${key}`"
                  >{{ bundleLabel(key) }}</RouterLink
                >
              </p>
            </article>
          </div>
        </section>

        <section class="section">
          <div class="section-head">
            <p class="kicker">Featured</p>
            <h2>High-traction projects</h2>
          </div>
          <div class="featured-grid">
            <article
              v-for="m in featuredMods"
              :key="m.statsId"
              class="featured reveal"
              :class="laneClass(m.lane)"
            >
              <div class="featured-top">
                <h3>
                  <RouterLink :to="`/mods/${m.modKey}`">{{ m.icon }} {{ m.name }}</RouterLink>
                </h3>
                <span>{{ formatInt(m.combined) }}</span>
              </div>
              <p class="meta">{{ m.lane }} lane</p>
              <div class="meter" aria-hidden="true">
                <span :style="{ width: relativeWidth(m) }"></span>
              </div>
            </article>
          </div>
        </section>

        <section class="section">
          <div class="section-head row">
            <div>
              <p class="kicker">Registry</p>
              <h2>Browse the suite</h2>
            </div>
            <button class="btn btn-ghost" type="button" @click="showAll = !showAll">
              {{ showAll ? "Show fewer" : "Show all mods" }}
            </button>
          </div>
          <div class="registry-grid">
            <RouterLink
              v-for="m in landingMods"
              :key="m.statsId"
              class="registry-item reveal"
              :class="laneClass(m.lane)"
              :to="`/mods/${m.modKey}`"
            >
              <span class="registry-name">{{ m.icon }} {{ m.name }}</span>
              <span class="meta">{{ m.lane }}</span>
              <strong>{{ formatInt(m.combined) }}</strong>
            </RouterLink>
          </div>
        </section>
      </template>
    </main>
  </div>
</template>
