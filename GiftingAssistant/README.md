# Gifting Assistant

Daily gift routine planner for Sun Haven — pick NPCs, see loved/liked gift options with icons, track who you've already gifted today, and sort your roster by priority.

## Features

- **Gift roster:** Add any NPC to a per-character daily routine list.
- **Gift suggestions:** Shows loved and liked gifts from live game data (`NPCGiftTable`) with item icons.
- **Gifted tracking:** Reads the game's `gaveGiftForDay` flag and lets you mark gifts manually; resets on a new day.
- **Inventory hints:** Optional indicator for whether you currently hold suggested gifts in your bag.
- **Priority sorting:** Low / Normal / High / Urgent — gifted NPCs sink to the bottom.
- **Soft integrations:** Birthday Reminder highlights today's birthdays; Sun Haven Todo can add a reminder task when `Integrations > ReminderMode` is set to **PushToTodo** (default **RosterOnly** — built-in daily roster only); Haven's Almanac shows gift roster progress when `UseAlmanacIntegration` is enabled (on by default).

## Hotkeys

| Action | Default |
|--------|---------|
| Toggle window | **Ctrl + G** |

Configurable in `BepInEx/config/GiftingAssistant.cfg`.

## Config

| Section | Key | Default | Description |
|---------|-----|---------|-------------|
| General | Enabled | true | Enable the mod |
| Hotkeys | ToggleKey | G | Window toggle key |
| Hotkeys | RequireCtrl | true | Require Ctrl with toggle key |
| Display | ShowInventoryPossession | true | Show bag counts for suggested gifts |
| Display | UIScale | 1.0 | Window scale |
| Saving | AutoSave | true | Periodic autosave |
| Saving | AutoSaveInterval | 60 | Autosave interval (seconds) |
| Integrations | ReminderMode | RosterOnly | **RosterOnly** = track gifts in this mod's daily roster (priorities, gifted-today). **PushToTodo** = same roster plus **+Todo** on each row when Sun Haven Todo is installed. Legacy `UseTodoIntegration = true` migrates to PushToTodo on first load. |
| Integrations | UseAlmanacIntegration | true | When Haven's Almanac is installed, share gift roster progress (pending count, priorities) with its HUD, dashboard, and daily briefing. Off = keep roster data private. |

Per-character roster saves to `BepInEx/config/com.azraelgodking.giftingassistant/<Character>_giftroster.json`.

## Dependencies

- **BepInEx 5.x**
- **Soft:** A Squirrel's Birthday Reminder, Sun Haven Todo, Haven's Almanac (optional integrations)

## Version

**1.0.0** — published in [`docs/versions.json`](../../docs/versions.json). Player-facing store text: [`thunderstore/README.md`](thunderstore/README.md).

## Changelog

Store release notes for each version also live in [`docs/versions.json`](../../docs/versions.json). Player-facing history across all mods: root [`CHANGELOG.md`](../../CHANGELOG.md).

### 1.0.0

Initial release — daily gift routine planner (**Ctrl + G** by default):

- Per-character NPC gift roster with priority sorting (Low / Normal / High / Urgent)
- Loved/liked gift suggestions with item icons, bag counts, and per-NPC preferred-gift picker
- Relationship heart bars on roster rows and the Add NPC picker
- Gifted-today tracking (game flag + manual toggle); gifted NPCs sink to the bottom; resets each new day
- Localized UI (16 languages) plus optional `[Localization] ForceEnglish`
- Optional integrations: Birthday Reminder (birthday badge), Sun Haven Todo (**PushToTodo** default — +Todo per row, daily refresh, in-game gift auto-complete; Gifted checkbox does not touch todos), Haven's Almanac (roster progress on HUD / dashboard / briefing when `UseAlmanacIntegration` is on)

