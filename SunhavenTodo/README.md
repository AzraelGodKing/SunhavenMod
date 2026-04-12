# Sunhaven Todo

An in-game to-do list for Sun Haven with per-character saves, categories, priorities, and a parchment-styled UI.

## Features

- **In-Game Todo List**: Add, edit, complete, and delete tasks without leaving the game
- **Per-Character Lists**: Each character has their own separate todo file
- **Categories & Priorities**: Organize tasks by category and urgency
- **Search & Filters**: Search titles/descriptions, filter by category, and hide completed items
- **Progress Stats**: See totals and completion percentage at a glance
- **Auto-Save**: Saves automatically on a timer and on game exit

## Controls

| Action | Key |
|--------|-----|
| Toggle Todo List | `Ctrl + T` |
| Close Window | `Escape` |

## Installation

1. Install BepInEx 5.x for Sun Haven
2. Build or download `SunhavenTodo.dll`
3. Copy `SunhavenTodo.dll` to `Sun Haven/BepInEx/plugins/SunhavenTodo/`
4. Launch the game

## Configuration

After first launch, edit the config file at:
`Sun Haven/BepInEx/config/SunhavenTodo.cfg`

| Setting | Default | Description |
|---------|---------|-------------|
| ToggleKey | T | Key to open/close the todo list |
| RequireCtrl | true | Require Ctrl to be held with ToggleKey |
| AutoSave | true | Automatically save the todo list |
| AutoSaveInterval | 60 | Auto-save interval (seconds) |

## Data Storage

Todo lists are saved per character under:
`Sun Haven/BepInEx/config/com.azraelgodking.sunhaventodo/`

Each file is named like:
`<CharacterName>_todos.json`

Character names are sanitized to avoid invalid filename characters.

## Notes

- The window is draggable by the header
- The UI pauses player input while the todo list is open

## Links

- [Nexus Mods](https://www.nexusmods.com/sunhaven/mods/491)
- [Documentation](https://azraelgodking.github.io/SunhavenMod/Todo/todo.html)
- [Discord — bugs & discussion](https://discord.gg/Vwh2y7qMXv)

## License

Feel free to use, modify, and distribute this mod.

## Credits

- Created by AzraelGodKing
- Built with BepInEx and Harmony
