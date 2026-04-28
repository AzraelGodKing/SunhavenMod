# Faster Races

Configurable movement speed bonus for Sun Haven.

## Default Behavior

- Adds a configurable speed bonus (default `+25%`).
- Applies only when `Enabled = true` in `FasterRaces.cfg`.
- Integrates with Haven's Birthright to avoid double speed stacking.

## Config

File: `Sun Haven/BepInEx/config/FasterRaces.cfg`

- `Enabled` (default `true`)
- `SpeedBonusPercent` (default `25`, clamped to `0`-`300`)

## Notes

- This README is intentionally short for repo maintainers.
- Player-facing details live in `thunderstore/README.md`.

## Changelog

### Unreleased

- **Build:** Added missing `using System;` in `Plugin.cs` so `Exception` in the race-name reflection guard compiles cleanly.
- **Lifecycle:** Added expected-teardown `OnDestroy` logging and Harmony cleanup for plugin unload/quit paths.
- **Debug:** Replaced `System.Diagnostics.Debug.WriteLine` reflection fallback output with one-time BepInEx debug logging.
