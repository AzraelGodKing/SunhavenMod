# Mod Overhaul Execution Plan

This plan enforces branch isolation:
- each mod overhaul in its own branch
- cross-mod/shared changes in one branch

## Branch Map

- `overhaul/cross-mod-foundation`
- `overhaul/senpaischest-hardening`
- `overhaul/thevault-stability`
- `overhaul/sunhaventodo-lifecycle`
- `overhaul/smut-dataflow`
- `overhaul/birthdayreminder-sequencing`
- `overhaul/havensbirthright-racedetection`
- `overhaul/havensalmanac-integrationlayer`
- `overhaul/havendevtools-modularization`
- `overhaul/cropoptimizer-resilience`
- `overhaul/trinketfortune-fallbacks`
- `overhaul/fasterraces-compat`
- `overhaul/havensrespec-transactional`

## Work Ownership Rules

- Put only shared utilities/contracts/tooling in `overhaul/cross-mod-foundation`.
- Put only mod-local code and docs updates in each mod branch.
- If a mod needs cross-mod changes, merge `overhaul/cross-mod-foundation` first.
- Keep PRs scoped to one concern and one branch.

## Phase Order

### Phase 1: Cross-mod foundation
Branch: `overhaul/cross-mod-foundation`

- Define lifecycle contract template:
  - explicit subscribe/unsubscribe symmetry
  - deterministic load/save ownership
- Define logging contract:
  - high-volume loop logs must be debug-gated
  - startup health summary line format
- Define diagnostics contract:
  - consistent "health snapshot" dump surface
- Apply only shared-doc and shared-utility updates here.

### Phase 2: Stability/hardening branches

- `overhaul/senpaischest-hardening` (fix/hardening only)
- `overhaul/thevault-stability`
- `overhaul/sunhaventodo-lifecycle`
- `overhaul/smut-dataflow`
- `overhaul/birthdayreminder-sequencing`
- `overhaul/havensbirthright-racedetection`

Goal: eliminate data-loss risk, lifecycle duplication, and scene/character-switch fragility.

### Phase 3: Maintainability/perf branches

- `overhaul/havensalmanac-integrationlayer`
- `overhaul/havendevtools-modularization`
- `overhaul/cropoptimizer-resilience`
- `overhaul/trinketfortune-fallbacks`
- `overhaul/fasterraces-compat`
- `overhaul/havensrespec-transactional`

Goal: improve maintainability, compatibility posture, and runtime predictability.

### Phase 4: Release/documentation polish

- Update player-facing `CHANGELOG.md` with user-visible outcomes.
- Record internal-only implementation notes in `MAINTAINER_CHANGELOG.md`.
- Run full consistency checks before release workflow.

## Per-Branch Acceptance Checklist

- Builds for touched mod(s) pass locally.
- No new lint issues in touched files.
- Lifecycle teardown and character/scene transitions tested.
- Logging remains readable in default config (debug noise behind toggles).
- Changelog entries follow player-facing vs maintainer split policy.

## Deferred Backlog (Parked)

- Standalone ideas parked:
  - NPC Finder Overlay
  - Loadout Manager
  - Route Planner Board
- Add-ons parked:
  - Haven's Almanac: Shipping forecast + 5-day average
  - Crop Optimizer: Farm heatmap
  - Sunhaven Todo: Festival prep helper

