<script setup>
import { computed, onMounted, onUnmounted, ref, watch } from "vue";
import {
  COMPARE_HEADERS,
  COMPARE_ROWS,
  FAQ_ITEMS,
  FILTER_TAGS,
  HUB_MODS,
  INSTALL_STEPS,
  STARTER_PACKS,
} from "./data/hub.js";
import { formatInt, formatNexus, useHubData } from "./composables/useHubData.js";

const ANNOUNCE_KEY = "announce-gifting-assistant-v100";
const THEME_KEY = "sh-mod-theme";

const { stats, versions, loading, error, load, assetUrl } = useHubData();

const query = ref("");
const selectedTag = ref("all");
const announceVisible = ref(true);
const searchOpen = ref(false);
const searchQuery = ref("");
const searchIndex = ref([]);
const theme = ref("light");

const siteTotal = computed(() => stats.value?.site_total || {});
const modsById = computed(() => stats.value?.mods || {});

const filteredMods = computed(() => {
  const q = query.value.trim().toLowerCase();
  return HUB_MODS.filter((mod) => {
    if (selectedTag.value !== "all" && !mod.tags.includes(selectedTag.value)) return false;
    if (!q) return true;
    const hay = `${mod.name} ${mod.tagline} ${mod.description} ${mod.features.join(" ")}`.toLowerCase();
    return hay.includes(q);
  });
});

const searchHits = computed(() => {
  const q = searchQuery.value.trim().toLowerCase();
  const rows = searchIndex.value;
  if (!q) return rows.slice(0, 8);
  const scored = [];
  for (const row of rows) {
    const t = String(row.t || "").toLowerCase();
    const d = String(row.d || "").toLowerCase();
    const k = String(row.k || "").toLowerCase();
    let score = 0;
    if (t.includes(q)) score += 5;
    if (d.includes(q)) score += 2;
    if (k.includes(q)) score += 3;
    if ((row.tags || []).some((tag) => String(tag).toLowerCase().includes(q))) score += 2;
    if (score) scored.push({ row, score });
  }
  return scored
    .sort((a, b) => b.score - a.score)
    .slice(0, 12)
    .map((s) => s.row);
});

function versionFor(mod) {
  const entry = versions.value?.[mod.jsonKey];
  return entry?.version ? `v${entry.version}` : "";
}

function modStat(mod, field) {
  const entry = modsById.value[mod.id] || {};
  if (field === "thunderstore") return formatInt(entry.thunderstore?.total_downloads);
  if (field === "nexus") return formatInt(entry.nexus?.total_downloads);
  if (field === "combined") return formatInt(entry.combined_total);
  return "—";
}

function statusClass(status) {
  if (status === "New") return "status-new";
  if (status === "Actively Maintained") return "status-active";
  return "status-stable";
}

function dismissAnnounce() {
  announceVisible.value = false;
  try {
    localStorage.setItem(ANNOUNCE_KEY, "1");
  } catch {
    /* ignore */
  }
}

function toggleTheme() {
  theme.value = theme.value === "dark" ? "light" : "dark";
  document.documentElement.setAttribute("data-theme", theme.value);
  try {
    localStorage.setItem(THEME_KEY, theme.value);
  } catch {
    /* ignore */
  }
}

function clearFilters() {
  query.value = "";
  selectedTag.value = "all";
}

function syncQueryParams() {
  const url = new URL(window.location.href);
  if (query.value.trim()) url.searchParams.set("q", query.value.trim());
  else url.searchParams.delete("q");
  if (selectedTag.value !== "all") url.searchParams.set("tag", selectedTag.value);
  else url.searchParams.delete("tag");
  history.replaceState(null, "", url);
}

function onKeydown(e) {
  const meta = e.ctrlKey || e.metaKey;
  if (meta && e.key.toLowerCase() === "k") {
    e.preventDefault();
    searchOpen.value = !searchOpen.value;
    return;
  }
  if (e.key === "Escape") searchOpen.value = false;
  if (!searchOpen.value && (e.key === "/" || e.key.toLowerCase() === "s")) {
    const tag = (e.target && e.target.tagName) || "";
    if (tag === "INPUT" || tag === "TEXTAREA") return;
    e.preventDefault();
    document.getElementById("modSearch")?.focus();
  }
}

watch([query, selectedTag], syncQueryParams);

onMounted(async () => {
  try {
    if (localStorage.getItem(ANNOUNCE_KEY) === "1") announceVisible.value = false;
    const storedTheme = localStorage.getItem(THEME_KEY);
    if (storedTheme === "dark" || storedTheme === "light") {
      theme.value = storedTheme;
      document.documentElement.setAttribute("data-theme", storedTheme);
    }
  } catch {
    /* ignore */
  }

  const url = new URL(window.location.href);
  if (url.searchParams.get("q")) query.value = url.searchParams.get("q") || "";
  if (url.searchParams.get("tag")) selectedTag.value = url.searchParams.get("tag") || "all";

  window.addEventListener("keydown", onKeydown);
  await load();
  try {
    const res = await fetch(assetUrl("search-index.json"), { cache: "no-cache" });
    if (res.ok) searchIndex.value = await res.json();
  } catch {
    /* ignore */
  }
});

onUnmounted(() => {
  window.removeEventListener("keydown", onKeydown);
});
</script>

<template>
  <div class="hub">
    <a class="skip-link" href="#main-content">Skip to content</a>

    <div v-if="announceVisible" class="announce" role="status">
      <span class="announce-icon" aria-hidden="true">🎁</span>
      <p>
        <strong>New:</strong> Gifting Assistant — daily gift roster, loved/liked icons with bag
        counts, Todo sync, and Almanac progress.
      </p>
      <a class="announce-link" href="GiftingAssistant/GiftingAssistant.html">See what's inside →</a>
      <button class="announce-dismiss" type="button" aria-label="Dismiss announcement" @click="dismissAnnounce">
        ×
      </button>
    </div>

    <header class="topbar">
      <a class="brand" href="#main-content">
        <span class="brand-mark">SunhavenMod</span>
        <span class="brand-sub">Mod Hub</span>
      </a>
      <nav class="topbar-nav" aria-label="Page sections">
        <a href="#mods">Mods</a>
        <a href="#party-loadouts">Starter Packs</a>
        <a href="#guild-initiation">Install</a>
        <a href="#guild-master-wisdom">FAQ</a>
        <a href="#arsenal-comparison">Compare</a>
      </nav>
      <div class="topbar-actions">
        <button class="ghost-btn" type="button" @click="searchOpen = true">Search</button>
        <button class="ghost-btn" type="button" :aria-label="'Switch to ' + (theme === 'dark' ? 'light' : 'dark') + ' theme'" @click="toggleTheme">
          {{ theme === "dark" ? "Light" : "Dark" }}
        </button>
        <span class="kbd-hint" aria-hidden="true"><kbd>Ctrl</kbd>+<kbd>K</kbd></span>
      </div>
    </header>

    <header class="hero">
      <div class="shell hero-inner">
        <p class="brand-hero">SunhavenMod</p>
        <h1>Make every day in Haven <span>a little brighter</span></h1>
        <p class="lead">
          Thirteen quality-of-life BepInEx mods by AzraelGodKing — track your museum, sort your
          chests, bank your tokens, remember birthdays, and plan your farm. Each standalone; all
          better together.
        </p>
        <div class="cta-row">
          <a class="btn btn-primary" href="#mods">Browse the mods</a>
          <a class="btn btn-ghost" href="#guild-initiation">Install guide</a>
        </div>
        <div class="hero-stats" aria-live="polite">
          <div>
            <strong>{{ loading ? "…" : formatInt(siteTotal.grand_total) }}</strong>
            <span>Total downloads</span>
          </div>
          <div>
            <strong>{{ loading ? "…" : formatInt(siteTotal.thunderstore) }}</strong>
            <span>Thunderstore</span>
          </div>
          <div>
            <strong>{{
              loading
                ? "…"
                : formatNexus(siteTotal.nexus_total, siteTotal.nexus_unique)
            }}</strong>
            <span>Nexus Mods</span>
          </div>
          <div>
            <strong>{{ HUB_MODS.length }}</strong>
            <span>Mods maintained</span>
          </div>
        </div>
        <p v-if="error" class="state-error">Could not load live stats: {{ error }}</p>
      </div>
      <div class="hero-wash" aria-hidden="true"></div>
    </header>

    <main id="main-content" class="shell stack">
      <section id="mods" class="section">
        <div class="section-head">
          <h2>The Mods</h2>
          <p>Every mod keeps per-character saves, ships with sensible defaults, and stays out of your way until you need it.</p>
        </div>

        <div class="filter-bar">
          <label class="search-field">
            <span class="sr-only">Search mods</span>
            <input
              id="modSearch"
              v-model.trim="query"
              type="search"
              placeholder="Search by name, feature, or description…  ( / )"
              autocomplete="off"
            />
          </label>
          <div class="tags" role="group" aria-label="Filter mods by tag">
            <button
              v-for="tag in FILTER_TAGS"
              :key="tag"
              type="button"
              class="tag"
              :class="{ active: selectedTag === tag }"
              @click="selectedTag = tag"
            >
              {{ tag === "all" ? "All" : tag }}
            </button>
          </div>
          <p class="count" aria-live="polite">
            Showing {{ filteredMods.length }} of {{ HUB_MODS.length }}
          </p>
        </div>

        <div class="mods-grid">
          <article
            v-for="mod in filteredMods"
            :key="mod.id"
            class="mod-card"
            :style="{ '--accent': mod.accent }"
          >
            <div class="mod-card-top">
              <img class="mod-icon" :src="assetUrl(mod.icon)" :alt="''" width="64" height="64" loading="lazy" />
              <div>
                <h3>{{ mod.name }}</h3>
                <p class="tagline">{{ mod.tagline }}</p>
              </div>
              <span v-if="versionFor(mod)" class="version">{{ versionFor(mod) }}</span>
            </div>
            <p class="desc">{{ mod.description }}</p>
            <div class="features">
              <span v-for="f in mod.features" :key="f">{{ f }}</span>
            </div>
            <div class="mod-foot">
              <div class="dls" aria-label="Recorded downloads">
                <span title="Thunderstore">⚡ {{ modStat(mod, "thunderstore") }}</span>
                <span title="Nexus Mods">📦 {{ modStat(mod, "nexus") }}</span>
                <span title="Combined total">⬇ {{ modStat(mod, "combined") }}</span>
              </div>
              <span class="badge" :class="statusClass(mod.status)">{{ mod.status }}</span>
            </div>
            <a class="mod-cta" :href="mod.href">Open mod page →</a>
          </article>
        </div>

        <p v-if="filteredMods.length === 0" class="empty" role="status">
          No mods match your search or filter.
          <button type="button" class="linkish" @click="clearFilters">Clear and show all</button>
        </p>
      </section>

      <section id="party-loadouts" class="section">
        <div class="section-head">
          <h2>Starter Packs</h2>
          <p>Not sure where to begin? These bundles suit common playstyles — every mod also works fine alone.</p>
        </div>
        <div class="packs-grid">
          <article
            v-for="pack in STARTER_PACKS"
            :key="pack.id"
            class="pack"
            :class="{ wide: pack.wide }"
            :style="{ '--accent': pack.accent }"
          >
            <h3>{{ pack.icon }} {{ pack.name }}</h3>
            <p>{{ pack.desc }}</p>
            <div class="pack-mods">
              <a v-for="m in pack.mods" :key="m.href + m.label" :href="m.href">{{ m.label }}</a>
            </div>
          </article>
        </div>
      </section>

      <section id="guild-initiation" class="section">
        <div class="section-head">
          <h2>Install in Four Steps</h2>
          <p>
            Built for <strong>Sun Haven on PC</strong> with <strong>BepInEx 5.x</strong>. Exact
            game-build compatibility varies by release — check each mod's page, and
            <code>BepInEx/LogOutput.log</code> if a plugin fails to load.
          </p>
        </div>
        <div class="install-grid">
          <article v-for="(step, idx) in INSTALL_STEPS" :key="step.title">
            <span class="step-num">{{ idx + 1 }}</span>
            <h3>{{ step.title }}</h3>
            <p v-html="step.html"></p>
          </article>
        </div>
      </section>

      <section id="guild-master-wisdom" class="section">
        <div class="section-head">
          <h2>Frequently Asked</h2>
        </div>
        <div class="faq">
          <details v-for="item in FAQ_ITEMS" :key="item.q">
            <summary>{{ item.q }}</summary>
            <div v-if="item.aHtml" class="faq-body" v-html="item.a"></div>
            <div v-else class="faq-body" v-html="item.a"></div>
          </details>
        </div>
      </section>

      <section id="arsenal-comparison" class="section">
        <div class="section-head">
          <h2>Feature Comparison</h2>
          <p>On a phone or narrow window, scroll the table sideways to see every column.</p>
        </div>
        <div class="table-scroll">
          <table>
            <thead>
              <tr>
                <th v-for="h in COMPARE_HEADERS" :key="h">{{ h }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in COMPARE_ROWS" :key="row[0]">
                <td>{{ row[0] }}</td>
                <td
                  v-for="(cell, i) in row.slice(1)"
                  :key="i"
                  :class="cell ? 'yes' : 'no'"
                >
                  {{ cell ? "Yes" : "—" }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </main>

    <footer class="footer">
      <div class="shell">
        <p class="footer-title">Sun Haven Mod Hub — built & maintained by AzraelGodKing</p>
        <p class="footer-links">
          <a href="https://www.nexusmods.com/profile/AzraelGodKing/mods" target="_blank" rel="noopener noreferrer">Nexus Mods</a>
          ·
          <a href="https://thunderstore.io/c/sun-haven/p/AzraelGodKing/" target="_blank" rel="noopener noreferrer">Thunderstore</a>
          ·
          <a href="https://github.com/AzraelGodKing/SunhavenMod" target="_blank" rel="noopener noreferrer">GitHub</a>
          ·
          <a href="https://discord.gg/Vwh2y7qMXv" target="_blank" rel="noopener noreferrer">Discord</a>
          ·
          <a href="icon-comparison.html">Icon Refresh: Old vs New</a>
        </p>
        <p class="footer-note">Not affiliated with Sun Haven or Pixel Sprout Studios</p>
      </div>
    </footer>

    <div v-if="searchOpen" class="search-modal" role="dialog" aria-modal="true" aria-label="Site search">
      <div class="search-panel">
        <input
          v-model.trim="searchQuery"
          type="search"
          placeholder="Search docs…"
          autofocus
        />
        <ul>
          <li v-for="hit in searchHits" :key="hit.u + hit.t">
            <a :href="hit.u" @click="searchOpen = false">
              <strong>{{ hit.t }}</strong>
              <span>{{ hit.d }}</span>
            </a>
          </li>
        </ul>
        <button type="button" class="ghost-btn" @click="searchOpen = false">Close</button>
      </div>
    </div>
  </div>
</template>
