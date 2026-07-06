# Mod review — 2026-07-02

Mod-code audit: new mod ideas, concrete fixes, and cross-mod compatibility. Findings below were spot-verified against the current tree; file:line citations point at the code in question.

## 1. New mod ideas

1. **Cellar/Preserve Tracker** — Track fermentation/aging items (wine, cheese, preserves) the way `CropOptimizer` tracks crop growth: a Harmony patch mirroring `CropGrowthPatch.cs`, plus a `CropHudView`-style overlay showing days remaining and a "ready" ping. Natural fit given the game's time-gated production objects and a proven pattern to copy. Medium complexity — mainly reflection to find the preserve/keg component fields.
2. **Relationship Dashboard** — A HUD/window (same IMGUI style as `GiftingAssistant/UI/GiftingWindow.cs`) aggregating NPC friendship levels, upcoming birthdays (via `BirthdayReminder`), and favorite-gift data (`BirthdayReminder/Data/FavoriteGiftStore.cs`) into one "who haven't I talked to this week" view. Could ship as a new `HavensAlmanac` panel via the existing `IModDataProvider` pattern rather than a standalone mod. Low-medium risk — read-only aggregation, no save-file ownership.
3. **Fishing Log / Tackle Advisor** — Companion to `TrinketFortune`: track per-fish catch counts/rarest catches, using the same `DonationHelper`-style soft dependency to SMUT for "needed for museum" prioritization, and suggest bait/rod loadout by time-of-day/weather. Additive to existing hooks (`TrinketFortune/Patches/FishingTrinketPatches.cs`, `DonationHelper.cs`) rather than new patch surface. Medium complexity/risk — needs a new save file, must follow `docs/ATOMIC_SAVE_POLICY.md`.
