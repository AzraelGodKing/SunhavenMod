# Trinket Fortune

**Unreleased:** Improved SMUT fallback resilience (retryable binding for soft-dependency startup order), added debug diagnostics for reflection fallback paths, and added clean lifecycle unpatch handling on plugin teardown.

**Version 1.2.0** — Increases the odds of **unowned fishing trinkets** dropping as you complete the museum aquarium. Fishing for that last trinket becomes less frustrating.

**1.0.2:** Config file now uses `BepInEx/config/TrinketFortune.cfg` (auto-migrates values from the legacy GUID-named config on first load). Runtime load safety was hardened so Trinket Fortune no longer requires `HavenDevTools.dll` to be present at startup.

## Features

- **Museum-aware** – As you donate more aquarium trinkets, the drop odds for trinkets you don't own yet increase.
- **Configurable** – Adjust the bonus strength and minimum museum progress threshold.

## Configuration

Edit `BepInEx/config/TrinketFortune.cfg`:

- **Enabled** – Turn the mod on or off (default: true).
- **MuseumProgressBonusPercent** – Bonus to unowned trinket odds per 10% aquarium completion (e.g. 5 = +5% per 10%, default: 5).
- **MinimumMuseumProgress** – No bonus below this progress (0.2 = 20%, default: 0.2).

## Requirements

- BepInEx 5.x (Sun Haven)
- Sun Haven

## Optional

- **Sun Haven Museum Utility Tracker (S.M.U.T.)** – Provides aquarium donation data for missing trinket list.
- **HavenDevTools** – Optional companion for debugging workflows. Trinket Fortune starts and runs normally without it.

## Links

- [Documentation](https://azraelgodking.github.io/SunhavenMod/TrinketFortune/TrinketFortune.html)
- [Report Bugs on Discord](https://discord.gg/Vwh2y7qMXv)
