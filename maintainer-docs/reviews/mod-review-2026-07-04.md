# Mod review — 2026-07-04

Follow-up to `mod-review-2026-07-02.md` (which covered six new-mod ideas). This pass covers: codebase improvement recommendations, per-mod feature suggestions, and additional new-mod ideas that do not repeat the 07-02 list. Findings are grounded in spot-reads of SharedUtilities, save systems, integrations, and UI layers plus repo-wide greps — not an exhaustive line-by-line audit of all ~230 source files.

## 1. Codebase improvements

### 1.1 Consolidate the per-character save-system pattern (highest leverage)
Eight files reimplement the same sanitize-name → write-temp → rotate-.bak → atomic-move → fallback-load pattern: `SunhavenTodo/Data/TodoSaveSystem.cs`, `SenpaisChest/Data/SmartChestSaveSystem.cs`, `GiftingAssistant/Data/GiftRosterSaveSystem.cs`, `SunHavenMuseumUtilityTracker/Data/DonationSaveSystem.cs`, `BirthdayReminder/Data/FavoriteGiftStore.cs`, `TheVault/Vault/VaultSaveSystem.cs`, plus `SanitizeFileName` copies in two `PlayerPatches.cs`. Extract a generic `CharacterSaveStore` into `SharedUtilities/` (serialize delegate in, path convention + atomicity + backup + corruption fallback handled once). Every future save-policy fix (e.g. fsync, versioned backups) then lands in one place instead of eight.

### 1.2 De-duplicate the Todo soft-integration client
Four diverged copies exist: `BirthdayReminder/Integration/TodoIntegration.cs` (223 lines), `CropOptimizer/Integration/TodoIntegration.cs` (61), `GiftingAssistant/Integration/TodoIntegration.cs` (705), `SenpaisChest/Integration/MuseumTodoIntegration.cs`. TheVault already solved this problem properly with `TheVault.Abstractions` + `VaultModApiBridge`. Do the same for Todo: a `SunhavenTodo.Abstractions` assembly (or a shared reflection client in SharedUtilities) exposing `AddTodo / CompleteTodo / TodoExists`, so consumers stop re-implementing reflection against Todo internals.

### 1.3 Finish the IconCache and UiStyle consolidation
`SharedUtilities/IconCache.cs` (605 lines) and `SunHavenMuseumUtilityTracker/UI/IconCache.cs` (497 lines) coexist; SMUT should migrate to the shared one. Likewise `CropOptimizer/UI/UiStyle.cs` (412) and `GiftingAssistant/UI/UiStyle.cs` (317) are diverged siblings — promote a superset into `SharedUtilities` (the repo already links shared source into mods, so no new assembly needed).

### 1.4 Vault crypto: ship the "future hardening" you already documented
`SharedUtilities/VaultCryptography.cs:12-14` honestly documents the weaknesses: fixed IV (`CurrencySpellIV1`), constant salt, PBKDF2 with 10k iterations. Since the file is tamper-resistance rather than confidentiality, this is acceptable — but a `CSVAULT3` header format with a random per-save IV + salt stored in the header, and an HMAC (encrypt-then-MAC; AES-GCM isn't available on net48) would close the gap cheaply. Keep the existing multi-key legacy decrypt chain for migration; write new saves only in V3.

### 1.5 Reflection probe hygiene
34 empty/silent catch blocks across 10 files, concentrated in `CropOptimizer/UI/CropTileReflection.cs` (14) and `CropOptimizer/UI/CropHoverQuery.cs` (5). Individually reasonable (probing game internals that may not exist), but a shared `ReflectionProbe.Try(name, action)` helper that logs the first failure per call-site would turn silent breakage after a game update into a single diagnostic line — which matters for a mod suite whose main failure mode is "game updated, reflection target moved."

### 1.6 Per-frame logging in fallback paths
`HavensBirthright/BirthrightRunner.cs:435` logs `LogInfo` with a `FindObjectsOfType` result inside the Tidal Blessing fallback update path. If the fallback engages, this both scans the scene and writes a log line repeatedly. Throttle the log (once per N seconds) and cache the scan; same review lens applies to the other `FindObjectsOfType` fallback sites (15 occurrences across 9 files — most are one-shot or guarded, which is fine).

### 1.7 IMGUI style allocation audit
~1,300 `GUILayout`/`new GUIStyle`/`new GUIContent` uses across 16 UI files, heaviest in `SenpaisChest/UI/SmartChestUI.cs` (228) and `GiftingAssistant/UI/GiftingWindow.cs` (155). The `UiStyle` caches suggest most styles are pre-built, but a quick pass confirming no `new GUIStyle(...)` executes inside `OnGUI` per frame would rule out GC churn in the two biggest windows. A shared IMGUI widget helper in SharedUtilities (folds into 1.3) would also shrink these files.

### 1.8 Grow the test surface
Only three mods have test projects (SenpaisChest, SunhavenTodo, TheVault — 5 test files total), yet several pure-logic components are ideal NUnit targets with zero Unity dependency: `GiftingAssistant/Game/GiftSuggestionResolver.cs`, `HavensBirthright/BonusTransferRules.cs`, `HavensRespec/Services/CostService.cs`, `SharedUtilities/MinimalJsonParser.cs`, `SharedUtilities/RelationshipHeartRules.cs`. The existing `VaultDataMigrationTests` pattern shows the payoff — migration/serialization bugs are exactly what users report as "my save vanished."

### 1.9 Three JSON layers
`SharedUtilities/MinimalJsonParser.cs`, `CommunityMods/UltraPolygamy/TinyJson/`, and per-mod `*Json.cs` serializers all hand-roll JSON. Standardizing on the SharedUtilities parser (and a small shared writer) reduces the surface where an escaping bug can corrupt a save. Keep per-mod DTOs; share the tokenizer/writer.

### 1.10 Repo hygiene (small)
- `Tests/bin` + `obj` artifacts show up in the working tree (e.g. `TheVault/obj/Release/net48/CurrencySpell.dll`); verify `.gitignore` covers all of them so clones stay lean.
- Root-level review docs (`mod-review-*.md`, `pt-BR-translation-review.md`, `repo-management-review-2026-07-02.md`) would sit better under `maintainer-docs/reviews/`.
- `docs/styles.css` (765 lines) appears unreferenced by `index.html` — confirm what still links it (404.html?) and delete or fold in if dead.

## 2. Feature suggestions for existing mods

- **The Vault** — (a) CSVAULT3 hardened save format (see 1.4); (b) a "ledger" tab showing the last N deposits/withdrawals with day-stamps, reusing the existing per-character save; (c) optional shared "family vault" across characters on the same Steam account (the key derivation already supports a Steam-scoped identity).
- **Senpai's Chest** — (a) "craft-pull": when a crafting station is open, pull missing ingredients from labeled Smart Chests; (b) rule presets import/export as JSON snippets so players can share sorting setups; (c) a chest-network overview window (which chest owns which group) reusing `ChestLabel` data.
- **Sun Haven Todo** — (a) recurring tasks with day/season resets (daily gift run, weekly museum sweep); (b) game-calendar due dates ("by Summer 12") with overdue highlighting; (c) a quick-add hotkey that captures a todo without opening the full window.
- **S.M.U.T.** — (a) sell-guard: warn (or block, configurable) when shipping/selling an item still needed by the museum — highest-value single feature in the suite for its audience; (b) "where to find" hints per missing item (season/weather/location strings are static data); (c) per-hall completion ETA in the Almanac panel.
- **Birthday Reminder** — (a) season-at-a-glance calendar grid view; (b) a "gift given" history log per NPC (dovetails with the existing `FavoriteGiftStore`).
- **Gifting Assistant** — (a) auto-roster builder: "keep everyone below X hearts on the roster" instead of manual picks; (b) "buyable today" badge when a loved/liked gift is in a currently-open shop's stock.
- **Crop Optimizer** — (a) season-end warning: flag crops that cannot mature before the season change (data already in `CropForecast`); (b) sprinkler/water coverage overlay using the existing tile highlight renderer; (c) sort HUD by profit-per-day.
- **Haven's Birthright** — (a) in-game ability loadout panel (which racial actives are bound where); (b) per-character config profiles instead of a single global config, using the `CharacterFingerprint` machinery that already exists.
- **Haven's Respec** — respec history log (when, which tree, what cost) on top of the existing `ResetSnapshot`; the 07-02 review already covers the dry-run planner.
- **Haven's Almanac** — (a) week-ahead planner view merging birthdays + crop ETAs + todos; (b) "copy briefing to clipboard" for streamers; (c) panel reorder/hide via config.
- **HavenDevTools** — (a) Harmony patch inspector: list every patch on every method grouped by owning mod (invaluable for conflict triage); (b) save-file viewer for the suite's own JSON/vault files with the decrypt path already in SharedUtilities; (c) reflection-health panel that surfaces failed probes from 1.5.
- **Trinket Fortune** — visible pity/progress indicator ("aquarium 62% → trinket bias +38%") in its DevTools panel and as an Almanac provider.
- **Faster Races** — optional separate multipliers for mount/boat if the game exposes them; otherwise leave it small and stable.

## 3. New mod ideas (beyond the 2026-07-02 list)

1. **Haven's Ledger — income & expense tracker.** Patch the currency-change path (HavenDevTools' `CurrencyTracker.cs` already watches it) to record daily gold in/out by source (shop sales, shipping, quests, purchases). Daily/weekly summary window + Almanac provider ("yesterday you earned 3,420g, 61% from crops"). Read-only economically, per-character save via the shared store from 1.1. Medium complexity, low risk.
2. **NPC Compass / "Where's Everybody?"** — a window listing NPCs with their current map/zone (reflection over the NPC manager's active agents), sorted by "has gift pending" via Gifting Assistant/Birthday integration. Turns the daily gift run from wandering into a route. Medium reflection risk (schedule internals), high daily value.
3. **Season Prep Assistant** — N days before a season change, generate a checklist (harvest maturing crops, buy next-season seeds, unfinished festival items) pushed as Todos through the shared Todo API (1.2). Mostly composition of existing integrations; low risk, very visible payoff.
4. **Mine Companion** — per-visit mine tracker: floors reached, ore/gems per floor, and a "resources still needed for museum" banner via SMUT on entering a floor. New save file (shared store), one or two Harmony patches on floor transition. Medium complexity.
5. **Ranch Roster** — barn/coop animal dashboard: happiness, produce-ready flags, names/ages; Almanac provider + optional "produce ready" morning todo. Similar patch surface to CropOptimizer but for animals; reuses `CropHudView` layout patterns.
6. **Suite Config Hub** — one in-game window (IMGUI, shared UiStyle) that surfaces every AzraelGodKing mod's key toggles by reading BepInEx `ConfigFile` entries, with per-character profile switching. Zero game-API risk — it only touches BepInEx config plumbing — and makes the 13-mod suite feel like one product.

## 4. Docs site redesign (implemented 2026-07-04)

Shipped in this pass, not just recommended — see `docs/index.html` + `docs/index-style.css` rewrite:
- New "harvest morning / lantern night" design system layered over `shared-styles.css` tokens so the injected chrome (site search, theme toggle, back-to-top, bug button) inherits the palette automatically; mod pages untouched and verified rendering.
- Real mod `icon.png`s on all 13 cards (copied `GiftingAssistant`, `HavensAlmanac`, `HavensRespec` icons from `thunderstore/` into `docs/`).
- Sticky topbar nav, hero with live download stats, added the missing **Skills** filter (Haven's Respec was untargetable before), preserved all `shared.js`/`stats-display.js` hooks and legacy section anchors; `search-index.json` titles updated.
