# Cross-Mod Compatibility Contract

This document defines compatibility expectations between the Sunhaven mod set.

## Goals

- Keep optional dependencies safe (mods should still run when peers are missing).
- Prevent stacked effects where two mods modify the same stat/feature.
- Keep shared metadata fields and event behavior stable for integrations.

## Contract: FasterRaces <-> HavensBirthright

### Required Behavior

- Haven's Birthright must skip its own movement-speed bonuses only when Faster Races speed bonus is actively enabled.
- If Faster Races is installed but disabled, Haven's Birthright movement-speed bonuses remain active.

### Integration Surface

- Faster Races exposes `Plugin.IsSpeedBonusActive` for compatibility checks.
- Haven's Birthright checks that property first and falls back to conservative behavior if reflection fails.

### Compatibility Rule

- Do not apply both Birthright movement-speed bonuses and Faster Races speed bonus in the same stat request.

## Contract: SMUT <-> SunhavenTodo <-> SenpaisChest

### Shared Data Fields (Todo Item)

- `TodoItem.IconItemId` must store the related game item ID.
- `TodoItem.MuseumDestination` must store hall text shown in UI/HUD.

### Event Expectations

- SMUT `DonationManager.OnDonationsChanged` signals donation-state changes.
- Senpai's Chest integration listens to donation events and completes corresponding todo entries.
- Senpai's Chest only creates museum todos when both SMUT and SunhavenTodo are present.

### Matching Rules

- Donation checks by game item ID must treat duplicate appearances across bundles consistently.
- Metadata writes between Senpai's Chest and SunhavenTodo should use typed properties, not reflection, when both mods share compile-time references.

## Contract: Optional Dependency Behavior

- Missing peer mods should disable only integration features, never core mod functionality.
- Integrations should log one clear startup message indicating whether compatibility mode is active.
- Runtime integration errors should degrade gracefully and avoid interrupting gameplay loops.

## Release Checklist Addendum

- Verify compatibility paths listed here whenever changing:
  - movement speed stat patches,
  - donation tracking APIs,
  - todo metadata model fields,
  - soft dependency GUIDs.
