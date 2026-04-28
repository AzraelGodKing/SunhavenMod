# A Squirrel's Birthday Reminder

**Unreleased:** Lifecycle hardening pass — plugin teardown now unsubscribes scene handlers, disposes Todo integration event hooks, and classifies expected menu/quit destroys; integration reflection fallback paths now log debug details instead of failing silently.

**Version 1.4.0** — Never miss an NPC birthday in Sun Haven. Auto reminders, gift suggestions, gift tracking, draggable HUD.

**1.1.5:** Normalized duplicate composite NPC names from game data (e.g. `Darius+Darius` now displays as `Darius`) and aligned gift tracking lookups with normalized names. Keepalive cleanup: removed the no-op `Plugin.Update` path so runtime processing stays solely in the hidden persistent runner. Config file now uses `BepInEx/config/BirthdayReminder.cfg` (auto-migrates values from the legacy GUID-named config on first load).

**Compatibility:** Hotkeys pause while text entry is focused (chat, console, or input fields), so mods like CheatEnabler can use those fields without BirthdayReminder stealing keys—via shared `SunhavenMods.Shared.TextInputFocusGuard` (same helper other Azrael mods use). Hotkeys run only from the persistent runner (no duplicate path), and the **Enabled** config is honored at startup.

## Features

- **Auto Reminders** - Get notified when NPCs have birthdays
- **Gift Suggestions** - See loved and liked items for each NPC
- **Gift Tracking** - Track who you've already gifted today
- **Draggable HUD** - Position the reminder anywhere on screen

## Usage

The mod automatically displays birthday reminders when you load into the game on an NPC's birthday.

## Installation

1. Install BepInEx 5.x
2. Place `BirthdayReminder.dll` in `BepInEx/plugins/`
3. Launch the game. Reminders show automatically on NPC birthdays.

## Configuration

Edit `BepInEx/config/BirthdayReminder.cfg` to customize settings.

## Links

- [Nexus Mods](https://www.nexusmods.com/sunhaven/mods/493)
- [Documentation](https://azraelgodking.github.io/SunhavenMod/BirthdayReminder/BirthdayReminder.html)
- [Report Bugs on Discord](https://discord.gg/Vwh2y7qMXv)
