# The List

A Sun Haven mod that provides an in-game todo list and journal with per-character saves.

## Features

### Core Features
- **Task Management**: Add, edit, complete, and delete tasks
- **Categories**: Organize tasks by type (Farming, Mining, Quests, etc.)
- **Priority Levels**: Low, Normal, High, and Urgent priorities with visual indicators
- **Notes**: Add detailed notes to each task
- **Timestamps**: Track when tasks were created and completed
- **Per-Character Saves**: Each character has their own separate todo list

### UI Features
- **Main Window**: Full-featured task management interface
- **Mini HUD**: Persistent display showing pending task count and top priority task
- **Filtering**: Filter tasks by category
- **Sorting**: Sort by priority, date, or category
- **Show/Hide Completed**: Toggle visibility of completed tasks

## Controls

| Action | Key |
|--------|-----|
| Open/Close List | `J` |
| Open/Close List (Alt) | `F9` |
| Close List/Cancel Edit | `Escape` |

## Installation

1. Install [BepInEx 5.x](https://github.com/BepInEx/BepInEx) for Sun Haven
2. Download TheList.dll from releases
3. Copy `TheList.dll` to `Sun Haven/BepInEx/plugins/TheList/`
4. Launch the game

## Configuration

After first launch, edit the config file at:
`Sun Haven/BepInEx/config/com.azraelgodking.thelist.cfg`

| Setting | Default | Description |
|---------|---------|-------------|
| ToggleKey | J | Key to open/close list UI |
| RequireCtrlModifier | false | Require Ctrl with toggle key |
| AltToggleKey | F9 | Alternative key (no modifier) |
| EnableHUD | true | Show mini HUD with pending count |
| HUDPosition | TopRight | HUD screen position |
| AutoSaveInterval | 60 | Auto-save interval (seconds) |

## Task Categories

- General
- Farming
- Mining
- Fishing
- Combat
- Quests
- Social
- Crafting
- Shopping
- Other

## Priority Levels

| Priority | Symbol | Color |
|----------|--------|-------|
| Low | - | Gray |
| Normal | o | White |
| High | ! | Yellow |
| Urgent | !! | Red |

## License

Feel free to use, modify, and distribute this mod.

## Credits

- Created by AzraelGodKing
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [Harmony](https://github.com/pardeike/Harmony)
