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

- Reworked chest-label rendering to use a screen overlay anchor so labels stay above chests.
- Hide chest labels while chest UI is open.
- Restrict chest labels to chest item/deco id `10110` only.
- Implement chest-label decoration ID allowlist (`10110` seeded) for easy future expansion.
- Add config-backed `ChestLabels.LabeledChestDecorationIds` list for runtime control of labeled chest IDs.
