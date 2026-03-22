# Haven's Birthright

A Sun Haven mod that adds unique racial bonuses, active abilities, drawbacks, and conditional synergies for each playable race.

## Features

Each race receives thematic passive bonuses, and select races gain powerful active abilities that can be toggled during gameplay.

### Passive Racial Bonuses

#### Human
- **Adaptable**: +10% Experience gain
- **Charismatic**: +15% Relationship point gain
- **Silver Tongue**: 5% Shop discount

#### Elf
- **Nature's Touch**: +15% Farming speed
- **Green Thumb**: +20% Crop quality chance
- **Forest Walker**: +25% Foraging find chance
- **Arcane Heritage**: +15% Mana regeneration

#### Angel
- **Divine Reservoir**: +20% Maximum mana
- **Holy Light**: +15% Magic damage
- **Blessed Recovery**: +25% Health regeneration
- **Fortune's Favor**: +10% Luck

#### Demon
- **Infernal Might**: +20% Melee damage
- **Ruthless**: +15% Critical hit chance
- **Hellforged Vitality**: +15% Maximum health
- **Greed**: +20% Gold drops

#### Fire Elemental
- **Burning Fury**: +15% Melee damage
- **Inferno**: +20% Magic damage
- **Wildfire**: +10% Attack speed
- **Scorching Strike**: +15% Critical hit chance

#### Water Elemental
- **Tidal Shield**: +20% Defense
- **Healing Waters**: +20% Health regeneration
- **Flowing Spirit**: +25% Mana regeneration
- **Aquatic Kinship**: +20% Fishing luck

#### Amari
- **Swift Paws**: +15% Movement speed
- **Predator's Reflexes**: +15% Attack speed
- **Skilled Artisan**: +20% Crafting speed
- **Forest Hunter**: +15% Woodcutting speed

#### Amari Cat
- **Feline Grace**: +20% Movement speed
- **Predator's Strike**: +15% Critical hit chance
- **Keen Senses**: +15% Foraging chance (find more foragables)
- **Nine Lives** (passive): When taking lethal damage, a configurable percent chance (max 60%) to survive and heal to a percent of max HP instead of dying. Not an active ability—always on when enabled in config.

#### Naga
- **Aquatic Nature**: +25% Fishing speed
- **Sea's Blessing**: +20% Fishing luck
- **Scaled Hide**: +10% Defense
- **Tidal Magic**: +15% Mana regeneration

---

### Active Abilities

Active abilities are powerful race-specific skills. **Infernal Forge, Tidal Blessing, Font of Light, and Soul Harvest** use the in-game toggle: they **start OFF** each session until you press **F9** (configurable) once to turn yours on. Press again to turn off. Other abilities (e.g. Divine Ward, Tailwind) are controlled only by config, not F9.

#### Water Elemental — Tidal Blessing
Automatically waters hoed crop tiles at the cost of HP. Stand on or next to a tilled plot and it will be watered for you.

- Waters tiles within 1 tile of the player
- Costs a configurable percentage of max HP per tile watered (default 3%)
- Won't activate below a safety HP threshold (default 20%)
- Cooldown between activations (default 2 seconds)

#### Fire Elemental — Infernal Forge
Periodically scans your inventory and automatically smelts raw ore into bars, consuming mana in the process.

- Scans inventory every 2 seconds (configurable)
- Requires the same ore-per-bar ratio as the game (default 3 ore = 1 bar)
- **Tiered mana costs** scale by ore rarity:
  - Copper: 1% max mana per bar
  - Iron: 2%
  - Gold: 3%
  - Adamant: 4%
  - Mithril: 5%
  - Sunite: 6%
  - Glorite: 7%
  - Elven Steel: 8%
- Won't activate below a mana safety threshold (default 10%)
- **Blocks all mana regeneration** while active

#### Angel — Font of Light (Celestial Bloodlines)
Periodically restores mana at the cost of gold. **Only affects characters who chose the Angel race.**

- Triggers on a configurable interval (default 45 seconds)
- Restores a percentage of max mana (default 5%) when your mana is below threshold (default 80%)
- Costs gold each time (default 10 gold); does not trigger if you cannot afford it
- Press **F9** to toggle on or off; you will see a notification when the ability is activated or deactivated

#### Demon — Soul Harvest (Celestial Bloodlines)
When you defeat an enemy, gain bonus gold and lose a small amount of HP. **Only affects characters who chose the Demon race.**

- Grants bonus gold per kill (default 15)
- Costs a percentage of max HP per kill (default 1%)
- Press **F9** to toggle on or off; you will see a notification when the ability is activated or deactivated

---

### Drawbacks (Optional)

Racial drawbacks add thematic penalties to balance racial bonuses. **Disabled by default** — enable in config.

| Race | Drawback | Effect |
|------|----------|--------|
| Fire Elemental | Hydrophobic | -20% Fishing speed/luck |
| Water Elemental | Fragile Form | -10% Max HP |
| Angel | Pacifist | -10% Melee damage |
| Demon | Distrusted | -15% Relationship gain |
| Elf | Fragile | -10% Max HP, -5% Defense |
| Amari Cat | Glass Cannon | -15% Max HP |
| Amari Dog | Slow Starter | -10% Movement speed |
| Amari Bird | Hollow Bones | -15% Max HP, -10% Defense |
| Amari Aquatic | Land Slug | -10% Movement speed |
| Amari Reptile | Cold Blooded | -10% Attack speed |
| Naga | Landlocked | -10% Movement speed, -10% Farming speed |

---

### Conditional Synergies

Bonuses that activate based on time of day, season, or HP threshold.

**Time of Day:**
- Angel — Solar Power: +10% Magic damage during daytime
- Demon — Night Stalker: +10% Melee damage during nighttime

**Season:**
- Elf — Spring Awakening: +15% Farming bonus during Spring
- Fire Elemental — Summer's Fury: +10% Combat bonus during Summer
- Water Elemental — Winter's Embrace: +15% Defense/regen bonus during Winter

**Health Threshold:**
- Amari Reptile — Last Stand: +25% Defense when HP below 40%
- Angel — Martyr's Light: 3x Health regen when HP below 20%

---

## Installation

1. Install [BepInEx](https://github.com/BepInEx/BepInEx) for Sun Haven
2. Build the mod or download the release
3. Copy `HavensBirthright.dll` to `Sun Haven/BepInEx/plugins/HavensBirthright/`
4. Launch the game

## Configuration

All bonus values are configurable! After first launch, edit the config file at:
`Sun Haven/BepInEx/config/com.azraelgodking.havensbirthright.cfg`

You can:
- Enable/disable racial bonuses entirely
- Toggle active abilities, drawbacks, and conditional synergies independently
- Adjust individual bonus percentages for each race
- Configure ability toggle keybind (default F9)
- Set HP/mana cost percentages and safety thresholds
- Adjust scan intervals and cooldowns
- **Amari Cat**: Under the `[Amari Cat]` section, configure Nine Lives (EnableNineLives, NineLivesChance, NineLivesHealPercent). Under `[Performance]`, AmariCatReduceCombatStutter can disable attack-speed application for Amari Cat if needed (e.g. stutter with fast weapons).
- **Reload config**: Press F12 (configurable) in-game to reload the config file without restarting the game.

## Changelog

### v1.4.0 — Save-Load Fix
- **Fixed**: Mod now correctly resets when switching to a different save without returning to the main menu. Race detection, ability states, and all caches are cleared on every save load, so racial bonuses and abilities always match the loaded character.
- Config reload key (default F12) can reload config from disk without restarting the game.

### v1.3.x (Amari Cat rework)
- **Amari Cat** passives reworked: **Feline Grace** (movement), **Predator's Strike** (crit), **Keen Senses** (foraging). **Nine Lives** is a passive cheat death (chance to survive lethal damage and heal to % max HP; max 60% chance). Removed Predator's Reflex (no combat attack-speed buff). Optional performance setting to reduce stutter with fast weapons.

### v1.2.3
- Documentation and comment updates.

### v1.2.0 — Elemental Enhancements
- **Tidal Blessing** (Water Elemental): Auto-water hoed tiles near the player at HP cost
- **Infernal Forge** (Fire Elemental): Auto-smelt ore into bars with tiered mana costs
  - Mana costs scale by ore tier (1% copper through 8% elven steel)
  - Blocks mana regeneration while active
- Added ability toggle hotkey (F9) for enabling/disabling active abilities in-game
- Added drawback system (disabled by default)
- Added conditional synergies (time-of-day, season, HP threshold)

### v1.1.0
- Initial release with passive racial bonuses for all races

## License

Feel free to use, modify, and distribute this mod.

## Credits

- Created by AzraelGodKing
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [Harmony](https://github.com/pardeike/Harmony)
