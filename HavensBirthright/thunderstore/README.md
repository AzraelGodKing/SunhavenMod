# Haven's Birthright

**Unreleased:** Lifecycle hardening pass — config setting-change handlers now unsubscribe on teardown to avoid duplicate callbacks after reload cycles; added expected-teardown lifecycle logging and extra debug diagnostics for runtime time/season/HP fallback reads.

**Version 2.2.0** — Unique racial bonuses, active abilities, drawbacks, and conditional synergies for all 12 playable races in Sun Haven. Celestial Bloodlines: Font of Light (Angel) and Soul Harvest (Demon). Correctly resets when switching saves. Optional **[BonusTransfers]** (off by default): copy **passive** table bonuses between races via `TargetRace|SourceRace|BonusType` rules in the main Birthright config.
