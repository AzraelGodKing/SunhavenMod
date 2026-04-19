# HavenDevTools

**Version 1.0.11** — Developer utilities for mod debugging and game state inspection in Sun Haven. Not intended for typical players; may impact performance.

**1.0.8:** Config file now uses `BepInEx/config/HavenDevTools.cfg` (auto-migrates values from the legacy GUID-named config on first load).

## Features

- **State Inspector** - Real-time game state inspection
- **Debug Console** - Advanced logging and commands
- **Performance Monitor** - Track performance metrics
- **Hot Reload** - Reload mods without restarting (experimental)

## Usage

This mod is intended for mod developers. Press the configured hotkey to open the dev tools panel.

## Installation

1. Install BepInEx 5.x
2. Place `HavenDevTools.dll` in `BepInEx/plugins/`
3. Launch the game

## Configuration

Edit `BepInEx/config/HavenDevTools.cfg` to customize settings.

**The Vault (when installed):** `[The Vault]` → **FullVaultInspector** — same as the old “full vault debug” mode (zeros in tabs, Debug dump tab, full HUD). You can also toggle it in the dev window under **Azrael's Mods → The Vault**.

## Note

This is a developer tool and may impact performance. Not recommended for normal gameplay.

## Links

- [Documentation](https://azraelgodking.github.io/SunhavenMod/HavenDevTools/HavenDevTools.html)
- [Report Bugs on Discord](https://discord.gg/Vwh2y7qMXv)
