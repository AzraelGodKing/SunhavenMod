# Changelog

## 2026-04-22
- CI: guarded release tag context access in `.github/workflows/build-release-publish.yml` to avoid workflow validation warnings about `github.event.release`.
- Scripts: renamed a local regex capture variable in `scripts/verify-version-consistency.ps1` to avoid assigning to PowerShell automatic variable names flagged by `PSAvoidAssignmentToAutomaticVariable`.
- Scripts: simplified brace-heavy error-string construction in `scripts/verify-version-consistency.ps1` to avoid parser/linter false positives about missing closing `}`.
- CI: upgraded workflow artifact actions to Node 24-ready majors (`actions/upload-artifact@v6`, `actions/download-artifact@v7`) in active and deprecated release workflows.

## 2026-04-21
- Perf: fixed texture leaks in `SenpaisChest/UI/SmartChestUI.cs` and `SunhavenTodo/UI/TodoUI.cs` by destroying old textures before recreating.
- Perf: removed per-frame `GUIStyle` allocations in `SunhavenTodo/UI/TodoUI.cs` by caching wrapped title styles and row style.
- Perf: cached season booleans and mine-scene checks in `HavensBirthright/StatFrameCache.cs`, and switched `HavensBirthright/Patches/StatPatches.cs` to use cached season flags.
- Perf: removed per-call dictionary allocation in `TheVault/Patches/ItemPatches.cs` notification draining and refactored currency prefix parsing through a shared helper.
- Perf: capped `SharedUtilities/IconCache.cs` texture cache size and destroy-evicted textures to prevent unbounded growth.
- CI: promoted `.github/workflows/release-self-hosted-sunhaven-runner.yml` to be the active release pipeline at `.github/workflows/build-release-publish.yml`.
- CI: archived the previous release workflow to `.github/workflows/deprecated/build-release-publish.yml` for rollback reference.
