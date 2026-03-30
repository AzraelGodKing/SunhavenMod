# Sun Haven Mods

A collection of BepInEx mods for [Sun Haven](https://store.steampowered.com/app/1432860/Sun_Haven/).

## Mods

| Mod | Description | Version |
|-----|-------------|---------|
| [**Senpai's Chest**](SenpaisChest/) | Smart chests with configurable item rules for automatic storage sorting | 2.2.0 |
| [**Sunhaven Todo**](SunhavenTodo/) | In-game todo list and journal with per-character saves | 1.1.5 |
| [**Sun Haven Museum Utility Tracker**](SunHavenMuseumUtilityTracker/) | Track museum donations across all sections | 2.2.5 |
| [**The Vault**](TheVault/) | Full rework of the vault system; classic core, single project; shop and inventory vault hooks | 3.0.2 |
| [**Haven's Birthright**](HavensBirthright/) | Unique racial bonuses and traits for each playable race | 1.3.3 |
| [**Haven's Almanac**](HavensAlmanac/) | Mod compatibility registry and info hub | 1.0.3 |
| [**A Squirrel's Birthday Reminder**](BirthdayReminder/) | Reminds you of villagers' birthdays | 1.1.3 |
| [**Haven Dev Tools**](HavenDevTools/) | Developer utilities and debugging tools | 1.0.5 |
| [**Trinket Fortune**](TrinketFortune/) | Increases odds of unowned fishing trinkets dropping as you complete the aquarium | 1.0.0 |
| [**Faster Races**](FasterRaces/) | Configurable movement speed bonus; integrates with Haven's Birthright to avoid double speed | 1.1.1 |

---

### Senpai's Chest

Smart chests that automatically collect items matching your rules. Open a chest, press F9 to configure, add rules (by item, category, type, or property), and let the mod sort your storage.

### Sunhaven Todo

In-game todo list and journal. Create tasks with categories, priorities, and notes. Each character has a separate list that persists between sessions. Hotkey: **J** or **F9**.

### Sun Haven Museum Utility Tracker (S.M.U.T.)

Track museum donation progress across all three sections:
- **Hall of Gems** — Gems, minerals, ores, crystals
- **Hall of Culture** — Artifacts, fossils, relics
- **Aquarium** — Freshwater, saltwater, exotic fish

Per-character saves, item icons, and bundle progress. Hotkey: **Ctrl+C**.

### The Vault

**Full rework of The Vault system** (released as **3.0.0**): classic `VaultManager` / `VaultSaveSystem` stack, one codebase under `TheVault/`, Harmony integration rebased on `Wish.*` types, single build (`TheVault/TheVault.csproj`). Secure storage for seasonal tokens, keys, and special currencies; auto-deposit, HUD, shop and inventory vault hooks; **Ctrl+V** or **F8** (Steam Deck). Details: [`TheVault/README.md`](TheVault/README.md). `TheVault-legacy/` is archive/reference only.

### Haven's Birthright

Adds unique racial bonuses and traits for each playable race in Sun Haven.

### Haven's Almanac

Mod compatibility registry. Detects installed mods and provides a centralized info hub for players and mod authors.

### A Squirrel's Birthday Reminder

Displays reminders for villagers' birthdays so you never miss a gift.

### Haven Dev Tools

Development and debugging utilities for mod authors. Not intended for typical players.

### Trinket Fortune

Increases the odds of unowned fishing trinkets dropping as you complete the aquarium, reducing the grind for the last trinket. Works with S.M.U.T.

### Faster Races

Configurable percentage bonus to movement speed for all races. When installed with Haven's Birthright, disables Birthright's speed buffs so speed is not doubled.

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
└── docs/                  # Shared documentation
```

---

## Documentation site changelog

- **2026-03-27** — Nexus detailed-description BBCode: each mod folder includes `NexusMods-BBCode.txt` (content aligned with that mod’s README). Paths listed in [`docs/NexusMods-BBCode-Index.txt`](docs/NexusMods-BBCode-Index.txt). Removed monolithic `docs/NexusMods-Collection-BBCode.txt`.
- **2026-03-27** — Haven's Almanac: [`docs/HavensAlmanac/HavensAlmanac.html`](docs/HavensAlmanac/HavensAlmanac.html) and [`HavensAlmanac/thunderstore/README.md`](HavensAlmanac/thunderstore/README.md) aligned with **v1.0.3** / [`AlmanacConfig.cs`](HavensAlmanac/Config/AlmanacConfig.cs) (BepInEx section names, HUD position, `DailyBriefing`, `Updates`, `Display.UIScale`); briefing timing (load-in + new day); main-menu UI hide; note that Faster Races / Trinket Fortune are not integrated; related link to Senpai's Chest.
- **2026-03-27** — Mod Hub & docs UX: contract/pack/table fixes, jump nav (**sticky** + blur on hub), sharable `?q=` / `?tag=`, empty search state, mobile TOC + comparison hint, FAB safe-areas, related-mod **Also see** strips, SEO (`docs/og-card.png`, canonical / `og:url` / `og:image` on hub and mod pages, `twitter:image` on hub), game-compat callout, Almanac banner v1.0.3 + dismiss key `announce-almanac-v103`, 404 theme from `localStorage` + toggle, `prefers-reduced-motion` (hub scroll, entrance animations, 404), **site-wide search** (`docs/search-index.json`, **Ctrl+K**, Search FAB, keyboard list), anchor `scroll-margin-top`, print stylesheet (hides chrome); **`/`** and **`s`** focus the notice-board filter when site search is closed; live **board count**, search-index **preload**, site-search **status line** + whole-row click + hover sync + scroll active item; TOC spy scrolls active link into view; **Escape** blurs board filter.

## Links

- [Nexus Mods (Senpai's Chest)](https://www.nexusmods.com/sunhaven/mods/496)
- [Documentation](https://azraelgodking.github.io/SunhavenMod/)
- [Discord](https://discord.gg/Vwh2y7qMXv)
