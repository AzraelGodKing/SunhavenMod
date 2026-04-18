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
- **Experimental:** HUD hover tooltip near the mouse lists crop item name (when resolvable), guessed water/fertilizer fields from `Wish.Crop` reflection, and growth ETA (live read or cached forecast). Toggle with `HUD.HoverTooltip` / tune `HUD.HoverTooltipMaxWorldDistance` in `CropOptimizer.cfg`. Uses a gameplay-camera fallback when `Camera.main` is unset, ray/plane mouse projection for orthographic 2D (fixes empty tooltips), XY distance to crops, default hover radius 5 world units; tooltip still draws when the summary HUD is hidden (F3).
- **Experimental:** Hover tooltip now also shows the **quality tier** (Normal / Silver / Gold + sell multiplier), **growth stage** (current / total with grown percent when available), **projected sell value** (`~Xg at shop`), **tile coord + farming state** via reflected `Wish.TileManager` (watered / hoed), and **item-panel extras** from `ItemData` (season, regrows, seed, museum / bundle flags — only when present). Falls back to reflected `Wish.Crop` water/fertilizer fields if the tile-state lookup fails.
- **Fixed:** Tooltip now reads the real `Wish.Crop` members confirmed via the one-time member dump: `Fertilized` / `ManaInfused` / `FullyGrown` / `PercentGrown` / `DaysLeft` (all properties, not fields — the previous field-only probes missed them). Fertilizer status is now reported directly from `Wish.Crop.Fertilized` (no more `?`), growth shows true `PercentGrown`, and "Ready now" is reported when `FullyGrown` is true.
- **Tile coord:** Uses `Wish.Crop.Position` (Vector3Int) as the source of truth for the farming tile instead of `transform.position` → `WorldToCell`, which could map crops to the wrong cell on orthographic grids. `TileManager` watered/hoed state now probes that coord and its 3×3 neighborhood.
- **Tile coord (v2):** Diagnostics revealed that neither `Wish.Crop.Position` nor `Grid.WorldToCell` aligned with `TileManager.farmingData` keys on the user's farm (key space and world space are offset by ~25 units on Y). Replaced the primary tile lookup with a **nearest-`farmingData`-key search** from the crop's world position, bounded to 64 world units. Since crops only grow on hoed/watered soil, the closest key is guaranteed to be the crop's real tile regardless of the tilemap's origin/scale transform. `WorldToCell` + `Wish.Crop.Position` remain as fallbacks.
- **Debug:** `[HoverDebug] Tile probe …` now logs the nearest `farmingData` key + its value and up to 3 sample keys, so any remaining coord-system mismatch (e.g. world-int vs cell-int) is visible in one line.
- **Water (sibling-scan fallback):** In-game testing showed `farmingData[tile] = Hoed` for tiles that are visibly watered — water state lives in a sibling structure on `TileManager`, not inside `farmingData`. `ProbeTile` now (a) takes live `IsWatered`/`IsHoed` results as ground truth, and (b) when `IsWatered` returns false, scans every `TileManager` field whose name contains `water` for `IDictionary`/`ICollection` membership of the tile coord (e.g. `wateredTiles` HashSet). Tooltip falls back to the old wording only when neither source reports watered.
- **Water (waterTileMap probe — definitive):** Deeper diagnostics revealed that `TileManager` exposes a dedicated `Tilemap waterTileMap` that owns the wet-soil sprites, and that `TileManager.IsWatered(Vector2Int)` and `wateredTiles` (a short `List<Vector2Int>` of queued waterings) do not match the full wet-soil set. The tooltip now queries `waterTileMap.HasTile(waterTileMap.WorldToCell(crop.transform.position))` directly — the water tilemap's own grid → cell mapping — which is the ground truth the game renders from. Hoed/dry/other states remain via the `farmingData` + `IsHoed` path.
- **Debug (v3):** `[HoverDebug] Tile probe …` now also logs the actual `IsWatered(tile)` / `IsHoed(tile)` / `IsHoedOrWatered(tile)` return values, `farmingData[tile]`, and every `TileManager` member whose name contains `water` (fields with counts / tile-membership inline, plus matching properties and method signatures), so the real water source can be pinned in one hover.
- **Log:** Replaced `HarmonyLib.AccessTools.Field/Property` member probes with silent `Type.GetField/GetProperty` in hover/forecast reflection helpers, so missing field names no longer flood the BepInEx log with HarmonyX Warnings.
- **Debug:** With `Debug.DebugLogging = true`, the first hovered crop logs its `Wish.Crop` public+private fields and properties one time (`[HoverDebug] ...`) so real member names (growth, quality, water, fertilizer) can be pinned down without dnSpy.
- **Build:** Project no longer references Sunhaven Todo or Birthday Reminder DLLs; those integrations call into optional mods via reflection, matching other slim mods (only game + BepInEx + TheVault.Abstractions for Vault types).
- Crop growth forecasting now reads reflected crop stage/timing, quality, and item sell data instead of placeholder values.
- **Fixed:** Harmony now patches real `Wish.Crop` entry points (`SetCropSprite`, `SetMeta`, `Water`, `Grow`) — the previous `UpdateGrowth`/`GrowCrop` targets do not exist in Sun Haven, so no crop data was ever recorded.
- **Fixed:** Projected sell value uses harvest item id from `Crop._cropItem` / nested item id (not `Decoration.id`); lookups prefer `ItemInfoDatabase` / `ItemSellInfo.sellPrice`, then `ItemDatabase`, with `ItemData.sellPrice` read as float where applicable.
- HUD hidden until a character save is active (main menu / pre-load no longer shows the panel).
- Draggable IMGUI HUD; position saved to `HUD.PositionX` / `HUD.PositionY`.
- HUD now shows only live-backed metrics (removed permanent `n/a` placeholder lines).
- Vault projected-value registration now hooks the Vault-loaded bridge event instead of running too early during plugin `Awake`.
- Crop forecast cache now clears on character load via a `GameSave.LoadCharacter` postfix patch.
- HUD runner now uses shared `PersistentRunnerBase` scene-survival behavior.
