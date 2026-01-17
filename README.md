# Haven's Birthright

A Sun Haven mod that adds unique racial bonuses and traits for each playable race.

## Features

Each race receives thematic bonuses that enhance their strengths:

### Human
- **Adaptable**: +10% Experience gain
- **Charismatic**: +15% Relationship point gain
- **Silver Tongue**: 5% Shop discount

### Elf
- **Nature's Touch**: +15% Farming speed
- **Green Thumb**: +20% Crop quality chance
- **Forest Walker**: +25% Foraging find chance
- **Arcane Heritage**: +15% Mana regeneration

### Angel
- **Divine Reservoir**: +20% Maximum mana
- **Holy Light**: +15% Magic damage
- **Blessed Recovery**: +25% Health regeneration
- **Fortune's Favor**: +10% Luck

### Demon
- **Infernal Might**: +20% Melee damage
- **Ruthless**: +15% Critical hit chance
- **Hellforged Vitality**: +15% Maximum health
- **Greed**: +20% Gold drops

### Fire Elemental
- **Burning Fury**: +15% Melee damage
- **Inferno**: +20% Magic damage
- **Wildfire**: +10% Attack speed
- **Scorching Strike**: +15% Critical hit chance

### Water Elemental
- **Tidal Shield**: +20% Defense
- **Healing Waters**: +20% Health regeneration
- **Flowing Spirit**: +25% Mana regeneration
- **Aquatic Kinship**: +20% Fishing luck

### Amari
- **Swift Paws**: +15% Movement speed
- **Predator's Reflexes**: +15% Attack speed
- **Skilled Artisan**: +20% Crafting speed
- **Forest Hunter**: +15% Woodcutting speed

### Naga
- **Aquatic Nature**: +25% Fishing speed
- **Sea's Blessing**: +20% Fishing luck
- **Scaled Hide**: +10% Defense
- **Tidal Magic**: +15% Mana regeneration

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
- Adjust individual bonus percentages for each race
- Show/hide bonus notifications

## Building from Source

### Requirements
- .NET Framework 4.8 SDK
- Visual Studio 2022 or VS Code with C# extension

### Build Steps
1. Open `HavensBirthright.sln` in Visual Studio
2. Ensure the game path in `HavensBirthright.csproj` matches your installation
3. Build the solution (Ctrl+Shift+B)
4. The DLL will be automatically copied to your BepInEx plugins folder

## Development Notes

### Finding Game Methods to Patch

The patch files contain template methods that need to be connected to actual game code:

1. Use [dnSpy](https://github.com/dnSpy/dnSpy) or [ILSpy](https://github.com/icsharpcode/ILSpy) to decompile `Assembly-CSharp.dll`
2. Search for classes related to:
   - Player stats (health, mana, speed)
   - Combat (damage, crits, defense)
   - Skills (farming, mining, fishing, etc.)
   - Economy (shops, relationships)
3. Update the `[HarmonyPatch]` attributes with the correct class and method names
4. Uncomment the patches once configured

### Project Structure

```
HavensBirthright/
├── Plugin.cs              # Main BepInEx plugin entry point
├── Races.cs               # Race and bonus type enums
├── RacialBonusManager.cs  # Manages all racial bonuses
├── RacialConfig.cs        # BepInEx configuration
└── Patches/
    ├── PlayerPatches.cs   # Player stat patches
    ├── CombatPatches.cs   # Combat mechanic patches
    ├── FarmingPatches.cs  # Farming/gathering patches
    ├── EconomyPatches.cs  # Shop/relationship patches
    └── RegenPatches.cs    # Health/mana regen patches
```

## License

Feel free to use, modify, and distribute this mod.

## Credits

- Created by AzraelGodKing
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [Harmony](https://github.com/pardeike/Harmony)
