# Senpai's Chest

**Version 1.2.0** — Smart chests with configurable item rules for hands-free storage organization in Sun Haven.

## Features

- **Smart Chests** — Mark any chest as a Smart Chest and define rules for which items it collects
- **Automatic Sorting** — Smart Chests periodically scan nearby chests and pull matching items (scan interval configurable)
- **Flexible Rules** — Filter items by:
  - **By Item** — Specific items (search by name)
  - **By Category** — Equip, Use, Craftable, Monster, Furniture, Quest
  - **By Item Type** — Normal, Armor, Food, Fish, Crop, Watering Can, Animal, Pet, Tool
  - **By Property** — Gems, Forageables, Animal Products, Meals, Fruits, Artisanry Items, Potions, Museum (Not Donated)
- **Per-Character Saves** — Each character's Smart Chest settings are saved separately
- **Multiplayer Safe** — Skips chests that other players are using

## Usage

1. Open any chest
2. Press **F9** (or your configured key) to open the Smart Chest configuration
3. Enable **Smart Chest** and add rules for items this chest should collect
4. Close the config and the chest
5. Matching items will be pulled from nearby chests automatically on each scan

## Configuration

Edit `BepInEx/config/com.azraelgodking.senpaischest.cfg`:

| Setting | Default | Description |
|---------|---------|-------------|
| ScanInterval | 60 | Seconds between scans (min: 10) |
| EnableNotifications | true | Show notifications when items are moved |
| MaxItemsPerScan | 50 | Max item stacks moved per scan (reduces lag) |
| ToggleKey | F9 | Key to open config UI while chest is open |
| RequireCtrlModifier | false | Require Ctrl held with toggle key |
| CheckForUpdates | true | Check for mod updates on startup |

## Notes

- Smart Chests do not pull from other Smart Chests (prevents loops)
- Chests in use by another player are skipped
- Configuration is tied to chest position; moving a chest resets its config

## Links

- [Nexus Mods](https://www.nexusmods.com/sunhaven/mods/496)
- [Documentation](https://azraelgodking.github.io/SunhavenMod/SenpaisChest/SenpaisChest.html)
- [Report Bugs / Discord](https://discord.gg/Vwh2y7qMXv)
