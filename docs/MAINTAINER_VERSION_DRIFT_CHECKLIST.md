# Maintainer Version Drift Checklist

Use this checklist before any release prep, documentation refresh, or compatibility pass.

## Global Verification

- Confirm each mod's current version in `docs/versions.json`.
- Verify each mod's version constant (`PluginInfo.cs` or inline `PLUGIN_VERSION`) matches `docs/versions.json`.
- Verify each Thunderstore manifest version matches `docs/versions.json`.
- Verify root `README.md` mod table and per-mod summary text match current versions and hotkeys.
- Verify each touched mod README has a changelog update entry when behavior changed.
- Verify `builds/README.md` lists the correct required artifact set for each releasable mod.

## Mod-by-Mod File Checklist

### BirthdayReminder

- `BirthdayReminder/PluginInfo.cs`
- `BirthdayReminder/thunderstore/manifest.json`
- `BirthdayReminder/thunderstore/README.md`

### FasterRaces

- `FasterRaces/Plugin.cs`
- `FasterRaces/thunderstore/manifest.json`
- `FasterRaces/thunderstore/README.md`

### HavensAlmanac

- `HavensAlmanac/PluginInfo.cs`
- `HavensAlmanac/thunderstore/manifest.json`
- `HavensAlmanac/thunderstore/README.md`

### HavensBirthright

- `HavensBirthright/Plugin.cs`
- `HavensBirthright/thunderstore/manifest.json`
- `HavensBirthright/README.md`
- `HavensBirthright/thunderstore/README.md`

### SunhavenTodo

- `SunhavenTodo/PluginInfo.cs`
- `SunhavenTodo/Plugin.cs` (hotkey defaults)
- `SunhavenTodo/README.md`
- `SunhavenTodo/thunderstore/manifest.json`
- `SunhavenTodo/thunderstore/README.md`

### SunHavenMuseumUtilityTracker

- `SunHavenMuseumUtilityTracker/Plugin.cs`
- `SunHavenMuseumUtilityTracker/README.md`
- `SunHavenMuseumUtilityTracker/thunderstore/manifest.json`
- `SunHavenMuseumUtilityTracker/thunderstore/README.md`

### SenpaisChest

- `SenpaisChest/PluginInfo.cs`
- `SenpaisChest/SenpaisChest.csproj` (build dependencies)
- `SenpaisChest/thunderstore/manifest.json`
- `SenpaisChest/thunderstore/README.md`

### TheVault

- `TheVault/TheVault.csproj`
- `TheVault/README.md`
- `TheVault/thunderstore/manifest.json`
- `TheVault/thunderstore/README.md`
- `TheVault.Abstractions/TheVault.Abstractions.csproj`

### HavenDevTools

- `HavenDevTools/Plugin.cs`
- `HavenDevTools/thunderstore/manifest.json`
- `HavenDevTools/thunderstore/README.md`

### TrinketFortune

- `TrinketFortune/Plugin.cs`
- `TrinketFortune/thunderstore/manifest.json`
- `TrinketFortune/thunderstore/README.md`

## Final Sanity Pass

- Run `scripts/pre-push-build.ps1 -All` without version bump.
- Ensure no version was incremented unless explicitly requested by the owner.
- Confirm changelog text describes user-facing behavior changes, not only internal refactors.
