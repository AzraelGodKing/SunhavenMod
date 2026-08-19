# Docs hub (Vue) — Agent Guide

> Source for the GitHub Pages **mod hub** lives in `docs-app/`. Built assets are merged into `docs/` for Pages (`/docs` folder on `main`).

## What stays stable in `docs/`

Do **not** move or rename lightly:

| Path | Public URL |
|------|------------|
| `docs/versions.json` | `/versions.json` |
| `docs/data/stats-cache.json` | `/data/stats-cache.json` |
| `docs/mod-matrix.json` | `/mod-matrix.json` |
| `docs/search-index.json` | `/search-index.json` |
| Per-mod `docs/{Folder}/{Page}.html` | `/{Folder}/{Page}.html` |

Section hashes on the hub: `#mods`, `#party-loadouts`, `#guild-initiation`, `#guild-master-wisdom`, `#arsenal-comparison`.

## Develop

```bash
npm run docs:hub:install
npm run docs:hub:dev          # http://localhost:5174/SunhavenMod/
```

Vite serves live JSON/icons from `../docs` under the `/SunhavenMod/` prefix.

## Publish into docs/

```bash
npm run docs:hub:build        # vite build + merge into docs/
```

Merge copies `dist/index.html` + `dist/assets/*` into `docs/`, removes legacy hub-only CSS (`index-style.css`, `styles.css`), and **preserves** JSON, `data/`, and per-mod guides.

Per-mod guide pages remain static HTML + `shared.js` / `shared-styles.css` / `scripts/stats-display.js`.
