# SunhavenMod Cloudflare Site — Agent Guide

> This file describes the `cloudflare-site/` directory, which is the static website deployed to Cloudflare Pages for the SunhavenMod project. It is a subdirectory of the larger `SunhavenMod` monorepo.

## Project Overview

This is a handcrafted, static marketing and discovery portal for **SunhavenMod** — a collection of BepInEx mods for the game *Sun Haven*. The site serves three primary purposes:

1. **Landing page** (`index.html`) — curated discovery with featured mods, "top movers," and starter bundle recommendations.
2. **Downloads command board** (`downloads.html`) — sortable, searchable, filterable registry of all mods with live download telemetry.
3. **Per-mod profile pages** (`mods/*.html`) — individually themed pages for each mod with stats, install instructions, and context.
4. **Feedback desk** (`feedback.html`) — inline bug report / feature request form backed by a serverless function.

The site is deployed to **Cloudflare Pages** (project name: `azrael-sunhaven-website`). There is no build step or bundler — all files are hand-written static assets.

## Technology Stack

| Layer | Technology |
|-------|------------|
| Markup | HTML5 (vanilla, no templating engine) |
| Styling | CSS3 custom properties, `color-mix()`, CSS Grid, media queries |
| Frontend framework | Vue 3.5.13 via ES modules (`https://esm.sh/vue@3.5.13/dist/vue.esm-browser.prod.js`) |
| Animation | Motion 12.23.24 via ES modules (`https://esm.sh/motion@12.23.24`) + Animate.css 4.1.1 CDN |
| Serverless | Cloudflare Pages Functions (`functions/api/feedback.js`) |
| Runtime data | Fetched at runtime from GitHub-hosted JSON files (see below) |

There is **no package.json, no bundler, and no transpilation** inside `cloudflare-site/`. The root `package.json` and `wrangler.toml` live one directory up and are used for deployment orchestration and Node-based stat-fetching scripts.

## File Organization

```
cloudflare-site/
├── index.html                 # Landing page (data-page="landing")
├── downloads.html             # Registry / command board (data-page="downloads")
├── feedback.html              # Feedback form (data-page="feedback")
├── 404.html                   # Themed 404 with 3 randomized visual themes
├── app.js                     # Vue app shared by index, downloads, and feedback
├── styles.css                 # Shared stylesheet for app.js pages
├── mod-page.js                # Vue app for per-mod profile pages
├── mod-page.css               # Stylesheet for per-mod profile pages
├── mods/
│   ├── cropoptimizer.html     # Thin HTML shells: <html data-mod-key="cropoptimizer">
│   ├── fasterraces.html
│   ├── havendevtools.html
│   ├── havensalmanac.html
│   ├── havensbirthright.html
│   ├── havensrespec.html
│   ├── senpaischest.html
│   ├── squirrelsbirthdayreminder.html
│   ├── sunhavenmuseumutilitytracker.html
│   ├── sunhaventodo.html
│   ├── thevault.html
│   └── trinketfortune.html
└── functions/
    └── api/
        └── feedback.js        # Cloudflare Pages Function: POST /api/feedback
```

### How the Vue apps work

- **`app.js`** mounts to `#app` on `index.html`, `downloads.html`, and `feedback.html`. It reads `document.documentElement.dataset.page` to decide which view to render.
- **`mod-page.js`** mounts to `#app` on every file in `mods/`. It reads `document.documentElement.dataset.modKey` to know which mod profile to load.

Both apps fetch external JSON data at runtime (see Data Sources).

## Data Sources

All stats and metadata are fetched client-side from GitHub-hosted files. These are the **same caches** used by the docs hub (GitHub Pages), so changes there automatically reflect here.

| URL | Purpose | Used By |
|-----|---------|---------|
| `https://raw.githubusercontent.com/AzraelGodKing/SunhavenMod/main/scripts/mod-matrix.json` | Canonical list of all mods with keys, names, Thunderstore slugs, docs paths | `app.js`, `mod-page.js` |
| `https://azraelgodking.github.io/SunhavenMod/data/stats-cache.json` | Download stats (Thunderstore + Nexus totals, unique counts, combined totals, site aggregates) | `app.js`, `mod-page.js` |
| `https://raw.githubusercontent.com/AzraelGodKing/SunhavenMod/main/docs/versions.json` | Version numbers and Nexus URLs per mod | `mod-page.js` |

### Important: `STATS_ID_BY_MOD_KEY`

Both `app.js` and `mod-page.js` contain a hardcoded mapping `STATS_ID_BY_MOD_KEY` that translates internal `modKey` values (e.g., `sunhaventodo`) into the stats-cache IDs (e.g., `havens-todo`). **This must stay aligned with `scripts/fetch-stats.js` in the parent repo.** If a new mod is added or a stats ID changes, update this mapping in both JS files.

## Mod System Conventions

### Mod keys
Every mod has a stable `modKey` used in filenames, data attributes, and JS lookup tables:
- `senpaischest`, `havensbirthright`, `sunhavenmuseumutilitytracker`, `squirrelsbirthdayreminder`, `sunhaventodo`, `thevault`, `havendevtools`, `havensalmanac`, `fasterraces`, `trinketfortune`, `cropoptimizer`, `havensrespec`

### Lanes
Mods are grouped into gameplay "lanes" (categories): `Storage`, `Races`, `Tracking`, `Social`, `Planning`, `Currency`, `Dev`, `Dashboard`, `Movement`, `Fishing`, `Farming`, `Skills`, `QoL`.

### Per-mod presentation metadata
`mod-page.js` contains three large lookup tables that drive the visual personality of each mod page:

- **`MOD_PRESENTATION`** — icon, status, tags, related mods
- **`MOD_PROFILES`** — theme class, tagline, motif, context, bestFor, synergy, story, highlights, panel titles
- **`MOD_SCENES`** — 3 bullet points describing the player experience loop
- **`MOD_LAYOUTS`** — layout type: `spotlight` (default), `timeline`, `checklist`, `console`, `dashboard`, `vault`

### Themes
Each mod profile can apply a CSS theme class (e.g., `theme-amber`, `theme-teal`, `theme-vault`). `mod-page.css` contains extensive per-theme overrides. New themes require CSS additions.

### Layout types
The `layoutType` determines how the "Play loop fit" section renders:
- `spotlight` — numbered steps (default)
- `timeline` — vertical timeline with dots
- `checklist` — interactive checkbox list
- `console` — monospace console log aesthetic
- `dashboard` — pill widgets
- `vault` — custom bank/vault showcase with emblem and ledger (special-cased for `thevault`)

## CSS Architecture

### `styles.css` (app pages)
- Uses CSS custom properties scoped to `:root` and overridden via `html[data-page="..."]`
- Two primary palettes:
  - **Landing** (`data-page="landing"`) — warm purple/gold tones (`#1c1328` background)
  - **Downloads/Feedback** (`data-page="downloads"`) — cool blue tones (`#050b18` background)
- Ambient background animations via `.page-anim` spans (floating orbs on landing, sweep lines on downloads)
- Lane accent colors via `.lane-{name}` classes with CSS custom property `--lane-accent`
- `prefers-reduced-motion: reduce` disables ambient animations

### `mod-page.css` (mod profile pages)
- Custom property namespace: `--mp-*` (e.g., `--mp-bg-a`, `--mp-accent`)
- Ambient animations per layout type (`.anim-timeline`, `.anim-checklist`, `.anim-console`, etc.)
- Extensive per-theme overrides (`.mod-page.theme-amber`, `.theme-teal`, `.theme-vault`, etc.)
- Special `.vault-showcase` and `.vault-emblem` components for The Vault mod

## Build and Development Commands

There is **no build step** for the website itself. Files are edited directly.

From the **parent repo root** (one directory up from `cloudflare-site/`):

```bash
# Local Cloudflare Pages dev server (includes Functions)
npm run cf:pages:dev

# Deploy to Cloudflare Pages
npm run cf:pages:deploy

# Update download stats cache (Node script in parent repo)
npm run stats
npm run stats:force

# Sync mod-matrix from docs to scripts
npm run sync-mod-matrix
```

These commands rely on `wrangler.toml` and `package.json` in the parent directory.

## Deployment

- **Primary host**: Cloudflare Pages (project `azrael-sunhaven-website`)
- **Build output directory**: `cloudflare-site` (configured in `wrangler.toml`)
- **Mirror workflow**: `.github/workflows/sync-cloudflare-site-mirror.yml` mirrors `cloudflare-site/`, `functions/`, `wrangler.toml`, `package.json`, and `package-lock.json` to a separate repo (`AzraelGodKing/azrael-sunhaven-website`) on every push to `main` that touches those paths.
- Cloudflare Pages automatically builds from the mirror repo.

## Testing

There are **no automated tests** for the website frontend code. The parent repo contains .NET unit tests for the actual game mods, but the site is tested manually.

Manual test checklist when making changes:
1. Load `index.html` — verify stats load, animations run, lane colors display
2. Load `downloads.html` — verify search, sort, lane filter, and mod card links
3. Load any `mods/*.html` — verify profile data loads, theme applies, layout renders correctly
4. Test ambient toggle in nav — verify `localStorage` persists preference
5. Test with `prefers-reduced-motion: reduce` — verify animations are disabled
6. Test `feedback.html` form submission (requires local dev server with Functions)

## Cloudflare Pages Function: `/api/feedback`

`functions/api/feedback.js` handles bug reports and feature requests.

### Required environment variables
| Variable | Purpose |
|----------|---------|
| `LINEAR_API_TOKEN` | Linear API bearer token |
| `LINEAR_TEAM_ID` | Linear team ID for issue creation |

### Optional environment variables
| Variable | Default | Purpose |
|----------|---------|---------|
| `FEEDBACK_RATE_WINDOW_SECONDS` | `600` | Rate-limit window |
| `FEEDBACK_RATE_MAX` | `5` | Max submissions per window per IP |
| `LINEAR_BUG_LABEL_ID` | — | Linear label ID for bugs |
| `LINEAR_FEATURE_LABEL_ID` | — | Linear label ID for features |

### Behavior
- `GET /api/feedback` — health check; returns whether the function is configured
- `POST /api/feedback` — accepts `{ type, name, title, description, website, mod?, priority? }`
  - `website` is a honeypot; if non-empty, rejects as spam
  - Validates `type` is `bug` or `feature`
  - Rate-limits by `CF-Connecting-IP` / `x-forwarded-for`
  - Sanitizes control characters from all text fields
  - Creates a Linear issue via GraphQL mutation
  - Returns `{ ok, id, identifier, url }` on success
  - Structured JSON logging; never logs tokens or user text

## Security Considerations

- **No sensitive data in source**: API tokens are env vars only.
- **Honeypot field**: `website` field in feedback form — invisible to humans, catches bots.
- **Input sanitization**: Control characters (`\u0000`–`\u001F`) are stripped from feedback text.
- **Rate limiting**: Per-IP sliding window with configurable max and duration.
- **Safe logging**: The feedback function logs structured JSON but never includes the Linear token or raw user input.
- **CORS**: Not explicitly configured; the function is intended to be called same-origin.

## Accessibility Conventions

- `prefers-reduced-motion: reduce` is respected everywhere (ambient animations, hover transforms, vault emblem spins)
- Focus-visible styles: `outline: 3px solid #ffffff; outline-offset: 2px;` on interactive elements
- `aria-hidden="true"` on all decorative ambient animation elements
- Form labels are explicit and visible
- Color contrast is maintained across all themes

## Adding a New Mod Page

1. Create `mods/{modkey}.html`:
   ```html
   <!doctype html>
   <html lang="en" data-mod-key="modkey">
     <head>
       <meta charset="utf-8" />
       <meta name="viewport" content="width=device-width, initial-scale=1" />
       <title>SunhavenMod | Display Name</title>
       <meta name="description" content="Display Name mod profile and download stats." />
       <link rel="stylesheet" href="../mod-page.css" />
     </head>
     <body>
       <div id="app"></div>
       <script type="module" src="../mod-page.js"></script>
     </body>
   </html>
   ```
2. Add the mod to `scripts/mod-matrix.json` in the parent repo.
3. Add entries to `STATS_ID_BY_MOD_KEY` in **both** `app.js` and `mod-page.js`.
4. Add entries to `MOD_META` in `app.js` (icon + lane).
5. Add entries to `MOD_PRESENTATION`, `MOD_PROFILES`, `MOD_SCENES`, and `MOD_LAYOUTS` in `mod-page.js`.
6. If a new theme is needed, add CSS rules to `mod-page.css`.
7. If a new layout type is needed, add template branches in `mod-page.js` and CSS in `mod-page.css`.

## Code Style Guidelines

- Use vanilla JS (ES2020+). No TypeScript, no transpilation.
- Import Vue and Motion from `esm.sh` CDN URLs with pinned versions.
- Use Vue Options API (not Composition API) to keep files single-module and simple.
- CSS custom properties for theming; avoid hardcoding colors in component logic.
- `localStorage` keys are prefixed with `sunhavenmod-`.
- Comments use `//` for inline and `/** ... */` for doc blocks.
- Error handling: catch and assign to `this.error`; avoid unhandled promise rejections.
