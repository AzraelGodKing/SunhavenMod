# Sun Haven Mods

A collection of BepInEx mods for [Sun Haven](https://store.steampowered.com/app/1432860/Sun_Haven/).

## Mods

| Mod | Description | Version |
|-----|-------------|---------|
| [**Senpai's Chest**](SenpaisChest/) | Smart chests with configurable item rules for automatic storage sorting | 2.2.4 |
| [**Sunhaven Todo**](SunhavenTodo/) | In-game todo list and journal with per-character saves | 1.1.8 |
| [**Sun Haven Museum Utility Tracker**](SunHavenMuseumUtilityTracker/) | Track museum donations across all sections | 2.2.7 |
| [**The Vault**](TheVault/) | Full rework of the vault system; classic core, single project; shop and inventory vault hooks | 3.0.5 |
| [**Haven's Birthright**](HavensBirthright/) | Unique racial bonuses and traits for each playable race; optional `[BonusTransfers]` cross-race passive grants (off by default) | 2.0.1 |
| [**Haven's Almanac**](HavensAlmanac/) | Mod compatibility registry and info hub | 1.0.5 |
| [**A Squirrel's Birthday Reminder**](BirthdayReminder/) | Reminds you of villagers' birthdays | 1.1.5 |
| [**Haven Dev Tools**](HavenDevTools/) | Developer utilities and debugging tools | 1.0.8 |
| [**Trinket Fortune**](TrinketFortune/) | Increases odds of unowned fishing trinkets dropping as you complete the aquarium | 1.0.3 |
| [**Faster Races**](FasterRaces/) | Configurable movement speed bonus; integrates with Haven's Birthright to avoid double speed | 1.1.3 |
| [**Justice for Harold**](JusticeForHarold/) | Quest interaction tweak for Harold's reward flow | 1.0.0 |

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
├── SharedUtilities/       # Shared code (VersionChecker, etc.)
├── scripts/               # Maintainer helpers (pre-push-build.ps1, remove-stale-github-releases.ps1)
└── docs/                  # Shared documentation
```

---

## Documentation site changelog

- **2026-04-11** — **Trinket Fortune load fix:** removed hard runtime dependency on `HavenDevTools.dll` to prevent startup black screens when HavenDevTools is not installed. Updated [`TrinketFortune/thunderstore/README.md`](TrinketFortune/thunderstore/README.md) and [`docs/versions.json`](docs/versions.json) changelog text (no version bump).
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