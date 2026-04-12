# Faster Races

**Version 1.1.3** — Adds a **configurable movement speed bonus** for your character. When used with **Haven's Birthright**, this mod suppresses Haven's Birthright movement speed bonuses only while Faster Races speed bonus is active, so you do not get double speed.

**1.1.3:** Config file now uses `BepInEx/config/FasterRaces.cfg` (auto-migrates values from the legacy GUID-named config on first load).

## Features

- **Configurable speed** – Set the bonus percentage in the config (default +25%).
- **Compatible with Haven's Birthright** – If both mods are installed and Faster Races speed bonus is active, Haven's Birthright will not apply its own movement speed bonuses (e.g. Amari Cat, Amari Bird Tailwind). Speed penalties from Haven's Birthright (e.g. Amari Dog, Naga) still apply.

## Configuration

Edit `BepInEx/config/FasterRaces.cfg`:

- **Enabled** – Turn the speed bonus on or off (default: true).
- **SpeedBonusPercent** – Percentage added to movement speed (e.g. 25 = +25%, default: 25).

## Requirements

- BepInEx 5.x (Sun Haven)
- Sun Haven

## Optional

- **Haven's Birthright** – Works best together; HB will skip its speed buffs when Faster Races is loaded.

## Links

- [Documentation](https://azraelgodking.github.io/SunhavenMod/FasterRaces/FasterRaces.html)
- [Report Bugs on Discord](https://discord.gg/Vwh2y7qMXv)
