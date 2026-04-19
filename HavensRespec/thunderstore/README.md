# Haven's Respec

Version 1.0.0

Respec your Sun Haven skill trees safely. Adds a styled **Reset** button to every skill tab,
with an optional confirmation dialog, a one-step **Undo** (per profession, session-only),
configurable cost (free / gold / tickets), and hotkeys for reset and undo.

## Features

- Clean-room reset that walks the live skill tree (not a hardcoded node-name list), so it
  survives future Sun Haven updates that add rows, rename nodes, or change columns.
- Confirmation dialog with a per-profession breakdown and cost preview.
- One-step per-profession Undo restores the exact allocation you had before the last reset.
- Cost model: `None` (default), `Gold`, or `Gems` — with configurable cost-per-refunded-point.
- Optional `Shift`-click to skip the confirmation dialog.
- Optional hotkeys for "reset current tab" and "undo current tab".
- Settings live in `BepInEx/config/HavensRespec.cfg` and are fully editable live from the
  in-game **BepInEx Configuration Manager** UI.

## Requirements

- BepInEx 5.4+
- Sun Haven

## Credits

Inspired by the community's need for a maintained skill-reset option. Clean-room implementation
— no code reuse from any prior Sun Haven reset mod.
