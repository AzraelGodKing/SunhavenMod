# Sun Haven Mods

A collection of BepInEx mods for [Sun Haven](https://store.steampowered.com/app/1432860/Sun_Haven/).

## Mods

| Mod | Description | Version |
|-----|-------------|---------|
| [**Senpai's Chest**](SenpaisChest/) | Smart chests with configurable item rules for automatic storage sorting | 2.0.1 |
| [**Sunhaven Todo**](SunhavenTodo/) | In-game todo list and journal with per-character saves | 1.1.2 |
| [**Sun Haven Museum Utility Tracker**](SunHavenMuseumUtilityTracker/) | Track museum donations across all sections | 2.2.1 |
| [**The Vault**](TheVault/) | Secure vault for tokens and keys with HUD and shop/door integration | 2.0.5 |
| [**Haven's Birthright**](HavensBirthright/) | Unique racial bonuses and traits for each playable race | 1.3.0 |
| [**Haven's Almanac**](HavensAlmanac/) | Mod compatibility registry and info hub | 1.0.1 |
| [**A Squirrel's Birthday Reminder**](BirthdayReminder/) | Reminds you of villagers' birthdays | 1.1.1 |
| [**Haven Dev Tools**](HavenDevTools/) | Developer utilities and debugging tools | 1.0.3 |

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

Secure storage for seasonal tokens, keys, and special currencies. Auto-deposit on pickup, persistent HUD, and seamless shop/door integration. Per-character vaults; open with **Ctrl+V** or **F8** (Steam Deck).

### Haven's Birthright

Adds unique racial bonuses and traits for each playable race in Sun Haven.

### Haven's Almanac

Mod compatibility registry. Detects installed mods and provides a centralized info hub for players and mod authors.

### A Squirrel's Birthday Reminder

Displays reminders for villagers' birthdays so you never miss a gift.

### Haven Dev Tools

Development and debugging utilities for mod authors. Not intended for typical players.

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
├── TheVault/              # Vault currency
├── HavensBirthright/      # Racial bonuses
├── HavensAlmanac/         # Mod registry
├── BirthdayReminder/      # Birthday reminders
├── HavenDevTools/         # Dev tools
├── SharedUtilities/       # Shared code (VersionChecker, etc.)
└── docs/                  # Shared documentation
```

---

## Links

- [Nexus Mods (Senpai's Chest)](https://www.nexusmods.com/sunhaven/mods/496)
- [Documentation](https://azraelgodking.github.io/SunhavenMod/)
- [Discord](https://discord.gg/Vwh2y7qMXv)
