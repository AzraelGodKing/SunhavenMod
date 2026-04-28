# Changelog

## 2026-04-28
### Senpai's Chest
- Hardened smart-chest persistence to avoid overwriting an existing non-empty save file with empty runtime data before a successful character load in the current session.
- Added teardown cleanup for scene/event subscriptions and museum integration hooks so reload cycles do not accumulate duplicate callbacks.
- Reduced unnecessary write pressure by gating UI-triggered saves behind dirty-state checks.
- Moved `[Scan] Next scan in ...` countdown output to optional Debug-level logging behind new config toggle `Debug.EnableScanCountdownDebugLog`.

## 2026-04-21
### Performance
- Fixed UI texture leaks by disposing stale textures before recreating them in chest and todo interfaces.
- Removed recurring runtime allocations in UI and item patch paths by caching styles, scene/season checks, and helper parsing flows.
- Added bounded eviction to `SharedUtilities/IconCache.cs` to prevent unbounded texture cache growth.
