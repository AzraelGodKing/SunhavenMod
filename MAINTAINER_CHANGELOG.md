# Maintainer changelog

Internal engineering log: **CI**, **release automation**, **scripts**, **docs infrastructure**, and **non–player-visible** observability. **Do not** duplicate full player-facing release notes here — those belong in [`CHANGELOG.md`](CHANGELOG.md), which is linked for cross-reference.

---

## 2026-05-02

- **Workflows:** Added [`reusable-mod-matrix-setup.yml`](.github/workflows/reusable-mod-matrix-setup.yml); **concurrency** groups on **Release & Publish** and **Test — Self-hosted** to reduce racing publishes.
- **Version verifier:** [`scripts/verify-version-consistency.py`](scripts/verify-version-consistency.py) scans for stray `manifest.json` under each mod directory; PowerShell script delegates to Python.
- **Mod matrix:** [`scripts/mod-matrix.json`](scripts/mod-matrix.json) includes `statsDomId`; [`sync-docs-mod-matrix.js`](scripts/sync-docs-mod-matrix.js) regenerates [`docs/mod-matrix.json`](docs/mod-matrix.json); [`fetch-stats.js`](scripts/fetch-stats.js) resolves stats IDs from the matrix.
- **Build:** [`Directory.Build.props`](Directory.Build.props) — `LangVersion` latest, `Deterministic`, `ContinuousIntegrationBuild` when `GITHUB_ACTIONS` is set.
- **Policy / governance:** Root [`LICENSE`](LICENSE), [`CONTRIBUTING.md`](CONTRIBUTING.md), [`SECURITY.md`](SECURITY.md), [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md), [`docs/SHARED_CODE_STRATEGY.md`](docs/SHARED_CODE_STRATEGY.md).
- **Hygiene:** Removed duplicate root `HavenDevTools/manifest.json`; `.gitignore` patterns for local `*_decompiled.cs` under Almanac `_refs/`.
- **Docs:** Rewrote root [`README.md`](README.md), [`CHANGELOG.md`](CHANGELOG.md), and this file — scoped maintainer vs player notes, dropped obsolete README changelog dump.

---

## 2026-04-28

- Established split: `CHANGELOG.md` = players; this file = maintainers.

---

## 2026-04-22 — 2026-04-26 (summary)

- Cloudflare / Pages / mirror workflows and site UX iterations; feedback endpoint and logging shape for triage.
- Hub telemetry presentation (Nexus aggregate display, formatting).

---

## Older entries

Before 2026-04-22, use `git log` and archived workflow README sections for fine-grained history. Large docs-site and CI narratives that previously lived only in the root README were retired in favor of this file + [`CHANGELOG.md`](CHANGELOG.md).
