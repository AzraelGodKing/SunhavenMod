# Maintainer changelog

Internal engineering log: **CI**, **release automation**, **scripts**, **docs infrastructure**, and **non–player-visible** observability. **Do not** duplicate full player-facing release notes here — those belong in [`CHANGELOG.md`](CHANGELOG.md), which is linked for cross-reference.

---

## 2026-05-03

- **`release-self-hosted-sunhaven-runner.yml`:** **`package_step.outputs.zip_path`** is the single source of truth for dry-run artifact upload, GitHub Release assets, Thunderstore `file`, and Nexus preflight fallback; **`test_discord`** packaging now emits `zip_path`; **preflight-success** guards all Nexus upload/retry/backoff steps; **`ignore_discord_notify`** workflow input matches **Release & Publish**; lightweight **Package diagnostics** step before dry-run completion / publishes.
- **`build-release-publish.yml`:** Comments clarify that `builds/` paths are **ephemeral on the runner** (stage → upload-artifact → download → package), not a committed tree — same idea as [`builds/README.md`](builds/README.md).
- **Nexus BBCode files:** Renamed from `NexusMods-BBCode.txt` to **`NexusMods-<ModFolder>-BBCode.txt`** in each project; [`scripts/pre-push-build.ps1`](scripts/pre-push-build.ps1) and [`scripts/stage-version-sync-files.py`](scripts/stage-version-sync-files.py) updated; hub list in [`docs/NexusMods-BBCode-Index.txt`](docs/NexusMods-BBCode-Index.txt).
- **Senpai's Chest:** Glob rules on item display names (`*` / `?`), `RuleType.ByNamePattern` + JSON `NamePattern`; group data now stores `NamePatterns` and `By Group` matches item IDs or wildcard patterns. New UI config: `SeparateWildcardRuleInUI` (default false; wildcard editing in Manage Groups unless explicitly separated).
- **Version bump:** `.\scripts\pre-push-build.ps1 -All -Bump patch` — patch bump for all twelve mods (`docs/versions.json`, manifests, Nexus BBCode headers, doc badges, `HavenDevTools` `DebugWindow` tuples, rebuild).

---

## 2026-05-02

- **Workflows:** Added [`reusable-mod-matrix-setup.yml`](.github/workflows/reusable-mod-matrix-setup.yml); **concurrency** groups on **Release & Publish** and **Test — Self-hosted** to reduce racing publishes.
- **Version verifier:** [`scripts/verify-version-consistency.py`](scripts/verify-version-consistency.py) scans for stray `manifest.json` under each mod directory; **matrix keys** in `scripts/mod-matrix.json` must exist in `docs/versions.json`; PowerShell script delegates to Python.
- **Mod matrix:** [`scripts/mod-matrix.json`](scripts/mod-matrix.json) includes `statsDomId`; [`sync-docs-mod-matrix.js`](scripts/sync-docs-mod-matrix.js) regenerates [`docs/mod-matrix.json`](docs/mod-matrix.json); [`fetch-stats.js`](scripts/fetch-stats.js) resolves stats IDs from the matrix; reusable matrix setup runs the generator and **`git diff --exit-code`** on `docs/mod-matrix.json`.
- **Sync workflow:** [`sync-mod-versions.yml`](.github/workflows/sync-mod-versions.yml) stages allowlisted paths via [`scripts/stage-version-sync-files.py`](scripts/stage-version-sync-files.py) (no broad `git add -A` / full-repo `-u`).
- **Release / mirror:** Self-hosted test workflow: Nexus **fail-closed** when publish requested but no upload succeeded; Nexus `upload-action` **v1.0.0-beta.4** aligned with main release workflow; [`sync-cloudflare-site-mirror.yml`](.github/workflows/sync-cloudflare-site-mirror.yml) uses `http.extraHeader` auth instead of token-in-URL; [`update-stats.yml`](.github/workflows/update-stats.yml) falls back to `github.token` when `ADMIN_PUSH_TOKEN` is unset.
- **Build:** [`Directory.Build.props`](Directory.Build.props) — `Nullable` annotations, `TreatWarningsAsErrors` when `GITHUB_ACTIONS` is set; `LangVersion` latest, `Deterministic`, `ContinuousIntegrationBuild`. **Senpai's Chest** suppresses **CS0436** (linked `SceneRootSurvivor` + `SunHavenMuseumUtilityTracker` reference embeds the same type).
- **Policy / governance:** Root [`LICENSE`](LICENSE), [`CONTRIBUTING.md`](CONTRIBUTING.md), [`SECURITY.md`](SECURITY.md), [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md), [`docs/SHARED_CODE_STRATEGY.md`](docs/SHARED_CODE_STRATEGY.md).
- **Hygiene:** Removed duplicate root `HavenDevTools/manifest.json`; `.gitignore` includes **`Decompiled/`** (alongside legacy typo dir); **removed** confusing duplicate `.github/workflows/deprecated/build-release-publish.yml`.

**2026-05-24**

- **Release & Publish:** Dropped `plan-release`/`fromJson` (plan job was skipped on [run #134](https://github.com/AzraelGodKing/SunhavenMod/actions/runs/27859289447)). Release uses a static 12-mod matrix + `pick` step; `build-gate` carries `ref` from `version`.
- **Hygiene:** Removed stray `HavenDevTools/manifest.json` again (canonical Thunderstore metadata is `HavenDevTools/thunderstore/manifest.json` only).
- **Localization:** Fixed `scripts/fix-localization-format-specifiers.ps1` — PowerShell `StartsWith([char]0xFEFF)` is always true, which stripped the root `{` and broke five `strings.json` files in CI; use explicit UTF-8 read without the bogus BOM strip.
- **Follow-up:** `DebugWindow` caches IMGUI toolbar/race arrays; localized Crop Optimizer hover tooltip and The Vault `DoorPatches` user-facing strings; `scripts/add-followup-localization-keys.ps1` helper for new keys.
- **Docs:** Rewrote root [`README.md`](README.md), [`CHANGELOG.md`](CHANGELOG.md), and this file — scoped maintainer vs player notes; [`docs/VERSION_AND_RELEASE.md`](docs/VERSION_AND_RELEASE.md) clarifies Python for version checks and hub matrix regen; [`docs/ATOMIC_SAVE_POLICY.md`](docs/ATOMIC_SAVE_POLICY.md) for save temp semantics.
- **README:** Per-mod `### Unreleased` headers converted to dated maintainer sections where applicable.

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
