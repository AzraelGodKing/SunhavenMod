# Haven's Almanac

**Version 1.0.4** — Unified mod dashboard for Sun Haven. Aggregates data from supported installed mods: compact HUD, full dashboard (Ctrl+F5 by default), daily briefing, draggable UI, optional UI scale and update check.

## Features

- **Compact HUD** - Always-visible widget showing a one-line summary per installed mod
- **Full Dashboard** - Expandable window with detailed sections from each mod (Ctrl+F5)
- **Daily Briefing** - Summary after load-in and when a new day starts (sleep); key info for the day
- **Draggable** - Position the HUD anywhere on screen, position is saved

## Supported Mods

Haven's Almanac soft-depends on these mods and will display data from any that are installed:

- **Sun Haven Todo** - Active tasks and completion progress
- **A Squirrel's Birthday Reminder** - Today's birthdays and ungifted NPCs
- **S.M.U.T. (Museum Tracker)** - Museum donation progress
- **Senpai's Chest** - Smart chest count
- **The Vault** - Stored currencies
- **Haven's Birthright** - Racial bonuses
- **Haven Dev Tools** - Dev tools status

At least one supported mod must be installed for the Almanac to display content.

## Hotkeys

- **Ctrl+F5** - Toggle the full dashboard
- **F4** - Toggle the HUD
- **Escape** - Dismiss the daily briefing

## Installation

1. Install BepInEx 5.x
2. Place `HavensAlmanac.dll` in `BepInEx/plugins/HavensAlmanac/`
3. Launch the game

## Configuration

Edit `BepInEx/config/com.azraelgodking.havensalmanac.cfg` to customize hotkeys, HUD visibility, and daily briefing settings.

## Links

- [Documentation](https://azraelgodking.github.io/SunhavenMod/HavensAlmanac/HavensAlmanac.html)
- [Report Bugs on Discord](https://discord.gg/Vwh2y7qMXv)
