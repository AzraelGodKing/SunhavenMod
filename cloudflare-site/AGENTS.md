# SunhavenMod Cloudflare Site — Agent Guide

> This file describes the `cloudflare-site/` directory: the Vite + Vue 3 marketing site for the SunhavenMod suite, deployed to Cloudflare Pages.

## Project Overview

Static marketing and discovery portal for **SunhavenMod** (BepInEx mods for *Sun Haven*). Routes:

1. **Home** (`/`) — brand-first landing, featured mods, starter bundles, registry
2. **Downloads** (`/downloads`) — searchable/sortable live download board
3. **Mod profiles** (`/mods/:modKey`) — themed per-mod pages
4. **Feedback** (`/feedback`) — bug/feature form → Pages Function → Linear

Deploy target: Cloudflare Pages project **`azrael-sunhaven-website`**.

## Technology Stack

| Layer | Technology |
|-------|------------|
| Bundler | Vite 8 |
| UI | Vue 3 (Composition API + `<script setup>`) |
| Routing | Vue Router 4 (HTML5 history) |
| Styling | Handcrafted CSS (`src/assets/main.css`), CSS variables |
| Fonts | Syne + Fraunces + Figtree (Google Fonts) |
| Serverless | Cloudflare Pages Functions (`functions/api/feedback.js`) |
| Runtime data | Fetched client-side from GitHub-hosted JSON |

## Local development

From repo root:

```bash
npm run cf:site:install
npm run cf:site:dev
```

Or inside `cloudflare-site/`:

```bash
npm install
npm run dev
```

Production build:

```bash
npm run cf:site:build
# output: cloudflare-site/dist
```

Root `wrangler.toml` points at `cloudflare-site/dist`. Deploy:

```bash
npm run cf:pages:deploy
```

SPA fallback: `public/_redirects` → `/*/index.html 200`.

## File Organization

```
cloudflare-site/
├── index.html
├── package.json
├── vite.config.js
├── public/
│   ├── _redirects
│   └── favicon.svg
├── functions/api/feedback.js
├── src/
│   ├── main.js
│   ├── App.vue
│   ├── assets/main.css
│   ├── router/index.js
│   ├── data/mods.js          # presentation + STATS_ID_BY_MOD_KEY
│   ├── data/urls.js
│   ├── composables/
│   ├── components/
│   └── views/
└── dist/                     # build output (gitignored)
```

Root `functions/api/feedback.js` is kept in sync for Pages Function discovery beside `wrangler.toml`.

## Data Sources

| URL | Purpose |
|-----|---------|
| `scripts/matrix/mod-matrix.json` (raw GitHub) | Canonical mod list |
| `docs/data/stats-cache.json` (GitHub Pages) | Download telemetry |
| `docs/versions.json` (raw GitHub) | Versions + Nexus URLs |

`STATS_ID_BY_MOD_KEY` in `src/data/mods.js` must stay aligned with `scripts/stats/fetch-stats.js`.

## Design notes

- Light, sun-washed coastal palette (tide teal + sun gold + meadow) — not purple/indigo defaults
- Brand (`SunhavenMod`) is the hero-level signal on the home viewport
- Prefer sections and strips over card-heavy dashboards on landing
- Respect `prefers-reduced-motion`; ambient motion can be toggled in the nav

## Mirror workflow

[`.github/workflows/sync-cloudflare-site-mirror.yml`](../.github/workflows/sync-cloudflare-site-mirror.yml) builds the Vite app and syncs **`dist/`** into `AzraelGodKing/azrael-sunhaven-website` as `cloudflare-site/` (static), plus root `functions/`.

## Legacy paths

Router redirects keep old static HTML URLs working (`/downloads.html`, `/mods/:key.html`, etc.).
