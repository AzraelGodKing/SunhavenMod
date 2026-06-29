# Changelog

Notes for **players and release readers**. Per-mod blurbs and upstream metadata are also in [`docs/versions.json`](docs/versions.json). Maintainer-only tooling and CI changes are in [`MAINTAINER_CHANGELOG.md`](MAINTAINER_CHANGELOG.md).

---

## 2026-06-28

**Current versions** ([`docs/versions.json`](docs/versions.json)): The Vault **4.0.1**, Haven's Birthright / Senpai's Chest / S.M.U.T. **3.0.1**, Sun Haven Todo / Birthday Reminder / Haven's Almanac / Haven Dev Tools / Faster Races / Trinket Fortune / Crop Optimizer / Haven's Respec **2.0.1**, Gifting Assistant **1.0.0**.

**Haven's Respec**

- **Fix:** Reset buttons no longer disappear after main menu → game transitions. Harmony patches and controller hooks now survive plugin `OnDestroy` during scene loads (matching The Vault / Sun Haven Todo). Added a `PersistentRunner` for hotkeys and `EnsureModReady()` on scene load to re-wire hooks if the BepInEx plugin MonoBehaviour is recreated.

**Docs (GitHub Pages)**

- **Fix blank white pages:** Restored mod and index HTML after a batch-edit corruption emptied several files. Removed the `mod-subpage.css` white-wash layer that stacked opaque white cards over per-mod themes. Gifting Assistant docs page restored.
- **Gifting Assistant hub listing:** Re-added Gifting Assistant to the notice board (13 contracts), Diplomat's Kit, Guild Master's Arsenal, FAQ synergy bullets, and Arsenal Comparison column. Announcement banner points to the custom gift-wrap docs page. Cross-links on Todo, Birthday Reminder, and Haven's Almanac mod pages.
- **Almanac integration docs:** Rewrote &ldquo;Integration with Haven&rsquo;s Almanac&rdquo; sections on every mod page (HUD / dashboard / briefing surfaces per data provider). Added Gifting Assistant to the Almanac supported-mod list; noted Respec alongside Faster Races and Trinket Fortune as not yet integrated.
- **versions.json:** Added **Gifting Assistant** **1.0.0** release metadata (`com.azraelgodking.giftingassistant`); Nexus [`/mods/507`](https://www.nexusmods.com/sunhaven/mods/507) (`nexus_file_group_id` **7597922**) and Thunderstore [`GiftingAssistant`](https://thunderstore.io/c/sun-haven/p/AzraelGodKing/GiftingAssistant/) URLs.
- **Release workflows:** Added `giftingassistant` to the **Release & Publish** and **Test — Self-hosted Sunhaven runner** mod picker (single-mod releases).
- **Mod README versions:** Every mod `README.md` now states the published version from `docs/versions.json` (fixed stale **The Vault** `3.3.2` and **S.M.U.T.** `2.2.7` headers). Added root `README.md` for Haven Dev Tools and Trinket Fortune.

**Gifting Assistant**

- **PR #74 review fixes:** ReminderMode legacy migration (`UseTodoIntegration` false → RosterOnly); autosave uses `Saving > AutoSaveInterval` from config (60s default); sort guard uses try/finally; shared `GameSaveCharacterName` helper; Almanac dashboard drops unreachable gifted branch; `GiftSuggestionResolver` formatting; pt-BR **Normal** priority translation.
- **Gifted toggle:** The row **Gifted** checkbox no longer adds or updates Sun Haven Todo tasks — only **+Todo** pushes reminders. In-game gifts still auto-complete matching todos when you give the gift in the world.
- **Hotkey toggle:** **Ctrl + G** (or configured toggle) now closes the gifting window when it is already open; previously the hotkey only opened it and Escape was required to dismiss. Added full `Localization/strings.json` coverage for all 16 supported game languages (da, de, es, fr, it, ja, ko, nl, pt, pt-BR, ru, sv, zh-CN, zh-TW, uk). Shared terminology aligned with Sun Haven Todo (Cancel, priority labels), Birthday Reminder (Gifts), and repo-wide UI strings where keys overlap.
- **Gift icon display split:** Roster rows show **all** selected preferred gift icons (capped at four with a +N overflow). Sun Haven Todo tasks pick **one random** icon from preferred gifts (or random loved/liked when none are set); the pick refreshes on **+Todo**, a new in-game day, or when the window reopens. Todo descriptions still list all suggested gift names.
- **+Todo replace:** Clicking **+Todo** on a roster row now removes any existing Gifting Assistant gift task for that NPC (active or completed) and adds a fresh one with current preferred gifts and icon — no need to delete the old todo manually first.
- **Performance:** Cached NPC reflection (`TryGetValue`, name accessors) at init; gifted-today flags now refresh on window open, day change, and in-game gifts instead of per OnGUI frame — roster stats, rows, and sort use the cache.
- **Performance (click freeze fix):** Major UI/Todo overhaul so button clicks no longer stall the game. **+Todo**, gifted toggle, roster add/remove, and **Gifts** now only flip flags or enqueue work; heavy paths (full `NPCManager` scan, todo reflection, `GetAllTodos` scan, disk save, gift-name resolution) run on the next `Update` tick via batched `TodoIntegration.ProcessPending` and deferred `GiftGameData` cache warm. Todo bridge caches context + todo IDs (Birthday Reminder pattern) for O(1) complete/remove instead of scanning every task per click. Gift selector and NPC picker build lists off the IMGUI thread and draw rows/icons incrementally with cached item names.
- **Todo integration fix (scene transitions):** Fixed gift todos never being pushed/completed with repeated `Todo integration: plugin instance not found` warnings while Sun Haven Todo was installed and loaded. Sun Haven Todo's BepInEx plugin MonoBehaviour is destroyed on scene transitions, so `PluginInfo.Instance` compared equal to null via Unity's overloaded operator and the integration bailed even though Sun Haven Todo's static `TodoManager` was still alive. The bridge now resolves the `SunhavenTodo.Plugin` type directly (instance-independent) and calls the static `GetTodoManager()`/`SaveData()` without the live instance — mirroring A Squirrel's Birthday Reminder's robust integration. The live instance is only used for the rare `LoadDataForCharacter` fallback, which is skipped when the manager already reports data loaded. Failure warnings are now logged once per cause instead of on every attempt.
- **Todo integration on by default:** `Integrations > ReminderMode` now defaults to **PushToTodo** (was RosterOnly) so a fresh install shows the per-row **+Todo** button and pushes gift tasks automatically when Sun Haven Todo is installed; it still falls back to roster-only when Sun Haven Todo is not installed. Legacy `UseTodoIntegration` migration is unchanged.
- **Todo button fix:** Fixed the per-row **+Todo** button not appearing when the integration was active. The button is gated on `CanPushTodos` (enabled + Sun Haven Todo loaded); with the opt-in default previously off it never showed on fresh installs. Also hardened the Sun Haven Todo availability check against soft-dependency load order (re-checks while not yet loaded, caches once detected) so the button reliably appears without re-probing every frame.
- **Todo auto-complete sync:** Gifting an NPC in-game now marks the matching Sun Haven Todo task complete (and un-completes if the game flag is cleared). The window **Gifted** toggle updates the roster only — it does not add or complete todos.
- **Todo daily refresh:** When `Integrations > ReminderMode = PushToTodo`, every new in-game day removes the previous gift tasks and re-adds a fresh one per rostered NPC (guarded so it runs once per day). Tasks are also ensured on character load so they appear without waiting for a day rollover.
- **Todo gift icons:** Gift tasks now carry the NPC's primary suggested gift as the task icon (`TodoItem.IconItemId`, via the established cross-mod contract) and list the preferred gift name(s) in the task description.
- **Per-NPC preferred gifts:** Each roster row has a **Gifts** button that opens a selector of the NPC's loved/liked gifts with checkboxes; selected gifts are persisted per character. When set, the row and the Todo task show only the chosen gifts (instead of the full loved/liked lists) to cut clutter, falling back to loved-then-liked when nothing is selected.
- **Gift selector UX:** Removed the pinned **Selected (N)** summary strip from the preferred-gift picker; selection is shown only by row checkmarks (and a subtle gold row highlight). Fixed garbled row layout where localization keys could leak beside item names — checkmarks now render inside a fixed-width checkbox column so icon and name no longer overlap.
- **Almanac integration:** When Haven's Almanac is installed and `Integrations > UseAlmanacIntegration` is enabled (default on), shares gift roster progress with the Almanac HUD (`pending / roster` summary), dashboard (remaining NPCs with priority), and daily briefing (pending count plus high/urgent breakdown).
- **Config:** `Integrations > UseAlmanacIntegration` now defaults to **true** (was false); set to false in `GiftingAssistant.cfg` to keep roster data private.
- **Todo integration:** Fixed **+Todo** not creating entries in Sun Haven Todo — ensures Sun Haven Todo character data is loaded before add, uses the string `AddTodo(title, description, priority, category)` overload (same reflection path as Crop Optimizer), verifies the todo appears in the active list, and logs warnings when add/save fails instead of reporting success on a silent no-op.
- **Todo integration:** **+Todo** is hidden unless Sun Haven Todo is installed *and* `Integrations > UseTodoIntegration` is enabled (default off — own gift roster/priority when off).
- **Tracking mode UX:** Replaced the opaque boolean `UseTodoIntegration` with `Integrations > ReminderMode` (**RosterOnly** / **PushToTodo**). The gifting window shows an active tracking line ("Daily gift roster" vs "Daily roster + Sun Haven Todo"), stats read "On roster" instead of "Tracked", and PushToTodo without Sun Haven Todo installed shows a clear fallback hint. Legacy `UseTodoIntegration = true` migrates automatically.
- **NPC picker:** Fixed duplicate composite names (e.g. `Anne+Anne`) in the add-NPC list; names are normalized the same way as Birthday Reminder, and gift-status lookup resolves roster names against game dictionary keys.
- **Relationship hearts:** Roster rows show each NPC's friendship level as a heart bar (from `CharacterData.Relationships`, 5 points per heart — same tiers as the in-game Relationship HUD). Values refresh when the window opens, not every frame; unknown NPCs show `?`. The Add NPC picker shows the same heart bar beside each candidate name (cache refreshes when the picker opens).

**The Vault**

- **Fix:** Vault UI and HUD icons display again — removed duplicate `TheVault/UI/IconCache.cs`; C# namespace shadowing had `VaultUI`/`VaultHUD` read the legacy cache while `Plugin`/`PlayerPatches` loaded and registered the shared `SunhavenMods.Shared.IconCache`.
- **Performance:** Player icon load now routes through shared `SunhavenMods.Shared.IconCache` only (removed duplicate legacy load path). Vault HUD rebuilds row layout only when currency totals or HUD scale/density change (integer hash fingerprint instead of per-refresh string join). Character-sync fallback throttled to ~0.75s; repeated `CurrentCharacter` fallback warning logs once per session.

**Senpai's Chest**

- **Performance:** Routine `[Scan]` log lines (start, per-chest scan, no-op complete) downgraded to LogDebug; item moves and errors remain LogInfo/LogError.

**Localization (UI mods)**

- Shared layer: `SharedUtilities/ModLocalization.cs`, `LanguageChangeWatcher.cs`, `LocalizationBootstrap.cs`; `Directory.Build.targets` adds `I2Localization.dll` for UI projects.
- **`[Localization] ForceEnglish`** (default `false`) on every localized mod — keep mod UI in English and ignore Sun Haven's in-game language. Toggle live in BepInEx Configuration Manager.
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
- **HUD localization fix:** Fixed Crop Optimizer and Sun Haven Todo HUDs showing raw localization keys (e.g. `crop.hud.title`, `todo.hud.title`) after plugin teardown. `ModLocalization.Shutdown()` no longer clears loaded string tables when persistent UI survives, and `LocalizationBootstrap` uses the mod assembly reliably when loading embedded `strings.json`.
- **The Vault:** Fixed `IconCache` type ambiguity in `PlayerPatches` (build blocker for Almanac dependency chain).

**Haven's Almanac**

- **Gifting Assistant integration:** When Gifting Assistant is installed and its `Integrations > UseAlmanacIntegration` is enabled, the Almanac HUD, dashboard, and daily briefing show gift roster progress (pending count, priority breakdown, remaining NPC list).

**Sun Haven Todo**

- **HUD:** The sticky task panel header includes a close (**X**) control that hides the HUD until you press the HUD toggle hotkey again (your config’s **HUDToggleKey**, often **Ctrl + H**). This does not change the **HUD → Enabled** setting in the config file.
- **HUD:** While the full Todo window is open, a **Sticky** button appears in its header when the sticky panel is hidden so you can bring it back without closing the list. The **HUD toggle hotkey** also works while the big window is open (the main **ToggleKey** for the full list still only applies when that window is closed—use **Escape** to close it).

---

## 2026-06-26

**Bugfix release:** Every mod received a **patch** semver bump so store metadata, DLLs, and [`docs/versions.json`](docs/versions.json) stay aligned. Current versions: The Vault **4.0.1**, Haven's Birthright **3.0.1**, Senpai's Chest **3.0.1**, S.M.U.T. **3.0.1**, Sun Haven Todo **2.0.1**, A Squirrel's Birthday Reminder **2.0.1**, Haven's Almanac **2.0.1**, Haven Dev Tools **2.0.1**, Faster Races **2.0.1**, Trinket Fortune **2.0.1**, Crop Optimizer **2.0.1**, Haven's Respec **2.0.1**.

- **Live language refresh:** Fixed a startup error (`Parameter "languageName" not found`) that prevented the shared `LanguageChangeWatcher` from patching the game's `SetLanguageAndCode`. The Harmony postfix parameter names now match the original method's casing, so the language-change hook registers and mod UIs re-localize correctly when you switch the in-game language.
- **Crop Optimizer:** Fixed stale crop instance IDs surviving across character/save loads. The crop instance registry is now cleared on character load, so reused Unity instance IDs from a previous save can no longer collide with unrelated new crops and report wrong growth state.

**Gifting Assistant** (new mod, **1.0.0**)

- **Daily gift routine:** New standalone mod with an in-game window (**Ctrl + G** by default) to build a per-character NPC gift roster, view loved/liked gift options with item icons, track who you've already gifted today, optionally show whether suggested gifts are in your bag, and sort by priority (Low / Normal / High / Urgent). Soft integrations with Birthday Reminder (birthday badge) and Sun Haven Todo (add reminder task).

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
