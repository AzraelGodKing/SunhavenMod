# Maintainer Changelog

Internal engineering log for maintainers.

Use this file for changes players do not need in the public changelog:
- CI/workflow updates
- build/release pipeline changes
- script/tooling refactors
- docs infrastructure and automation changes
- internal observability/logging improvements

Keep player-facing behavior updates in `CHANGELOG.md`.

## 2026-05-02
- Reusable **`reusable-mod-matrix-setup.yml`** + concurrency on **Release & Publish** / **Test — Self-hosted** workflows.
- **`verify-version-consistency.py`:** stray `manifest.json` scan under each mod dir; Python verifier canonical; PowerShell wrapper delegates.
- **`scripts/mod-matrix.json`:** `statsDomId`; **`sync-docs-mod-matrix.js`** regenerates **`docs/mod-matrix.json`**; **`fetch-stats.js`** reads `statsDomId` from matrix.
- **`Directory.Build.props`:** `LangVersion` latest, `Deterministic`, `ContinuousIntegrationBuild` on GitHub Actions.
- Policy files: **`LICENSE`**, **`CONTRIBUTING.md`**, **`SECURITY.md`**, **`CODE_OF_CONDUCT.md`**, **`docs/SHARED_CODE_STRATEGY.md`**.
- Removed tracked **`HavenDevTools/manifest.json`** duplicate; **`.gitignore`** patterns for local `*_decompiled.cs`.

## 2026-04-28
- Established changelog policy split:
  - `CHANGELOG.md` is player-facing.
  - `MAINTAINER_CHANGELOG.md` stores internal maintainer-only notes.

## 2026-04-26 (Backfilled Internal History)
- Web/docs telemetry presentation iteration: adjusted Nexus total vs unique display handling and aggregate formatting across GitHub Pages and Cloudflare pages.

## 2026-04-24 (Backfilled Internal History)
- Web feedback observability refinement: reworked Pages Function logging shape to structured single-line JSON for easier Worker log triage.

## 2026-04-23 (Backfilled Internal History)
- Cloudflare-site UX/theme iteration pass: extensive landing/download/mod-page layout, styling, and animation refinements with accessibility/reduced-motion handling.
- Feedback endpoint reliability and deployment wiring hardening for Pages Functions and mirror sync behavior.
- Cloudflare deployment/config consistency updates (including observability and project naming alignment).

## 2026-04-22 (Backfilled Internal History)
- CI workflow maintenance: release workflow guard updates and artifact action version upgrades.
- Script hygiene updates in version consistency tooling.
- Release/docs maintenance for BBCode/index coverage and branch guidance alignment.
- Cloudflare site setup and deployment pipeline expansion (wrangler config, scripts, mirror workflow, pages routes).
