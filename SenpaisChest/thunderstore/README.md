# Senpai's Chest — Smart Chests for Sun Haven

Adds Smart Chests with configurable item rules for hands-free storage organization.

## Features

- **Smart Chests**: Mark any chest as a Smart Chest and configure rules for which items it should collect
- **Automatic Sorting**: Every 60 seconds (configurable), Smart Chests scan other chests in the area and pull matching items into themselves
- **Flexible Rules**: Filter items by:
  - **Item ID** — specific items
  - **Category** — Equip, Use, Craftable, Monster, Furniture, Quest
  - **Item Type** — Normal, Armor, Food, Fish, Crop, Tool, etc.
  - **Property** — Gems, Forageables, Animal Products, Meals, Fruits, Artisanry Items, Potions
- **Per-Character Saves**: Each character's Smart Chest configurations are saved independently
- **Multiplayer Safe**: Skips chests currently in use by other players

## Usage

1. Place a chest and interact with it (open it)
2. Press **F9** (configurable) to open the Smart Chest configuration window
3. Toggle "Smart Chest Enabled" on
4. Add rules for which items this chest should collect
5. Close the config window and the chest
6. Items matching your rules will automatically be pulled from other chests on the scan timer

## Configuration

Edit `BepInEx/config/com.azraelgodking.senpaischest.cfg`:

| Setting | Default | Description |
|---------|---------|-------------|
| ScanInterval | 60 | Seconds between scans (min: 10) |
| EnableNotifications | true | Show notifications when items move |
| MaxItemsPerScan | 50 | Max item stacks per scan cycle |
| ToggleKey | F9 | Key to open config UI |
| RequireCtrlModifier | false | Require Ctrl held with toggle key |

## Notes

- Smart Chests will not pull items from other Smart Chests (prevents loops)
- Chests currently being used by a player are skipped during scans
- Item rules are saved per chest position, so moving a chest resets its config
