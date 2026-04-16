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
- `HUD.PositionX` / `HUD.PositionY` (updated when you drag the HUD window)
- `HUD.ToggleKey`
- `Debug.DebugLogging`

## Roadmap (later)

- Richer visual design for the HUD (themed panel, typography, optional uGUI) while keeping the same data.

## Changelog

### Unreleased
- **Build:** Project no longer references Sunhaven Todo or Birthday Reminder DLLs; those integrations call into optional mods via reflection, matching other slim mods (only game + BepInEx + TheVault.Abstractions for Vault types).
- Crop growth forecasting now reads reflected crop stage/timing, quality, and item sell data instead of placeholder values.
- **Fixed:** Harmony now patches real `Wish.Crop` entry points (`SetCropSprite`, `SetMeta`, `Water`, `Grow`) — the previous `UpdateGrowth`/`GrowCrop` targets do not exist in Sun Haven, so no crop data was ever recorded.
- **Fixed:** Projected sell value uses harvest item id from `Crop._cropItem` / `ItemData.id` and looks up `ItemSellInfo.sellPrice` via `ItemInfoDatabase` first (not `Decoration.id` or broken dictionary `out` reflection).
- HUD hidden until a character save is active (main menu / pre-load no longer shows the panel).
- Draggable IMGUI HUD; position saved to `HUD.PositionX` / `HUD.PositionY`.
- HUD now shows only live-backed metrics (removed permanent `n/a` placeholder lines).
- Vault projected-value registration now hooks the Vault-loaded bridge event instead of running too early during plugin `Awake`.
- Crop forecast cache now clears on character load via a `GameSave.LoadCharacter` postfix patch.
- HUD runner now uses shared `PersistentRunnerBase` scene-survival behavior.
