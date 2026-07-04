# Mod review — On hold

**Status:** Deferred while foundation work is active (2026-07-04)  
**Active queue:** [`mod-review-foundation-active-2026-07-04.md`](mod-review-foundation-active-2026-07-04.md)

Items below are **not cancelled** — they are parked until SharedUtilities / save / integration consolidation (§1 foundation) reduces duplication and risk for future features.

---

## Shipped since original reviews (remove from backlog when picking up)

| Item | Source | Shipped |
|------|--------|---------|
| Relationship Dashboard | 07-02 #2 | Haven's Almanac panel + Gifting Assistant heart strip |
| Docs site redesign | 07-04 §4 | `docs/index.html` + `index-style.css` harvest/lantern theme |
| FPS counter | 07-04 §2 HavenDevTools | Corner HUD + Perf tab (PR #89) |
| Crop Optimizer off-farm hover perf | — | Scene-scoped cache + hover miss cache |

---

## 07-02 — New mod ideas (on hold)

1. **Cellar/Preserve Tracker** — Fermentation/aging HUD mirroring CropOptimizer (`CropGrowthPatch`, `CropHudView` pattern). Medium complexity.
2. ~~**Relationship Dashboard**~~ — **Shipped** (see above).
3. **Fishing Log / Tackle Advisor** — TrinketFortune companion; per-fish stats, SMUT prioritization, bait/rod suggestions. Needs new save (`ATOMIC_SAVE_POLICY.md`).
4. **Skill Point Planner (Respec Simulator)** — HavensRespec dry-run on top of `SkillResetService` snapshot/undo. Low risk, UI-heavy.
5. **Museum Wishlist Overlay** — “Museum needed” badge in shop/NPC gift UI via SMUT `DonationManager.HasDonatedByGameId`.
6. **Mod Health Dashboard** — HavenDevTools panel aggregating per-mod health snapshots (`MOD_LIFECYCLE_AND_LOGGING_CONTRACT.md` §5).

---

## 07-04 §2 — Feature suggestions for existing mods (on hold)

### The Vault
- Ledger tab (last N deposits/withdrawals with day-stamps)
- Optional shared “family vault” across characters (Steam-scoped identity)

### Senpai's Chest
- Craft-pull missing ingredients from labeled Smart Chests
- Rule presets import/export (JSON snippets)
- Chest-network overview window

### Sun Haven Todo
- Recurring tasks (daily/season resets)
- Game-calendar due dates with overdue highlighting
- Quick-add hotkey without opening full window

### S.M.U.T.
- **Sell-guard** — warn/block shipping museum-needed items *(highest player value in suite)*
- “Where to find” hints per missing item
- Per-hall completion ETA in Almanac panel

### Birthday Reminder
- Season-at-a-glance calendar grid
- Gift-given history log per NPC

### Gifting Assistant
- Auto-roster builder (“keep everyone below X hearts”)
- “Buyable today” badge when loved/liked gift is in open shop stock

### Crop Optimizer
- Season-end warning (crops that cannot mature before season change)
- Sprinkler/water coverage overlay
- Sort HUD by profit-per-day

### Haven's Birthright
- In-game ability loadout panel
- Per-character config profiles via `CharacterFingerprint`

### Haven's Respec
- Respec history log (when, tree, cost) on `ResetSnapshot`
- Dry-run planner (see 07-02 #4)

### Haven's Almanac
- Week-ahead planner (birthdays + crop ETAs + todos)
- Copy briefing to clipboard
- Panel reorder/hide via config

### HavenDevTools
- Harmony patch inspector (patches grouped by mod)
- Save-file viewer for suite JSON/vault files
- Reflection-health panel (pairs with foundation 1.5)

### Trinket Fortune
- Visible pity/progress indicator in DevTools + Almanac provider

### Faster Races
- Separate mount/boat multipliers if game exposes them; otherwise leave stable

---

## 07-04 §3 — New mod ideas beyond 07-02 (on hold)

1. **Haven's Ledger** — Daily gold in/out tracker; Almanac provider; uses shared save store (foundation 1.1).
2. **NPC Compass / “Where's Everybody?”** — NPC map/zone list sorted by pending gifts.
3. **Season Prep Assistant** — Pre-season checklist pushed as Todos (needs foundation 1.2).
4. **Mine Companion** — Per-visit floor/ore tracker + SMUT banner on floor enter.
5. **Ranch Roster** — Barn/coop animal dashboard; Almanac + morning todo.
6. **Suite Config Hub** — Single window for all AzraelGodKing mod BepInEx toggles + per-character profiles.

---

## Revisit triggers

Pick items back up when:

- **1.1 CharacterSaveStore** lands → Ledger, Mine Companion, Ranch Roster, any new per-character save
- **1.2 Todo client** lands → Season Prep, most cross-mod todo features
- **1.3 UiStyle / IconCache** lands → Suite Config Hub, Museum Wishlist overlay, large IMGUI features
- **1.5 ReflectionProbe** lands → Mod Health Dashboard, NPC Compass, reflection-heavy mods

---

## Source documents

- [`mod-review-2026-07-02.md`](mod-review-2026-07-02.md) — original six new-mod ideas
- [`mod-review-2026-07-04.md`](mod-review-2026-07-04.md) — full follow-up (codebase + features + new mods + docs redesign)
