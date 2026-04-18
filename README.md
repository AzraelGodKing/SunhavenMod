# Sun Haven Mods

A collection of BepInEx mods for [Sun Haven](https://store.steampowered.com/app/1432860/Sun_Haven/).

## Mods

| Mod | Description | Version |
|-----|-------------|---------|
| [**Senpai's Chest**](SenpaisChest/) | Smart chests with configurable item rules for automatic storage sorting | 2.2.5 |
| [**Sunhaven Todo**](SunhavenTodo/) | In-game todo list and journal with per-character saves | 1.1.9 |
| [**Sun Haven Museum Utility Tracker**](SunHavenMuseumUtilityTracker/) | Track museum donations across all sections | 2.2.8 |
| [**The Vault**](TheVault/) | Full rework of the vault system; classic core, single project; shop and inventory vault hooks | 3.0.6 |
| [**Haven's Birthright**](HavensBirthright/) | Unique racial bonuses and traits for each playable race; optional `[BonusTransfers]` cross-race passive grants (off by default) | 2.0.2 |
| [**Haven's Almanac**](HavensAlmanac/) | Mod compatibility registry and info hub | 1.1.0 |
| [**A Squirrel's Birthday Reminder**](BirthdayReminder/) | Reminds you of villagers' birthdays | 1.1.6 |
| [**Haven Dev Tools**](HavenDevTools/) | Developer utilities and debugging tools | 1.0.9 |
| [**Trinket Fortune**](TrinketFortune/) | Increases odds of unowned fishing trinkets dropping as you complete the aquarium | 1.0.4 |
| [**Faster Races**](FasterRaces/) | Configurable movement speed bonus; integrates with Haven's Birthright to avoid double speed | 1.1.4 |
| [**Justice for Harold**](JusticeForHarold/) | Quest interaction tweak for Harold's reward flow | 1.0.0 |
| [**Crop Optimizer**](CropOptimizer/) | Crop forecast HUD with soft Todo/Birthday/Vault integrations | 1.0.0 |

---

### Senpai's Chest

Smart chests that automatically collect items matching your rules. Open a chest, press F9 to configure, add rules (by item, category, type, or property), and let the mod sort your storage.

### Sunhaven Todo

In-game todo list and journal. Create tasks with categories, priorities, and notes. Each character has a separate list that persists between sessions. Hotkey: **Ctrl+T** by default.

### Sun Haven Museum Utility Tracker (S.M.U.T.)

Track museum donation progress across all three sections:
- **Hall of Gems** — Gems, minerals, ores, crystals
- **Hall of Culture** — Artifacts, fossils, relics
- **Aquarium** — Freshwater, saltwater, exotic fish

Per-character saves, item icons, and bundle progress. Hotkey: **Ctrl+C**.

### The Vault

**Full rework of The Vault system** (current release **3.0.5**): classic `VaultManager` / `VaultSaveSystem` stack, one codebase under `TheVault/`, Harmony integration rebased on `Wish.*` types, single build (`TheVault/TheVault.csproj`). Secure storage for seasonal tokens, keys, and special currencies; auto-deposit, HUD, shop and inventory vault hooks; **Ctrl+V** or **F8** (Steam Deck). Details: [`TheVault/README.md`]

### Haven's Birthright

Adds unique racial bonuses and traits for each playable race in Sun Haven. Optional **`[BonusTransfers]`** in `HavensBirthright.cfg` lets you grant extra **passive** table bonuses to one race using the configured values from another (`TargetRace|SourceRace|BonusType`, semicolon-separated). **`EnableBonusTransfers`** defaults to **false**. Does not copy active abilities, drawbacks, or synergies.

### Haven's Almanac

Mod compatibility registry. Detects installed mods and provides a centralized info hub for players and mod authors.

### A Squirrel's Birthday Reminder

Displays reminders for villagers' birthdays so you never miss a gift.

### Haven Dev Tools

Development and debugging utilities for mod authors. Not intended for typical players.

### Trinket Fortune

Increases the odds of unowned fishing trinkets dropping as you complete the aquarium, reducing the grind for the last trinket. Works with S.M.U.T.

### Faster Races

Configurable percentage bonus to movement speed for all races. With Haven's Birthright installed, Birthright speed bonuses are suppressed only while Faster Races speed bonus is active.

### Justice for Harold

Small quest behavior tweak focused on Harold interaction outcomes.

### Crop Optimizer

Adds crop forecast tracking and a compact HUD with soft integrations for Sunhaven Todo, Birthday Reminder, The Vault, and Haven's Almanac.

---

## Installation

1. Install [BepInEx 5.x](https://docs.bepinex.dev/articles/user_guide/installation/index.html) for Sun Haven.
2. Download the mod or build from source.
3. Place the mod DLL in `Sun Haven/BepInEx/plugins/` (or in a subfolder such as `SenpaisChest/`).
4. Launch the game.

### Build from Source

```bash
dotnet build SunhavenMod/<ModName>/<ModName>.csproj
```

Output goes to `bin/Debug/net48/` and is typically copied to the BepInEx plugins folder.

---

## Repository Layout

```
SunhavenMod/
├── SenpaisChest/          # Smart chests
├── SunhavenTodo/          # Todo list & journal
├── SunHavenMuseumUtilityTracker/  # Museum tracker
├── TheVault/              # Vault (current; TheVault.dll)
├── TheVault-legacy/       # Archive notes / optional Decomplied reference (not built)
├── HavensBirthright/      # Racial bonuses
├── HavensAlmanac/         # Mod registry
├── BirthdayReminder/      # Birthday reminders
├── HavenDevTools/         # Dev tools
├── TrinketFortune/        # Fishing trinket drop rates
├── FasterRaces/           # Movement speed bonus
├── CropOptimizer/         # Crop forecasting + HUD
├── SharedUtilities/       # Shared code (VersionChecker, etc.)
├── scripts/               # Maintainer helpers (pre-push-build.ps1, remove-stale-github-releases.ps1)
└── docs/                  # Shared documentation
```

---

## Documentation site changelog

- **2026-04-18** — **Docs: Haven's Almanac-related pages refreshed for the 1.0.6 shipped state + today's Mod Health fix.** [`docs/HavensAlmanac/HavensAlmanac.html`](docs/HavensAlmanac/HavensAlmanac.html) breadcrumb date bumped from "Updated Mar 2026" → "Updated Apr 2026"; the Daily Briefing feature card now reflects the "only opens when noteworthy" gate and the auto-dismiss countdown; the `AutoDismissSeconds` config-table row notes the under-Dismiss countdown label; a new **Built-in: Mod Health** section sits below Supported Mods explaining that the always-on telemetry panel aggregates `SunhavenMods.Shared.VersionChecker` snapshots across every loaded mod DLL via reflection (and that a mod that never calls `CheckForUpdate` won't appear). [`docs/index.html`](docs/index.html) announcement banner moved off the ancient `v1.0.3` text + `data-dismiss-key="announce-almanac-v103"` (anyone who dismissed the v1.0.3 banner was missing every refresh since) to `v1.0.6` copy highlighting Crop Optimizer, quieter briefing, and cross-mod Mod Health — new dismiss key `announce-almanac-v106-modhealth` so the refresh actually re-appears for prior dismissals; the Haven's Almanac mod card's feature chip bumped **7 → 8 Mod Integrations** (the Supported Mods grid on the page already listed all 8 — Crop Optimizer had been added earlier in the branch without refreshing this chip) and "Daily Briefing" → "Smart Daily Briefing" to match the new gating. [`docs/search-index.json`](docs/search-index.json) entry description expanded to mention mod-health aggregation, and keywords now include `modhealth version checker telemetry auto-dismiss almanac` so Ctrl+K / site-search surfaces the page for those queries. JSON validated, no lint errors. No version bump.
- **2026-04-18** — **Haven's Almanac: Mod Health now surfaces every mod, not just the Almanac itself.** The dashboard's **Mod Health** panel had been showing `1 checked / 0 issues` with a single row for Haven's Almanac even though the boot log proved 8 mods (Birthright, Sunhaven Todo, Birthday Reminder, The Vault, Museum Tracker, Senpai's Chest, Haven Dev Tools, Haven's Almanac) were successfully running `VersionChecker.CheckForUpdate` on startup. Root cause: each mod compiles its **own** copy of [`SharedUtilities/VersionChecker.cs`](SharedUtilities/VersionChecker.cs) into its DLL (via the `<Compile Include="..\SharedUtilities\VersionChecker.cs">` link pattern + suppressed CS0436), so every mod has a distinct `SunhavenMods.Shared.VersionChecker` runtime Type with its own private static `HealthByPluginGuid` dictionary containing exactly one entry — itself. Haven's Almanac's provider was only asking **its own** dictionary, which by definition only knows about Haven's Almanac. [`HavensAlmanac/Integration/ModHealthDataProvider.cs`](HavensAlmanac/Integration/ModHealthDataProvider.cs) now reflects across every loaded assembly, finds each assembly's `SunhavenMods.Shared.VersionChecker` Type if present, pulls its private static `HealthByPluginGuid` field, reads each snapshot's `LastCheckUtc` / `ExceptionCount` / `LastError` shape-compatibly (the POCO is identical across DLLs but each is technically a different Type, so we reflect on the returned instances rather than casting), and merges per-GUID entries into one view — keeping the freshest `LastCheckUtc` and max `ExceptionCount` per GUID. Field lookups are cached keyed on `AppDomain.CurrentDomain.GetAssemblies().Length` (assemblies don't churn after chainload, but a cheap re-scan kicks in if one ever does), and friendly display names resolve from `Chainloader.PluginInfos[guid].Metadata.Name` so the dashboard now shows `Sun Haven Todo` / `The Vault` / `Senpai's Chest` rows instead of raw GUIDs. HUD summary correspondingly jumps from `1 checked / 0 issues` to `8 checked / 0 issues` for a typical full-stack install. Still gated on the mod actually calling `CheckForUpdate` — Crop Optimizer currently doesn't, so it won't appear until that's addressed in a separate Crop Optimizer branch. No version bump per repo rule; fix lives on `feat/havens-almanac-improvements`.
- **2026-04-18** — **Haven's Almanac: V1 bug sweep on `feat/havens-almanac-improvements`.** Eight audit-confirmed defects fixed + three tech-debt cleanups, no version bump. Briefing now honors `AutoDismissSeconds` (it was bound in `AlmanacConfig` but never read by `DailyBriefing.Update`) with a live "Auto-dismissing in Ns" label under the Dismiss button. Briefing no longer opens when there's nothing to say — added `IModDataProvider.HasBriefingContent` + `AlmanacDataAggregator.HasAnyBriefingContent` so the window gates on "will any provider actually emit content?" instead of the looser `IsReady` check, which the always-on Mod Health provider was satisfying vacuously. Fixed the orphan section-header bug by skipping providers whose `HasBriefingContent` is false *before* drawing their `{icon} {name}` title, and replaced the latent double-`EndVertical` in the old `catch` block with a `try/finally` + `verticalOpened` flag inside a new `TryDrawProviderSection` helper so an exception mid-draw can't corrupt the IMGUI layout stack. Tightened the Museum briefing (was `return true` unconditionally → now only fires within 5 items of completion, using the same threshold for `HasBriefingContent` and `DrawBriefingSection`). Killed the unreachable "No supported mods detected" warning path in `Plugin.Awake`, the "No mods detected" line in `AlmanacHUD.DrawWindow`, and the matching placeholder in `AlmanacDashboard.DrawWindow` by swapping `InstalledModCount == 0` → new `IntegrationModCount == 0` (excludes the always-registered Mod Health provider), with friendlier copy that names the eight companion mods. Docs drift fixes: the [`docs/HavensAlmanac/HavensAlmanac.html`](docs/HavensAlmanac/HavensAlmanac.html) Configuration note now says `HavensAlmanac.cfg` with a 1.0.5+ migration aside (was the legacy `com.azraelgodking.havensalmanac.cfg`), and both the Supported Mods grid there and the bullet list in [`HavensAlmanac/thunderstore/README.md`](HavensAlmanac/thunderstore/README.md) gained a Crop Optimizer entry to match the `InitializeIntegrations` call that already registers it. Tech debt: [`ChestDataProvider`](HavensAlmanac/Integration/ChestDataProvider.cs) now caches the reflected `Type`/`MethodInfo`/`PropertyInfo`/`FieldInfo` handles for `SenpaisChest.Plugin` → `GetManager` → `GetSaveData` → `ChestConfigs` after the first successful probe (was resolving all four every 5s refresh), and the `Inventory_decompiled.cs` dnSpy dump that was sitting in the mod's project root moved to [`HavensAlmanac/_refs/`](HavensAlmanac/_refs/) with a folder README; the csproj exclusion broadened to `_refs\**\*.cs` + `<None Include=...>` so future decompiles auto-route there without more wiring. Added a root [`HavensAlmanac/README.md`](HavensAlmanac/README.md) matching the pattern of the other mod folders' root READMEs (it previously only had `thunderstore/README.md`). No version bump per repo rule — 1.0.6 remains the shipped version until the branch is reviewed and a release cadence is picked.
- **2026-04-17** — **Crop Optimizer: Thunderstore URL wired.** Added `"thunderstore": "https://thunderstore.io/c/sun-haven/p/AzraelGodKing/CropOptimizer/"` to the `com.azraelgodking.cropoptimizer` entry in [`docs/versions.json`](docs/versions.json). With this slotted in, [`docs/shared.js`](docs/shared.js) now renders a Thunderstore download button in the `<nav class="download-nav">` on [`docs/CropOptimizer/CropOptimizer.html`](docs/CropOptimizer/CropOptimizer.html) alongside the Nexus and GitHub links, and the Discord release ping gets a working `Thunderstore: …` line. The package is live at `AzraelGodKing-CropOptimizer-1.0.0` with the expected `BepInEx-BepInExPack-5.4.2100` dependency. Display-only edit — no rebuild, no version bump.
- **2026-04-17** — **Crop Optimizer: Nexus page URL wired.** Added `"nexus": "https://www.nexusmods.com/sunhaven/mods/500"` to the `com.azraelgodking.cropoptimizer` entry in [`docs/versions.json`](docs/versions.json) (canonical form, `?tab=files` query stripped to match the other mods' stored URLs). This unlocks three display surfaces that all read `m.nexus` dynamically: (1) the `<nav class="download-nav" data-mod-key="com.azraelgodking.cropoptimizer">` on [`docs/CropOptimizer/CropOptimizer.html`](docs/CropOptimizer/CropOptimizer.html), which [`docs/shared.js`](docs/shared.js) populates with Thunderstore / Nexus / GitHub download buttons at load time; (2) the `Nexus Mods: ${{ steps.config.outputs.nexus_url }}` line in the Discord release ping on `build-release-publish.yml`; (3) the `nexus_url` output consumed by downstream workflow steps. No rebuild or version change needed — this is a display-only field. The already-wired `nexus_file_group_id=7320911` remains the source of truth for the upload action.
- **2026-04-17** — **Crop Optimizer: Nexus file group wired.** Added `"nexus_file_group_id": "7320911"` to the `com.azraelgodking.cropoptimizer` entry in [`docs/versions.json`](docs/versions.json). The release workflow already reads this key (line `NEXUS_FILE_GROUP_ID=$(jq -r ".[\"$JSON_KEY\"].nexus_file_group_id // empty" docs/versions.json)`), and the condition `steps.config.outputs.nexus_file_group_id != ''` now evaluates true for Crop Optimizer — so picking `cropoptimizer` in **Release & Publish** with `publish_nexus=true` will push the zip through the 4-attempt retry chain just added in the previous entry.
- **2026-04-17** — **CI: Nexus upload hardened against v3 API flakes.** `Nexus-Mods/upload-action@v1.0.0-beta.3` was throwing `TypeError: Cannot read properties of undefined (reading 'length')` right after `Requesting multipart upload from: /uploads/multipart` on almost every release. Traced it to the Nexus v3 `POST /uploads/multipart` endpoint intermittently returning `201 OK` with a body where `data.part_presigned_urls` is undefined, which the action then destructures and calls `.length` on (their README openly flags v3 as "evaluation only"). No newer action version exists (`v1.0.0-beta.3` is the latest tag). Replaced the single `Upload to Nexus Mods` step in both [`.github/workflows/build-release-publish.yml`](.github/workflows/build-release-publish.yml) and [`.github/workflows/test-self-hosted-sunhaven-runner.yml`](.github/workflows/test-self-hosted-sunhaven-runner.yml) with a **4-attempt retry chain** (`continue-on-error: true` on each, `id: nexus_upload_{1..4}`, cascading `steps.nexus_upload_N.outcome == 'failure'` guards, and shell `sleep` gap steps of **30s → 2min → 5min** between tries — ~7.5 min worst case). Added a final `Nexus upload summary` step that writes to `$GITHUB_STEP_SUMMARY`: ✅ which attempt succeeded, or ⚠️ a "GitHub/Thunderstore already published, here's how to retry just Nexus" recovery message with the dist zip path. Net effect: a Nexus flake no longer takes down the GitHub Release, Thunderstore publish, or Discord notification for the rest of the matrix. Also expanded the `NEXUS:` comment block at the top of `build-release-publish.yml` with the new failure signature.
- **2026-04-17** — **CI: Crop Optimizer added to the build-and-release matrix.** The two manual GitHub Actions workflows were missing `cropoptimizer` in their `workflow_dispatch.inputs.mod` choice list, the `all` fan-out array, the build-job `case "$MOD_KEY"` block (`MOD_DIR`/`DLL_NAME`/`CSPROJ`), the release-job `case "$MOD_KEY"` block (`JSON_KEY=com.azraelgodking.cropoptimizer`, `THUNDERSTORE_NAME=CropOptimizer`), and the `test_discord` random-mod array — so the mod could not be selected or rolled into an `all` release. Wired those five slots in both [`.github/workflows/build-release-publish.yml`](.github/workflows/build-release-publish.yml) and [`.github/workflows/test-self-hosted-sunhaven-runner.yml`](.github/workflows/test-self-hosted-sunhaven-runner.yml), which also makes the Crop Optimizer entry in `docs/versions.json` actually drive the GitHub Release / Thunderstore publish / Discord notification paths (Nexus auto-skips because `nexus_file_group_id` is empty, same as Haven's Almanac — happy to flip a switch if you ever add a Nexus page). Also updated [`scripts/pre-push-build.ps1`](scripts/pre-push-build.ps1) to point `cropoptimizer`'s `DocsHtml` at the shipped `CropOptimizer\CropOptimizer.html` so the local pre-push check links the doc page instead of `$null`. Per the repo rule no versions were touched.
- **2026-04-17** — **Faster Races v1.1.4 staged: Configuration Manager compatibility fix + version sync.** Last *released* build was **1.1.3** (the one shipped after the `FasterRaces.cfg` rename / migration). The next build carries the cross-cutting Configuration Manager fix &mdash; Faster Races binds its settings to `FasterRaces.cfg` (not the default per-GUID file), but `BepInEx.ConfigurationManager` only inspects each plugin's inherited `BaseUnityPlugin.Config`, so the toggles were invisible in the in-game UI. The shared `SunhavenMods.Shared.ConfigFileHelper.ReplacePluginConfig` helper now rewires that inherited property to the custom file via reflection right after `Awake`, so `Enabled` and `SpeedBonusPercent` are live-editable in Configuration Manager with no file-path change. To match the rest of the repo, [`FasterRaces/PluginInfo.cs`](FasterRaces/PluginInfo.cs) was also bumped **1.1.3 &rarr; 1.1.4** (the csproj, `thunderstore/manifest.json`, `thunderstore/README.md` header, `NexusMods-BBCode.txt`, the hub mod table, and `docs/versions.json` were already on `1.1.4`); with `PluginInfo.cs` aligned, `scripts/verify-version-consistency.py` now passes for `com.azraelgodking.fasterraces`, the next build's DLL logs `Faster Races v1.1.4 loaded`, and the Thunderstore / Nexus / hub release notes all describe the Configuration Manager fix instead of re-hashing the 1.1.3 config-migration note.
- **2026-04-17** — **Docs: Crop Optimizer now syncs its version badge from `versions.json`.** Added `"Crop Optimizer": "com.azraelgodking.cropoptimizer"` to both the hub `nameToKey` map and the per-page `navToKey` map in [`docs/shared.js`](docs/shared.js), so the mod-card badge on [`docs/index.html`](docs/index.html) and the hero `.version-badge` on [`docs/CropOptimizer/CropOptimizer.html`](docs/CropOptimizer/CropOptimizer.html) are overwritten from `versions.json` at load time. The hardcoded `v1.0.0` in the HTML is now just a pre-JS fallback for crawlers / no-JS readers.
- **2026-04-17** — **Crop Optimizer page shipped:** added [`docs/CropOptimizer/CropOptimizer.html`](docs/CropOptimizer/CropOptimizer.html) + [`docs/CropOptimizer/crop-style.css`](docs/CropOptimizer/crop-style.css) (field-journal parchment theme: sprout green + harvest gold on warm parchment, dark-mode twilight-field variant). Added a `crop-theme` mod card with `crop-btn` CTA and a pulsing sprout-green `crop-badge` to [`docs/index.html`](docs/index.html) / [`docs/index-style.css`](docs/index-style.css); bumped the guild-header contract counter from 10 to 11. Page covers HUD, hover tooltip, growth ETA, quality tier, water / fertilizer detection (direct `TileManager.waterTileMap` read), projected sell value, soft integrations (Sunhaven Todo / Birthday Reminder / The Vault / Haven's Almanac), a tooltip-anatomy grid, compatibility notes, and the full `CropOptimizer.cfg` table. Registered the page in [`docs/search-index.json`](docs/search-index.json) so it surfaces in site-wide search / `Ctrl+K`.
- **2026-04-17** — **All mods: configs now visible in BepInEx Configuration Manager.** Every mod in this repo binds to a custom `ConfigFile` (e.g. `CropOptimizer.cfg`, `thevault.cfg`, `HavensBirthright.cfg`) rather than the default per-GUID file, and Configuration Manager only scans each plugin's inherited `BaseUnityPlugin.Config` property — so it never saw any of our entries. Added [`SharedUtilities/ConfigFileHelper.ReplacePluginConfig`](SharedUtilities/ConfigFileHelper.cs) which rewires that inherited property to the custom file via reflection right after Awake (applied in every plugin's `Awake`), so the live config UI now picks up every `Config.Bind(...)` call without changing any config file names or paths.
- **2026-04-16** — Persistent runner `OnDestroy` logs now distinguish expected teardown (app quit/menu unload → info) from unexpected in-session destruction (warning) across shared runner base and mods with custom runner classes.
- **2026-04-16** — Sun Haven Museum Utility Tracker log spam reduction: `GetCurrentCharacterName` now logs source/name only on change, preventing repeated identical `LastLoadedCharacterName` info lines during poll loops.
- **2026-04-16** — The Vault log spam reduction: `GetCurrentCharacterName` now logs source/name only when it changes (instead of every poll), preventing massive repeated `LastLoadedCharacterName` info lines in `LogOutput.log`.
- **2026-04-16** — CI **Collect build outputs** copies `TheVault.Abstractions.dll` from `bin/Release/netstandard2.0/` (the abstractions project targets netstandard2.0, not net48).
- **2026-04-16** — `verify-version-consistency.py` reads JSON and sources with **UTF-8 with BOM** (`utf-8-sig`) so `docs/versions.json` saved from editors that emit a BOM does not break CI.
- **2026-04-16** — **Version → build → release:** added [`docs/VERSION_AND_RELEASE.md`](docs/VERSION_AND_RELEASE.md) and [`scripts/verify-version-consistency.py`](scripts/verify-version-consistency.py). Release and test workflows run the verifier in the setup job so `docs/versions.json`, `PLUGIN_VERSION`, and `thunderstore/manifest.json` agree before the self-hosted build.
- **2026-04-16** — `CopyToPlugins` targets skip copying into `$(BepInExPath)/plugins` when `SunhavenCopyToBepInExPlugins` is false (CI uses `-p:SunhavenCopyToBepInExPlugins=false` because the `/sunhaven` mount is read-only); local builds still copy to the game by default.
- **2026-04-16** — Self-hosted build jobs pass MSBuild properties `-p:BepInExPath` and `-p:ManagedPath` (container paths `/sunhaven/BepInEx` and `/sunhaven/Sun Haven_Data/Managed`) in the **Build mod** step so `dotnet build` resolves game assemblies.
- **2026-04-16** — Self-hosted Linux build jobs set `DOTNET_INSTALL_DIR` / `DOTNET_ROOT` to `${{ runner.temp }}/dotnet` so `actions/setup-dotnet` does not try to install under `/usr/share/dotnet` (non-root runners get “Permission denied”).
- **2026-04-16** — Self-hosted build jobs use `runs-on: [self-hosted, Linux, X64, sunhaven]` to match the registered runner (labels are **case-sensitive**).
- **2026-04-16** — Added [`Test — Self-hosted Sunhaven runner`](.github/workflows/test-self-hosted-sunhaven-runner.yml): same build/package shape as Release & Publish with **`dry_run` defaulting to true** (workflow artifacts only; no GitHub Release / Thunderstore / Nexus / Discord until you set `dry_run` to false).
- **2026-04-16** — Updated the release workflow build job to use a self-hosted runner for .NET mod builds (`actions/setup-dotnet`, DLL artifacts) instead of committing pre-built DLLs [`.github/workflows/build-release-publish.yml`](.github/workflows/build-release-publish.yml).
- **2026-04-16** — Added a local Docker .NET SDK builder setup (`Dockerfile.dotnet-sdk`, `docker-compose.dotnet-sdk.yml`, `.env.dotnet-sdk.example`) and usage doc at [`docs/DOCKER_DOTNET_CONTAINER.md`](docs/DOCKER_DOTNET_CONTAINER.md) to run .NET commands in a container on your own PC.
- **2026-04-11** — **Trinket Fortune load fix:** removed hard runtime dependency on `HavenDevTools.dll` to prevent startup black screens when HavenDevTools is not installed. Updated [`TrinketFortune/thunderstore/README.md`](TrinketFortune/thunderstore/README.md) and [`docs/versions.json`](docs/versions.json) changelog text (no version bump).
- **2026-04-15** — Added new mod scaffold [`CropOptimizer/`](CropOptimizer/) with crop-growth Harmony hook, HUD, soft integrations (SunhavenTodo/BirthdayReminder/TheVault), Almanac provider wiring, and release metadata entries.
- **2026-04-11** — Maintainer/compat pass: added [`docs/MAINTAINER_VERSION_DRIFT_CHECKLIST.md`](docs/MAINTAINER_VERSION_DRIFT_CHECKLIST.md) and [`docs/COMPATIBILITY_CONTRACT.md`](docs/COMPATIBILITY_CONTRACT.md); aligned Sunhaven Todo + The Vault docs text drift; updated release metadata URLs (`SenpaisChest`, `HavenDevTools`, `TrinketFortune`); expanded [`builds/README.md`](builds/README.md) artifact map; added concise repo READMEs for `BirthdayReminder`, `FasterRaces`, and `SenpaisChest`.
- **2026-04-09** — **Haven's Birthright v2.0.0:** Changing saves or characters—and Fire/Water Elemental behavior after loads and F9—is more reliable. The code was reorganized for upkeep, and the mod keeps its hidden runner alive with shared SceneRootSurvivor instead of a third-party Keep Alive mod. Docs: [`docs/RacialBonuses/RacialBonuses.html`](docs/RacialBonuses/RacialBonuses.html), [`docs/index.html`](docs/index.html); source changelog [`HavensBirthright/README.md`](HavensBirthright/README.md); [`docs/versions.json`](docs/versions.json) for live hub badges.
- **2026-04-05** — Maintainer script [`scripts/remove-stale-github-releases.ps1`](scripts/remove-stale-github-releases.ps1): removes GitHub releases whose tags are not the current `{thunderstoreName}-v{version}` from [`docs/versions.json`](docs/versions.json). **GitHub Actions:** workflow [`.github/workflows/remove-stale-github-releases.yml`](.github/workflows/remove-stale-github-releases.yml) (manual dispatch; **dry run** default; turn off dry run + enable **confirm_delete** to delete). **Local:** set **`GITHUB_TOKEN`** (classic PAT, **repo** scope) or **`-Token`**; **`-WhatIf`** first; **`YES`** or **`-Force`**.
- **2026-04-03** — **The Vault:** [`TheVault/README.md`](TheVault/README.md) aligned with **v3.0.4** (version + changelog **3.0.3** / **3.0.4**); removed erroneous in-repo breaking callout; HUD usage text matches cfg keys; intro links **`TheVault.Abstractions/`**.
- **2026-03-31** — **The Vault:** HUD is draggable via the top accent strip; **`[HUD] PositionX`** / **`PositionY`** in `thevault.cfg` persist placement.
- **2026-03-31** — **Thunderstore readmes:** [`TheVault/thunderstore/README.md`](TheVault/thunderstore/README.md) and [`TrinketFortune/thunderstore/README.md`](TrinketFortune/thunderstore/README.md) now include a `**Version X.Y.Z**` line so `pre-push-build.ps1` can keep them in sync with `docs/versions.json`.
- **2026-03-31** — **Repo:** [`scripts/pre-push-build.ps1`](scripts/pre-push-build.ps1): `-All` without `-Bump` syncs every mod from `docs/versions.json` and builds all; `-BuildOnly` with `-Mod` or `-All` runs `dotnet build` only (no version file edits; does not require `versions.json`).
- **2026-03-27** — Nexus detailed-description BBCode: each mod folder includes `NexusMods-BBCode.txt` (content aligned with that mod’s README). Paths listed in [`docs/NexusMods-BBCode-Index.txt`](docs/NexusMods-BBCode-Index.txt). Removed monolithic `docs/NexusMods-Collection-BBCode.txt`.
- **2026-03-27** — Haven's Almanac: [`docs/HavensAlmanac/HavensAlmanac.html`](docs/HavensAlmanac/HavensAlmanac.html) and [`HavensAlmanac/thunderstore/README.md`](HavensAlmanac/thunderstore/README.md) aligned with **v1.0.3** / [`AlmanacConfig.cs`](HavensAlmanac/Config/AlmanacConfig.cs) (BepInEx section names, HUD position, `DailyBriefing`, `Updates`, `Display.UIScale`); briefing timing (load-in + new day); main-menu UI hide; note that Faster Races / Trinket Fortune are not integrated; related link to Senpai's Chest.
- **2026-03-27** — Mod Hub & docs UX: contract/pack/table fixes, jump nav (**sticky** + blur on hub), sharable `?q=` / `?tag=`, empty search state, mobile TOC + comparison hint, FAB safe-areas, related-mod **Also see** strips, SEO (`docs/og-card.png`, canonical / `og:url` / `og:image` on hub and mod pages, `twitter:image` on hub), game-compat callout, Almanac banner v1.0.3 + dismiss key `announce-almanac-v103`, 404 theme from `localStorage` + toggle, `prefers-reduced-motion` (hub scroll, entrance animations, 404), **site-wide search** (`docs/search-index.json`, **Ctrl+K**, Search FAB, keyboard list), anchor `scroll-margin-top`, print stylesheet (hides chrome); **`/`** and **`s`** focus the notice-board filter when site search is closed; live **board count**, search-index **preload**, site-search **status line** + whole-row click + hover sync + scroll active item; TOC spy scrolls active link into view; **Escape** blurs board filter.

## Links

- [Nexus Mods](https://www.nexusmods.com/profile/AzraelGodKing/mods)
- [Documentation](https://azraelgodking.github.io/SunhavenMod/)
- [Discord](https://discord.gg/Vwh2y7qMXv)