# Crop Optimizer

Crop Optimizer adds field-level harvest forecasting, a lightweight crop HUD, and soft integrations with Sunhaven Todo, Birthday Reminder, and The Vault.

## Features

- Tracks crop growth updates through a single Harmony patch on crop growth.
- HUD summary for tracked crops and projected sell value.
- Optional Sunhaven Todo morning task injection for harvest-ready tiles.
- Optional Birthday reminder integration for produce reservation hints.
- Optional The Vault integration through `TheVault.Abstractions` for projected-value registration.

## Integration Badges

| Integration | Status |
|---|---|
| Haven's Almanac | Compatible |
| Sunhaven Todo | Soft dependency |
| Birthday Reminder | Soft dependency |
| The Vault | Soft dependency |

## Config

`BepInEx/config/CropOptimizer.cfg`

- `Enabled`
- `HUD.Enabled`
- `HUD.Scale`
- `HUD.ToggleKey`
- `Debug.DebugLogging`

## Changelog

### Unreleased
- Crop growth forecasting now reads reflected crop stage/timing, quality, and item sell data instead of placeholder values.
- HUD now shows only live-backed metrics (removed permanent `n/a` placeholder lines).
- Vault projected-value registration now hooks the Vault-loaded bridge event instead of running too early during plugin `Awake`.
- Crop forecast cache now clears on character load via a `GameSave.LoadCharacter` postfix patch.
- HUD runner now uses shared `PersistentRunnerBase` scene-survival behavior.
