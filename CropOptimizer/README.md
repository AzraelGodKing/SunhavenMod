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
