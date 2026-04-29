# Changelog

## 2026-04-29
### CI / Release
- Hardened Nexus preflight in both release workflows to resolve the expected zip path and fall back to a single detected `dist/*.zip` artifact when naming mismatches occur.
- Updated Nexus upload steps to reuse the resolved preflight zip path so non-Almanac mods do not fail solely on strict filename expectations.
- Removed the Haven's Almanac Nexus skip guard in both active release workflows now that the mod has a live Nexus listing and file group configured.

## 2026-04-28
### Senpai's Chest
- Hardened smart-chest persistence to avoid overwriting an existing non-empty save file with empty runtime data before a successful character load in the current session.
- Added teardown cleanup for scene/event subscriptions and museum integration hooks so reload cycles do not accumulate duplicate callbacks.
- Reduced unnecessary write pressure by gating UI-triggered saves behind dirty-state checks.
- Moved `[Scan] Next scan in ...` countdown output to optional Debug-level logging behind new config toggle `Debug.EnableScanCountdownDebugLog`.

### The Vault
- Reduced duplicate menu transition resets by deduplicating `SaveAndReset` and only triggering menu reset on actual non-menu -> menu transitions.
- Added config event unsubscription during teardown to prevent duplicate config handlers after plugin recreation.
- Reduced false critical lifecycle noise by treating expected menu/quit `OnDisable` as normal lifecycle events.

### Sun Haven Todo
- Hardened lifecycle teardown by unsubscribing scene and manager events on destroy to avoid duplicate handlers after plugin recreation.
- Improved menu-transition behavior by saving dirty data once on gameplay -> menu transitions and resetting per-character runtime patch state.
- Reduced false critical lifecycle noise by treating expected destroy paths as normal teardown events.

### S.M.U.T.
- Reduced duplicate menu resets by triggering character save/reset only on actual gameplay -> menu transitions.
- Hardened plugin teardown by unsubscribing scene handlers and treating menu/quit destroy paths as expected lifecycle events.

### Crop Optimizer
- Improved lifecycle diagnostics by classifying expected quit/menu teardown in plugin destroy logs.
- Replaced silent HUD exception handling with debug logging for character-session detection and config flush edge cases.

### Trinket Fortune
- Hardened SMUT fallback binding with periodic re-resolve attempts so late/soft dependency initialization no longer permanently disables donation-aware biasing.
- Added debug-level diagnostics for reflection fallback failures and lifecycle teardown logging with clean Harmony unpatch on destroy.

### Haven's Birthright
- Added teardown cleanup for config setting-change handlers to avoid duplicate callbacks after plugin recreation.
- Improved lifecycle diagnostics by classifying expected menu/quit destroy paths and keeping unexpected runtime teardown visible.
- Added debug logging for time/season/HP fallback reads used by active ability and synergy runtime checks.

### Faster Races
- Added expected-teardown lifecycle logging and explicit Harmony unpatch cleanup on plugin destroy.
- Switched race-name reflection fallback diagnostics to one-time BepInEx debug logging.

### Haven's Respec
- Added expected-teardown lifecycle diagnostics on plugin shutdown so menu/quit unloads are treated as normal and unexpected runtime teardown remains visible.

### Birthday Reminder
- Added lifecycle teardown cleanup for scene subscriptions and todo integration hooks to prevent duplicate callbacks after reload cycles.
- Improved plugin destroy diagnostics by classifying expected menu/quit teardown and unpatching Harmony on shutdown.
- Added debug-level logging when todo reflection fallbacks fail in cross-mod integration.

### Haven Dev Tools
- Reduced false critical lifecycle noise by classifying expected plugin destroy paths during menu/quit teardown.

### Haven's Almanac
- Added lifecycle teardown cleanup by unsubscribing scene hooks and unpatching Harmony on destroy, with expected/unexpected teardown diagnostics.
- Reset overnight hook state on menu transitions so daily hook rebinding remains clean across reload cycles.

## 2026-04-21
### Performance
- Fixed UI texture leaks by disposing stale textures before recreating them in chest and todo interfaces.
- Removed recurring runtime allocations in UI and item patch paths by caching styles, scene/season checks, and helper parsing flows.
- Added bounded eviction to `SharedUtilities/IconCache.cs` to prevent unbounded texture cache growth.
