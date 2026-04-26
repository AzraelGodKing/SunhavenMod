# Changelog

## 2026-04-24
### Web
- Reworked `/api/feedback` Pages Function logging into single-line JSON (`service`, `component`, `event`, `failureReason`, `linearHttpStatus`, previews, GraphQL error summaries, `cfRay`, etc.) in `functions/api/feedback.js` and `cloudflare-site/functions/api/feedback.js`, so Workers Logs queries can pinpoint Linear vs validation vs config failures without logging tokens or user-submitted copy.

## 2026-04-23
### Web
- Redesigned the Cloudflare landing page experience in `cloudflare-site/app.js` with a stronger hero, dual call-to-action flow, live snapshot panel, and a featured-mods showcase that highlights top projects dynamically from cached stats.
- Added richer supporting styles in `cloudflare-site/styles.css` for spotlight cards, feature lists, responsive landing layouts, and visual progress meters to create a more playful, premium presentation.
- Added Animate.css CDN styling to `cloudflare-site/index.html` and `cloudflare-site/downloads.html`, and wired landing sections to animation classes for smoother first-load motion.
- Added an `All mods` section directly on the landing page with a toggle to expand from a curated preview to the full mod list, so users can browse everything without leaving Home.
- Re-themed the landing page visuals to a warmer fantasy palette (gold/pink/violet accents) with page-specific background and card styling distinct from the downloads dashboard look.
- Added per-mod Cloudflare pages under `cloudflare-site/mods/*.html`, each with its own URL and unique mod profile presentation backed by shared Vue logic in `cloudflare-site/mod-page.js`.
- Wired landing/download cards to each mod detail page and added `cloudflare-site/mod-page.css` with accessible, colorblind-friendly design defaults (high contrast, explicit labels, and visible keyboard focus states).
- Expanded mod detail customization so each page now has mod-specific motif/story/panel labeling, and added a stronger bank-inspired treatment for `The Vault` page (ledger-toned palette and vault-styled cards).
- Added extra “pizzazz” for `The Vault` profile with a premium vault showcase section, emblem lockup, stronger metallic hero treatment, and richer ledger-style stat presentation.
- Added a subtle vault-door hover/focus animation on The Vault emblem with `prefers-reduced-motion` handling to keep the effect accessible.
- Added a small pressed-state “unlock click” microinteraction for The Vault emblem on active/click while preserving reduced-motion fallbacks.
- Pulled presentation cues from the docs hub into Cloudflare mod pages: related-mod strip, hero metadata badges, and quick-jump section nav for better structure and easier scanning.
- Reworked Cloudflare Home and Downloads into more custom page experiences (guild-style landing messaging, command-board downloads framing, lane/icon metadata, top-movers panel, and richer section copy/layout).
- Expanded mod-page customization with per-mod play-loop panels so each mod profile includes unique scenario-driven content beyond palette/theme changes.
- Added true per-mod layout variants for mod detail pages (`timeline`, `checklist`, `console`, `dashboard`, `spotlight`, plus `vault`), so structure now changes by mod type and not just styling/content.
- Added subtle, layout-specific micro-animations for mod detail variants (timeline/checklist/console/dashboard/spotlight) with `prefers-reduced-motion` safeguards.
- Added page-level ambient animations across Home, Downloads, and mod detail layouts (timeline/checklist/console/dashboard/spotlight/vault), each with reduced-motion fallbacks for accessibility.
- Added richer per-mod context blocks (`overview`, `best for`, `synergy`) plus a changelog-style release panel on mod pages populated from Thunderstore version telemetry in `stats-cache.json`.
- Updated mod-page Nexus CTA to use direct per-mod Nexus URLs sourced from `docs/versions.json` (with search fallback only if a direct URL is unavailable).
- Fixed page animation layering to prevent dark/black overlay artifacts by explicitly stacking content above ambient animation layers on Home, Downloads, and mod pages.
- Added a persistent Ambient toggle control (Home/Downloads + mod pages) backed by `localStorage` so users can disable animations globally without code edits.
- Removed the mod-page release changelog panel and related telemetry list rendering, keeping the context section focused on overview/best-for/synergy content.
- Added a Downloads-page “Live feed” ticker animation for top combined download stats, including hover-to-pause and reduced-motion fallback behavior.
- Removed the mod-page header metadata line showing `Mod key` and `Cache refreshed` to keep the hero cleaner.
- Removed the Home/Downloads header “Telemetry refreshed” timestamp pill for a cleaner top section.
- Removed the Downloads-page live feed ticker section and related animation styles.
- Added a visual-first polish pass with landing starter bundles, downloads lane filter + tactical badges, and mod-page utility sections (trust strip, 60-second install guide, and direct issue/feature links).
- Refined the `Haven's Almanac` mod-page art direction with a stronger morning aesthetic (sunrise gradients, warmer highlights, and softer dawn-toned panel surfaces).
- Refined the `Crop Optimizer` mod-page theme with a stronger farm feel (earthy green/gold palette, field-toned surfaces, and harvest-style accents).
- Refined the `Sun Haven Todo` mod-page art direction with a stronger task/list identity (checklist-paper styling, dashed task cards, and list-focused purple notebook palette).
- Refined the `Birthday Reminder` mod-page art direction with a stronger birthday feel (celebration palette, confetti-like highlights, and festive timeline card treatment).
- Added Motion One powered entrance animations for Home/Downloads/mod pages, plus lane shape-language refinements, layout-level styling variants, and downloads result-count feedback while preserving reduced-motion safeguards.
- Expanded `/api/feedback` Pages Function logging for Linear failures (`functions/api/feedback.js` and mirrored `cloudflare-site/functions/api/feedback.js`): read body as text, safe `JSON.parse`, log HTTP status plus raw body, then either non-JSON raw text or parsed `errors`/`data`; failures throw a tagged upstream error and map to the same generic 502 message for clients.
- Set explicit `[observability] enabled = true` in `wrangler.toml` (and mirrored `azrael-sunhaven-website/wrangler.toml`) so Workers Logs / observability behavior stays consistent with Wrangler-driven deployments.

## 2026-04-22
### CI
- Guarded release tag context access in `.github/workflows/build-release-publish.yml` to avoid workflow validation warnings about `github.event.release`.
- Upgraded workflow artifact actions to Node 24-ready majors (`actions/upload-artifact@v6`, `actions/download-artifact@v7`) in active and deprecated release workflows.

### Scripts
- Renamed a local regex capture variable in `scripts/verify-version-consistency.ps1` to avoid assigning to PowerShell automatic variable names flagged by `PSAvoidAssignmentToAutomaticVariable`.
- Simplified brace-heavy error-string construction in `scripts/verify-version-consistency.ps1` to avoid parser/linter false positives about missing closing `}`.

### Docs
- Updated all mod Nexus BBCode assets to the current formatted template, synchronized versioned headers, and normalized section/link wording for consistent Nexus copy.
- Expanded `docs/NexusMods-BBCode-Index.txt` to include `CropOptimizer` and `HavensRespec`.

### Web
- Added `wrangler.toml` and `cloudflare-site/` so Cloudflare Pages work targets a separate directory instead of `docs/`, preserving existing GitHub Pages content.
- Added npm scripts for Cloudflare Pages (`cf:pages:dev`, `cf:pages:deploy`) so deployments use `wrangler pages deploy` instead of Worker-mode `wrangler deploy`.
- Replaced the Cloudflare placeholder page with a structured starter homepage in `cloudflare-site/index.html` and added `cloudflare-site/styles.css` for a reusable base style system.
- Upgraded `cloudflare-site` landing page to a more polished marketing-style hero with CTAs, stat highlights, feature framing, and improved visual styling while keeping the rollout static/simple.
- Added `.github/workflows/sync-cloudflare-site-mirror.yml` to mirror `cloudflare-site/` (plus `wrangler.toml` / `package*.json` when present) from `SunhavenMod` to `AzraelGodKing/azrael-sunhaven-website` on pushes to `main` (uses `AZRAEL_WEBSITE_SYNC_TOKEN` when set, otherwise falls back to `ADMIN_PUSH_TOKEN`).
- Adjusted mirror commit messages to avoid `[skip ci]` so Cloudflare Pages does not skip builds after mirrored pushes.
- Rebuilt the Cloudflare landing page as a Vue-powered “Download Pulse” dashboard that reads `docs/data/stats-cache.json` via GitHub Pages and the mod roster from `scripts/mod-matrix.json` (same stat id rules as `scripts/fetch-stats.js`).
- Added `cloudflare-site/downloads.html` and shared top navigation so Home and Downloads cross-link while reusing the same Vue app.
- Added an inline “Report a Bug / Request a Feature” form to the Cloudflare site with conditional fields, async in-page submission states, honeypot spam protection, and a secure server-side Pages Function endpoint (`cloudflare-site/functions/api/feedback.js`) that validates input and creates Linear issues.
- Moved feedback into its own page at `cloudflare-site/feedback.html`, updated shared navigation to Home/Downloads/Feedback, and scoped the form UI to the dedicated feedback route.
- Updated the feedback endpoint to consume optional `LINEAR_BUG_LABEL_ID` / `LINEAR_FEATURE_LABEL_ID` env vars for issue labeling in Linear, and documented that `CLOUDFLARE_API_TOKEN` is deploy-time only (not a runtime feedback secret).
- Simplified the feedback page to bug-report essentials only (`Name`, `Title`, `Description`) and aligned the server-side Linear payload/validation to the same minimal bug-only shape.
- Restored a simple `Type` selector (`bug` / `feature`) on the feedback page while keeping the compact form fields (`Name`, `Title`, `Description`), and mapped server-side labeling/title prefix back to type-specific behavior.
- Fixed Cloudflare Pages route deployment wiring for feedback submissions by adding a root `functions/api/feedback.js` entrypoint that re-exports the Cloudflare-site handler, so Wrangler can discover `POST /api/feedback` without unsupported CLI flags.
- Replaced the root feedback function re-export shim with a full standalone `functions/api/feedback.js` implementation to avoid function-bundling path issues and ensure `POST /api/feedback` is detected in Pages deployments.
- Updated `.github/workflows/sync-cloudflare-site-mirror.yml` to also mirror root `functions/` into `azrael-sunhaven-website`, so deployed mirror builds include `/api/feedback` server routes instead of returning `405`.
- Updated Cloudflare npm scripts to deploy in project-root mode (`wrangler pages deploy` / `wrangler pages dev` without explicit static directory argument), so Wrangler resolves `pages_build_output_dir` from `wrangler.toml` and reliably includes root `functions/` routes.
- Set `wrangler.toml` `name` to `azrael-sunhaven-website` to match the Cloudflare Pages project slug.
- Removed the redundant `Nexus (total)` metric from the Cloudflare Download Pulse totals row so Nexus is represented only once via `Nexus (unique)`.

### Docs
- Updated branch guidance from `master` to `main` in release/version docs and repo cleanup conventions, and narrowed `.github/workflows/sync-mod-versions.yml` push trigger to `main`.

## 2026-04-21
### Performance
- Fixed UI texture leaks by disposing stale textures before recreating them in chest and todo interfaces.
- Removed recurring runtime allocations in UI and item patch paths by caching styles, scene/season checks, and helper parsing flows.
- Added bounded eviction to `SharedUtilities/IconCache.cs` to prevent unbounded texture cache growth.

### CI
- Promoted the self-hosted release workflow to `.github/workflows/build-release-publish.yml`.
- Archived the previous release workflow to `.github/workflows/deprecated/build-release-publish.yml` for rollback reference.
