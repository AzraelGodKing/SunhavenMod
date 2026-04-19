# Senpai's Chest

Smart chest automation and sorting rules for Sun Haven.

## Default Behavior

- Configure smart-chest rules by item, category, type, property, or group.
- Supports chest labels and periodic scan-based sorting.
- Optional integration with S.M.U.T. and SunhavenTodo for museum workflows.

## Config

File: `Sun Haven/BepInEx/config/SenpaisChest.cfg`

## Notes

- This README is intentionally short for repository contributors.
- Full player-facing documentation is in `thunderstore/README.md`.

## Changelog

### Unreleased

- **Build:** Soft-dependency references to `SunHavenMuseumUtilityTracker` and `SunhavenTodo` now use `ProjectReference` to those sibling projects instead of `..\builds\...\*.dll`, matching other mods in the repo.
- **Fixed:** Configs now appear in BepInEx Configuration Manager. The mod binds entries to a custom `ConfigFile` (`SenpaisChest.cfg`) rather than the default per-GUID file, and Configuration Manager only scans each plugin's inherited `BaseUnityPlugin.Config` property — so it never saw our entries. `ConfigFileHelper.ReplacePluginConfig` now rewires that inherited property to the custom file via reflection right after Awake, so the live config UI picks up every `Config.Bind(...)` call without changing the config file name or path.
- Reworked chest-label rendering to use a screen overlay anchor so labels stay above chests.
- Hide chest labels while chest UI is open.
- Restrict chest labels to chest item/deco id `10110` only.
- Implement chest-label decoration ID allowlist (`10110` seeded) for easy future expansion.
- Add config-backed `ChestLabels.LabeledChestDecorationIds` list for runtime control of labeled chest IDs.
