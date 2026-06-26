# Changelog

Notes for **players and release readers**. Per-mod blurbs and upstream metadata are also in [`docs/versions.json`](docs/versions.json). Maintainer-only tooling and CI changes are in [`MAINTAINER_CHANGELOG.md`](MAINTAINER_CHANGELOG.md).

---

## Unreleased

**Localization (UI mods)**

- Shared layer: `SharedUtilities/ModLocalization.cs`, `LanguageChangeWatcher.cs`, `LocalizationBootstrap.cs`; `Directory.Build.targets` adds `I2Localization.dll` for UI projects.
- All nine UI mods ship `Localization/strings.json` (16 languages: en, da, de, es, fr, it, ja, ko, nl, pt, pt-BR, ru, sv, zh-CN, zh-TW, uk) as embedded resources; `ModLocalization.T()` in HUD/windows; live refresh when the game language changes (Haven's Respec refreshes TMP labels explicitly).
- PRs run `scripts/validate-localization.ps1`. Optional per-language overrides: `BepInEx/config/<ModFolder>/lang/<code>.json`.
- Machine translation via MyMemory: run one mod and one language per invocation to avoid rate limits, e.g. `scripts/fill-localization-languages.ps1 -Translate -ForceRetranslate -Mod HavensRespec -Language fr`. Full matrix: `scripts/translate-all-localization.ps1 -ForceRetranslate` (long-running; 30s pause between passes by default). Progress audit: `scripts/audit-untranslated.ps1`.
- **Localization fill:** Full MyMemory pass for all nine UI mods (`-ForceRetranslate`) to replace English placeholder copies with per-language strings; Senpai's Chest bullet glyphs normalized to UTF-8 `•`.
- **Haven's Almanac:** Dashboard provider sections (birthday, vault, todo, museum, chest, birthright, crop optimizer, dev tools, mod health) now use `ModLocalization` keys.
- **Crop Optimizer:** TMP HUD title and tooltip toggle labels refresh when the game language changes.
- **Crop Optimizer:** Fixed startup crash when The Vault is not installed (`TheVault.Abstractions` load failure); optional Vault integration now uses reflection like Todo/Birthday integrations.
- **Small-mod localization sweep:** Filled remaining English-copy gaps in Crop Optimizer, Senpai's Chest, The Vault, Sun Haven Todo, Haven's Respec, S.M.U.T., and Haven's Almanac; fixed Crop Optimizer Rich Text placeholders (`{0}`, `<color={n}>`) and Japanese `XPH`/`XRT` artifacts across those mods.
- **PR #72 review fixes:** Removed stray `HavenDevTools/manifest.json` and root translation scratch files; normalized broken `{0}` format tokens (Japanese `XPH`/`XRT`, French NBSP-in-specifier); split Birthday Reminder `birthday.gift.universal` into `universalLoved` / `universalLiked`.
- **Follow-up polish:** Haven Dev Tools debug window caches IMGUI toolbar/race label arrays (no per-frame allocations); Crop Optimizer hover tooltip and The Vault door patches use `ModLocalization` for remaining hardcoded English strings.
- **The Vault:** Fixed `IconCache` type ambiguity in `PlayerPatches` (build blocker for Almanac dependency chain).

**Sun Haven Todo**

- **HUD:** The sticky task panel header includes a close (**X**) control that hides the HUD until you press the HUD toggle hotkey again (your config’s **HUDToggleKey**, often **Ctrl + H**). This does not change the **HUD → Enabled** setting in the config file.
- **HUD:** While the full Todo window is open, a **Sticky** button appears in its header when the sticky panel is hidden so you can bring it back without closing the list. The **HUD toggle hotkey** also works while the big window is open (the main **ToggleKey** for the full list still only applies when that window is closed—use **Escape** to close it).

---

## 2026-06-26

**Bugfix release:** Every mod received a **patch** semver bump so store metadata, DLLs, and [`docs/versions.json`](docs/versions.json) stay aligned. Current versions: The Vault **4.0.1**, Haven's Birthright **3.0.1**, Senpai's Chest **3.0.1**, S.M.U.T. **3.0.1**, Sun Haven Todo **2.0.1**, A Squirrel's Birthday Reminder **2.0.1**, Haven's Almanac **2.0.1**, Haven Dev Tools **2.0.1**, Faster Races **2.0.1**, Trinket Fortune **2.0.1**, Crop Optimizer **2.0.1**, Haven's Respec **2.0.1**.

- **Live language refresh:** Fixed a startup error (`Parameter "languageName" not found`) that prevented the shared `LanguageChangeWatcher` from patching the game's `SetLanguageAndCode`. The Harmony postfix parameter names now match the original method's casing, so the language-change hook registers and mod UIs re-localize correctly when you switch the in-game language.
- **Crop Optimizer:** Fixed stale crop instance IDs surviving across character/save loads. The crop instance registry is now cleared on character load, so reused Unity instance IDs from a previous save can no longer collide with unrelated new crops and report wrong growth state.

---

## 2026-05-03

**Senpai's Chest**

- **Wildcard rules:** Wildcard/glob matching (`*` / `?`) now lives in **Manage Groups** by default (patterns stored with each group, then applied through **By Group** rules). Optional UI toggle `SeparateWildcardRuleInUI` exposes a standalone **By wildcard name** rule when explicitly enabled.

**Patch release:** Every mod in the repo received a **patch** semver bump so store metadata, DLLs, and [`docs/versions.json`](docs/versions.json) stay aligned after the cross-cutting review. Current versions: The Vault **3.3.2**, Haven's Birthright **2.2.2**, Senpai's Chest **2.6.2**, S.M.U.T. **2.4.2**, Sun Haven Todo **1.4.2**, A Squirrel's Birthday Reminder **1.4.2**, Haven's Almanac **1.4.2**, Haven Dev Tools **1.2.2**, Faster Races **1.4.2**, Trinket Fortune **1.2.2**, Crop Optimizer **1.4.2**, Haven's Respec **1.3.2**.

- **Maintainers:** Nexus “detailed description” BBCode sources were renamed per mod folder: [`docs/NexusMods-BBCode-Index.txt`](docs/NexusMods-BBCode-Index.txt) (`NexusMods-<ModFolder>-BBCode.txt`).

---

## 2026-05-02

Cross-cutting hardening and documentation (see also maintainer log).

**Shared code**

- **IconCache:** Per-entry texture ownership; eviction and clear only destroy textures the mod allocated — full-atlas game sprites are never destroyed. RenderTexture copy path always releases temp RTs.
- **VersionChecker:** Logger scoped to the check runner (no cross-mod static clobbering). Version compare ignores SemVer prerelease/`+build` tails for ordering.
- **GUIStyleHelper / MinimalJsonParser / VaultCryptography:** Safer gradient height edge case; UTF-16 surrogate pairs in JSON escapes; docs clarify tamper-resistant local storage vs confidentiality.

**The Vault**

- **Saves:** Filenames use a Steam or per-machine suffix to avoid collisions; legacy bare-name files migrate on load. Unreadable vault files are **quarantined** (`.corrupt-*.bak`) before starting an empty in-memory vault; see mod logs if you need to restore. **Load path:** invalid character names no longer leave a prior character’s vault state “loaded” for a new save; the plugin only marks the vault loaded when load succeeds. **Quarantine UX:** the HUD and vault window surface a one-time notice when a corrupt file was quarantined.
- **API:** `IsVaultManagerAvailable` / `IsVaultDataLoadedForCurrentSession`; `IsVaultReady` kept as a legacy alias for “manager exists.”
- **UI / patches:** Vault UI disposes generated IMGUI textures on rebuild and destroy; vault load trigger uses the same lock as other character-load paths; noisy hot-path logging reduced to Debug where appropriate; `PersistentUpdateRunner` lives in its own file; `VaultModApiBridge` subscription contract documented for soft dependencies.

**Haven's Birthright**

- If an essential Harmony patch fails to apply, the mod sets a fail-closed flag so stat/combat postfixes do not run half-patched.
- **Faster Races:** explicit Harmony priority on `Player.GetStat` (Birthright before Faster Races); coordination with movement speed no longer treats “Faster Races enabled at 0%” as an active speed bonus.

**Haven Dev Tools**

- Comments clarify Steam ID hashing is a client-side convenience gate, not a security boundary.

**Trinket Fortune**

- Nexus detailed-description BBCode refreshed ([`TrinketFortune/NexusMods-TrinketFortune-BBCode.txt`](TrinketFortune/NexusMods-TrinketFortune-BBCode.txt)): `BepInEx/config/TrinketFortune.cfg` (with legacy GUID migration), `MaxBonusChancePercent`, standalone-safe / optional S.M.U.T. and Haven Dev Tools, Thunderstore / Nexus / GitHub links.

**Repository**

- Reusable workflow setup; concurrency on release/test workflows. Stray `manifest.json` files under mod dirs are rejected by the version verifier. Policy files (license, security, contributing, code of conduct) and shared-code strategy doc. Almanac: no committed game decompiles — use local-only `_refs/` per `.gitignore`.
- **Docs:** Root [`README.md`](README.md), this changelog, and [`MAINTAINER_CHANGELOG.md`](MAINTAINER_CHANGELOG.md) were rewritten — concise hub layout, versions aligned with [`docs/versions.json`](docs/versions.json), long-running docs-site diary removed from the README in favor of links.
- **Mod matrix / versions:** `verify-version-consistency.py` also requires every `jsonKey` in `scripts/mod-matrix.json` to exist in `docs/versions.json`. [`docs/ATOMIC_SAVE_POLICY.md`](docs/ATOMIC_SAVE_POLICY.md) documents temp-file behavior for competing save systems.

---

## 2026-05-01

- Thunderstore manifests normalized (no UTF-8 BOM) for strict tooling.
- Thunderstore READMEs: removed stale “Unreleased” blocks from several mods.
- **The Vault:** clearer destroy-path logging for expected vs unexpected teardown.
- **Sunhaven Todo:** confirmed dirty-save flags on all task mutation paths after save guard work.

---

## 2026-04-29

- Release workflows: Nexus preflight and zip path resolution hardened; Almanac Nexus skip removed where listing exists.

---

## 2026-04-28

Lifecycle and teardown pass across multiple mods: duplicate handler prevention, menu-transition deduplication, Harmony unpatch on shutdown where applicable, debug-level diagnostics for reflection fallbacks. **Senpai's Chest:** smarter persistence and scan logging toggle. **The Vault / Todo / S.M.U.T. / Crop Optimizer / Trinket Fortune / Birthright / Faster Races / Respec / Birthday / Dev Tools / Almanac:** see git history for per-file detail.

---

## 2026-04-21

Performance and stability: UI texture disposal in hot paths, IconCache bounded eviction, fewer allocations in patches and helpers, IconCache eviction fallback fixes, ItemSearch race fix, vault validation and player-context locking, VersionChecker regex caching, silent catches logged at Debug in shared helpers.

---

## Earlier releases

Summaries for older shipped work (vault HUD, mod hub, Crop Optimizer, Haven's Respec, CI migration to self-hosted builds, docs site) live in git history and in each mod’s folder README / [`docs/versions.json`](docs/versions.json) changelog fields.
