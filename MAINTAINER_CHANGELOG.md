# Maintainer changelog

Internal engineering log: **CI**, **release automation**, **scripts**, **docs infrastructure**, and **non–player-visible** observability. **Do not** duplicate full player-facing release notes here — those belong in [`CHANGELOG.md`](CHANGELOG.md), which is linked for cross-reference.

---

## 2026-07-02 (continued)

- **Haven Dev Tools — NPC relationships:** New **Relationships** Tools sub-tab (`NpcRelationshipEditor`) with hearts, date/platonic/marry/divorce, skip-to-cycle, set primary spouse, and reset-all-hearts. Dev console aliases: `relationship.set`, `relationship.marry`, `relationship.divorce`, `relationship.cycle`. Cheat commands: use **CheatEnabler** (removed in-mod `ForceEnableCheats` / Quantum Console patches).
- **Haven Dev Tools — Marriable tab:** Multi-select romanceable NPCs and marry via `MarryNpcPolygamy` (no divorce-first). Gated on `UltraPolygamyHelper`: BepInEx GUID `vurawnica.sunhaven.polygamy` plus Harmony prefix on `NPCAI.MarryPlayer`. Status line in F11 header (`Poly: Yes/No`).
- **Haven Dev Tools — resizable window:** `DebugWindowLayout` + corner grip; `[DebugWindow] Width` / `Height` config persistence; header bar, mod status on two rows, flexible scroll/list heights in tabs.
- **Haven Dev Tools — input:** `[DebugWindow] PauseGameWhenDebugOpen` default **false**. Built-in console tab: auto-focus + Enter/history keys.
- **Senpai's Chest — CheatEnabler compat:** Removed `PlayerInput.DisableInput` / `AddPauseObject` on config open (`SmartChestUI.BlockGameInput`); was blocking `QuantumConsole.CanOpen`. `ShouldBlockGameInput` yields when QC or uGUI text fields are active; `TextInputFocusGuard` exposes `IsQuantumConsoleActive` / `IsUnityUiTextInputFocused`.
- **Community mod — Ultra Polygamy:** 3.1 port of `vurawnica.sunhaven.polygamy` v0.0.5 under `CommunityMods/UltraPolygamy/` (original behavior; Harmony signature + decompiler fixes only). **Gitignored** and **excluded** from `mod-matrix.json` / release CI. See [`CommunityMods/README.md`](CommunityMods/README.md).

---

## 2026-06-29

- **Mod review fixes (save + compat):** The Vault `VaultSaveSystem.Load()` tries `.backup` when primary/legacy saves are missing or unreadable; SMUT `DonationSaveSystem.Load()` matches SenpaisChest/Todo backup-first pattern; HavensBirthright shop discount restores listing price in `BuyItem` postfixes; HavenDevTools version checker uses `Chainloader.PluginInfos` instead of compiled-in version tuples. See [`mod-review-2026-07-02.md`](mod-review-2026-07-02.md).
- **Vault load (PR review):** Candidate order canonical → backup → legacy; quarantine corrupt primary when a later candidate loads; reset `_needsReEncryption` per candidate try.
- **Haven's Respec:** **RESET ALL** moved from the top of the skill grid to the left sidebar under **Reset** (`RespecButtonInjector` row layout); `UI.EnableResetAll` restored (default on).
- **Cloudflare mirror:** [`sync-cloudflare-site-mirror.yml`](.github/workflows/sync-cloudflare-site-mirror.yml) authenticates git clone/push with host-scoped `Authorization: Basic` (`x-access-token`) instead of `Bearer`, which GitHub’s git HTTPS rejects and caused `could not read Username for 'https://github.com'` in CI.
- **Version verifier:** [`scripts/version/verify-version-consistency.py`](scripts/version/verify-version-consistency.py) uses [`scripts/lib/Resolve-Python.ps1`](scripts/lib/Resolve-Python.ps1) in the PowerShell wrapper so local dev skips broken `py` launcher entries (e.g. stale self-hosted Actions tool cache) while CI still picks up `setup-python` / runner `python3`.
- **pre-push-build:** `-Mod` accepts mod matrix keys or project folder names/paths (e.g. `.\HavensRespec\` → `havensrespec`).

---

## 2026-06-28

- **Release workflows:** Added `giftingassistant` to the mod `options` list in [`build-release-publish.yml`](.github/workflows/build-release-publish.yml) and [`release-self-hosted-sunhaven-runner.yml`](.github/workflows/release-self-hosted-sunhaven-runner.yml) so single-mod dispatches can target Gifting Assistant (matrix / `mod=all` already included it via `scripts/matrix/mod-matrix.json`).

---

## 2026-05-03

- **`release-self-hosted-sunhaven-runner.yml`:** **`package_step.outputs.zip_path`** is the single source of truth for dry-run artifact upload, GitHub Release assets, Thunderstore `file`, and Nexus preflight fallback; **`test_discord`** packaging now emits `zip_path`; **preflight-success** guards all Nexus upload/retry/backoff steps; **`ignore_discord_notify`** workflow input matches **Release & Publish**; lightweight **Package diagnostics** step before dry-run completion / publishes.
- **`build-release-publish.yml`:** Comments clarify that `builds/` paths are **ephemeral on the runner** (stage → upload-artifact → download → package), not a committed tree — same idea as [`builds/README.md`](builds/README.md).
- **Nexus BBCode files:** Renamed from `NexusMods-BBCode.txt` to **`NexusMods-<ModFolder>-BBCode.txt`** in each project; [`scripts/version/pre-push-build.ps1`](scripts/version/pre-push-build.ps1) and [`scripts/version/stage-version-sync-files.py`](scripts/version/stage-version-sync-files.py) updated; hub list in [`docs/NexusMods-BBCode-Index.txt`](docs/NexusMods-BBCode-Index.txt).
- **Senpai's Chest:** Glob rules on item display names (`*` / `?`), `RuleType.ByNamePattern` + JSON `NamePattern`; group data now stores `NamePatterns` and `By Group` matches item IDs or wildcard patterns. New UI config: `SeparateWildcardRuleInUI` (default false; wildcard editing in Manage Groups unless explicitly separated).
- **Version bump:** `.\scripts\pre-push-build.ps1 -All -Bump patch` — patch bump for all twelve mods (`docs/versions.json`, manifests, Nexus BBCode headers, doc badges, `HavenDevTools` `DebugWindow` tuples, rebuild).

---

## 2026-05-02

- **Workflows:** Added [`reusable-mod-matrix-setup.yml`](.github/workflows/reusable-mod-matrix-setup.yml); **concurrency** groups on **Release & Publish** and **Test — Self-hosted** to reduce racing publishes.
- **Version verifier:** [`scripts/version/verify-version-consistency.py`](scripts/version/verify-version-consistency.py) scans for stray `manifest.json` under each mod directory; **matrix keys** in `scripts/matrix/mod-matrix.json` must exist in `docs/versions.json`; PowerShell script delegates to Python.
- **Mod matrix:** [`scripts/matrix/mod-matrix.json`](scripts/matrix/mod-matrix.json) includes `statsDomId`; [`sync-docs-mod-matrix.js`](scripts/matrix/sync-docs-mod-matrix.js) regenerates [`docs/mod-matrix.json`](docs/mod-matrix.json); [`fetch-stats.js`](scripts/stats/fetch-stats.js) resolves stats IDs from the matrix; reusable matrix setup runs the generator and **`git diff --exit-code`** on `docs/mod-matrix.json`.
- **Sync workflow:** [`sync-mod-versions.yml`](.github/workflows/sync-mod-versions.yml) stages allowlisted paths via [`scripts/version/stage-version-sync-files.py`](scripts/version/stage-version-sync-files.py) (no broad `git add -A` / full-repo `-u`).
- **Release / mirror:** Self-hosted test workflow: Nexus **fail-closed** when publish requested but no upload succeeded; Nexus `upload-action` **v1.0.0-beta.4** aligned with main release workflow; [`sync-cloudflare-site-mirror.yml`](.github/workflows/sync-cloudflare-site-mirror.yml) uses `http.extraHeader` auth instead of token-in-URL; [`update-stats.yml`](.github/workflows/update-stats.yml) falls back to `github.token` when `ADMIN_PUSH_TOKEN` is unset.
- **Build:** [`Directory.Build.props`](Directory.Build.props) — `Nullable` annotations, `TreatWarningsAsErrors` when `GITHUB_ACTIONS` is set; `LangVersion` latest, `Deterministic`, `ContinuousIntegrationBuild`. **Senpai's Chest** suppresses **CS0436** (linked `SceneRootSurvivor` + `SunHavenMuseumUtilityTracker` reference embeds the same type).
- **Policy / governance:** Root [`LICENSE`](LICENSE), [`CONTRIBUTING.md`](CONTRIBUTING.md), [`SECURITY.md`](SECURITY.md), [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md), [`docs/SHARED_CODE_STRATEGY.md`](docs/SHARED_CODE_STRATEGY.md).
- **Hygiene:** Removed duplicate root `HavenDevTools/manifest.json`; `.gitignore` includes **`Decompiled/`** (alongside legacy typo dir); **removed** confusing duplicate `.github/workflows/deprecated/build-release-publish.yml`.

**2026-05-24**

- **Release & Publish (Nexus zip missing after Thunderstore):** With the release jobs finally running, mods failed at **Nexus — resolve and verify zip** with `dist/ directory does not exist` ([run #142](https://github.com/AzraelGodKing/SunhavenMod/actions/runs/27860763313)). Cause: `GreenTF/upload-thunderstore-package@v4.3` is a Docker container action that resets the workspace and wipes the uncommitted `dist/` tree, so the later Nexus step finds no zip. Fix in [`reusable-release-mod.yml`](.github/workflows/reusable-release-mod.yml): `package_step` now stages a copy of the zip in `$RUNNER_TEMP/release-pkg` (outside the workspace), and the Nexus preflight restores `dist/<zip>` from that stage if the workspace was reset.
- **Release & Publish (release stage rewrite):** Root cause of the long-standing "release skipped / 0 steps" — in this repo **any job that transitively depends on the matrix `build` job is auto-skipped unless its `if` begins with `always()`**. Proof from [run #139](https://github.com/AzraelGodKing/SunhavenMod/actions/runs/27860386455): all 12 `release` jobs `skipped` (0 steps) while `Build complete` and the `always()` `Combined release report` ran. Removed `build-gate` and the twelve explicit per-mod caller jobs; replaced with a **single matrix `release` job** (`if: always() && … && needs.build.result == 'success'`, `strategy.matrix.mod: fromJson(needs.setup.outputs.matrix)`) calling [`reusable-release-mod.yml`](.github/workflows/reusable-release-mod.yml). Same matrix the `build` job already fans out on, so `mod=all` → all mods and single-mod → one. `release_aggregate_report` now `needs: [release]`. Removed obsolete `scripts/generate-reusable-release-workflow.py`.
- **Release & Publish:** (superseded) Replaced matrix `release` job (skipped with 0 steps on `mod=all` after matrix `build` — [run #137](https://github.com/AzraelGodKing/SunhavenMod/actions/runs/27860163768)) with twelve explicit jobs calling the reusable workflow; granted `contents: write` per caller after nested `release-mod` was rejected at startup ([run #138](https://github.com/AzraelGodKing/SunhavenMod/actions/runs/27860332770)).
- **Release & Publish:** Fixed release skipped on dispatch: boolean `build_only == false` never matched (GitHub passes `'false'` string); use `!= 'true'` for version/release gates ([run #136](https://github.com/AzraelGodKing/SunhavenMod/actions/runs/27859681812)).
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
