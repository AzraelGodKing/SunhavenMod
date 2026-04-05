# Haven's Birthright

**Version 1.4.3** — Unique racial bonuses, active abilities, drawbacks, and conditional synergies for all 12 playable races in Sun Haven. Celestial Bloodlines: Font of Light (Angel) and Soul Harvest (Demon). Correctly resets when switching saves. Optional **[BonusTransfers]** (off by default): copy **passive** table bonuses between races via `TargetRace|SourceRace|BonusType` rules in the main Birthright config.

## Features

- **12 Unique Races** - Each race has distinct gameplay advantages
- **Active Abilities** - Fire and Water Elementals have special triggered abilities
- **Configurable Bonuses** - Adjust values to your preference
- **Balanced Gameplay** - Designed to enhance without breaking the game

## Races

### Core Races
- **Human** - Jack of all trades with XP and relationship bonuses
- **Elf** - Nature's guardian with farming and foraging bonuses

### Celestial Races
- **Angel** - Divine blessed with mana and magic bonuses
- **Demon** - Infernal warrior with melee and critical bonuses

### Elemental Races
- **Fire Elemental** - Blazing fury with combat bonuses + Infernal Forge ability
- **Water Elemental** - Flowing tide with defense and fishing bonuses + Tidal Blessing ability

### Amari Variants
- **Amari Cat** - Feline Grace (movement), Predator's Strike (crit), Keen Senses (foraging), and Nine Lives (passive: chance to survive lethal damage and heal)
- **Amari Dog** - Loyal guardian with friendship bonuses
- **Amari Bird** - Sky dancer with movement bonuses
- **Amari Aquatic** - Water dweller with fishing bonuses
- **Amari Reptile** - Scaled survivor with defense bonuses

### Serpent Race
- **Naga** - Serpent kin with fishing and swimming bonuses

## Installation

1. Install BepInEx 5.x
2. Place `HavensBirthright.dll` in `BepInEx/plugins/HavensBirthright/`
3. Launch the game

## Configuration

Edit `BepInEx/config/com.azraelgodking.havensbirthright.cfg` to customize bonuses.

### Bonus transfers (optional)

Section **`[BonusTransfers]`**:

- **`EnableBonusTransfers`** — default **false**. When true, **`Rules`** apply.
- **`Rules`** — semicolon-separated entries: **`TargetRace|SourceRace|BonusType`**. Each rule adds the **same passive percentage** the source race gets for that bonus type (from the usual Birthright tables in config). Does **not** copy active abilities, drawbacks, or conditional synergies.

**Example** (`com.azraelgodking.havensbirthright.cfg`):

```ini
[BonusTransfers]
EnableBonusTransfers = true
Rules = Human|WaterElemental|Defense; Elf|Human|ExperienceGain
```

That gives **Humans** Water Elemental’s **Defense** passive (same % as `[Water Elemental]` → Defense in your cfg) and gives **Elves** the **ExperienceGain** bonus Humans get from `[Human]`. Add more grants by chaining with `;` (no line breaks inside the value).

## Links

- [Nexus Mods](https://www.nexusmods.com/sunhaven/mods/487)
- [Documentation](https://azraelgodking.github.io/SunhavenMod/RacialBonuses/RacialBonuses.html)
- [Report Bugs on Discord](https://discord.gg/Vwh2y7qMXv)
