# Changelog

Notes for **players and release readers**. Per-mod blurbs and upstream metadata are also in [`docs/versions.json`](docs/versions.json). Maintainer-only tooling and CI changes are in [`MAINTAINER_CHANGELOG.md`](MAINTAINER_CHANGELOG.md).

---

## 2026-05-02

Cross-cutting hardening and documentation (see also maintainer log).

**Shared code**

- **IconCache:** Per-entry texture ownership; eviction and clear only destroy textures the mod allocated — full-atlas game sprites are never destroyed. RenderTexture copy path always releases temp RTs.
- **VersionChecker:** Logger scoped to the check runner (no cross-mod static clobbering). Version compare ignores SemVer prerelease/`+build` tails for ordering.
- **GUIStyleHelper / MinimalJsonParser / VaultCryptography:** Safer gradient height edge case; UTF-16 surrogate pairs in JSON escapes; docs clarify tamper-resistant local storage vs confidentiality.

**The Vault**

- **Saves:** Filenames use a Steam or per-machine suffix to avoid collisions; legacy bare-name files migrate on load. Unreadable vault files are **quarantined** (`.corrupt-*.bak`) before starting an empty in-memory vault; see mod logs if you need to restore.
- **API:** `IsVaultManagerAvailable` / `IsVaultDataLoadedForCurrentSession`; `IsVaultReady` kept as a legacy alias for “manager exists.”
- **UI / patches:** Vault UI disposes generated IMGUI textures on rebuild and destroy; vault load trigger uses the same lock as other character-load paths; noisy hot-path logging reduced to Debug where appropriate; `PersistentUpdateRunner` lives in its own file.

**Haven's Birthright**

- If an essential Harmony patch fails to apply, the mod sets a fail-closed flag so stat/combat postfixes do not run half-patched.

**Haven Dev Tools**

- Comments clarify Steam ID hashing is a client-side convenience gate, not a security boundary.

**Trinket Fortune**

- Nexus detailed-description BBCode refreshed ([`TrinketFortune/NexusMods-BBCode.txt`](TrinketFortune/NexusMods-BBCode.txt)): `BepInEx/config/TrinketFortune.cfg` (with legacy GUID migration), `MaxBonusChancePercent`, standalone-safe / optional S.M.U.T. and Haven Dev Tools, Thunderstore / Nexus / GitHub links.

**Repository**

- Reusable workflow setup; concurrency on release/test workflows. Stray `manifest.json` files under mod dirs are rejected by the version verifier. Policy files (license, security, contributing, code of conduct) and shared-code strategy doc. Almanac: no committed game decompiles — use local-only `_refs/` per `.gitignore`.
- **Docs:** Root [`README.md`](README.md), this changelog, and [`MAINTAINER_CHANGELOG.md`](MAINTAINER_CHANGELOG.md) were rewritten — concise hub layout, versions aligned with [`docs/versions.json`](docs/versions.json), long-running docs-site diary removed from the README in favor of links.

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
