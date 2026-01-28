# Sun Haven Museum Utility Tracker (S.M.U.T.)

A Sun Haven mod that helps you track which items you've donated to the museum and which ones you still need.

## Features

### Core Features
- **Donation Tracking**: Mark items as donated or needed across all museum sections
- **Per-Character Saves**: Each character has their own separate donation tracking
- **Progress Statistics**: See completion percentages for sections, bundles, and overall
- **Item Icons**: Displays game item icons when available

### Museum Sections

#### The Hall of Gems
- Precious Gems (Diamond, Ruby, Emerald, Sapphire, etc.)
- Common Minerals (Quartz, Obsidian, Granite, etc.)
- Ore Specimens (Copper, Iron, Gold, Mithril, etc.)
- Magical Crystals (Mana Crystal, Sun Crystal, Moon Crystal, etc.)
- Mana (Mana Drop)

#### The Hall of Culture
- Ancient Artifacts (Coins, Vases, Scrolls, Statues, etc.)
- Elven Heritage (Bow Fragment, Pendant, Tome, Ring, Crown)
- Nelvari Relics (Mask, Totem, Drum, Amulet, Spirit Stone)
- Withergate Remnants (Shards, Medallions, Shadow Orb, Key)
- Fossils (Trilobite, Ammonite, Fern, Bone, Dragon)

#### Aquarium
- Freshwater Fish (Bass, Trout, Salmon, Catfish, etc.)
- Saltwater Fish (Tuna, Mackerel, Swordfish, Marlin, etc.)
- Exotic Fish (Angelfish, Clownfish, Pufferfish, Seahorse, etc.)
- Legendary Sea Creatures (Ghost Fish, Void Fish, Golden Koi, etc.)
- Shellfish & Crustaceans (Crab, Lobster, Nautilus, Giant Squid)

## Controls

| Action | Key |
|--------|-----|
| Open/Close Tracker | `Ctrl+C` |
| Close Tracker | `Escape` |
| Toggle Item Donated | Click checkbox |
| Expand/Collapse Bundle | Click bundle header |

## Installation

1. Install [BepInEx 5.x](https://github.com/BepInEx/BepInEx) for Sun Haven
2. Download SunHavenMuseumUtilityTracker.dll from releases
3. Copy to `Sun Haven/BepInEx/plugins/SunHavenMuseumUtilityTracker/`
4. Launch the game

## Configuration

After first launch, edit the config file at:
`Sun Haven/BepInEx/config/com.azraelgodking.museumtracker.cfg`

| Setting | Default | Description |
|---------|---------|-------------|
| ToggleKey | C | Key to open/close tracker UI |
| RequireCtrl | true | Require Ctrl key with toggle key |

## UI Features

- **Section Tabs**: Switch between Hall of Gems, Hall of Culture, and Aquarium
- **Bundle Expansion**: Click bundles to show/hide their items
- **Filter Toggle**: Show only needed items to focus on what's missing
- **Progress Bar**: Visual progress for current section
- **Rarity Colors**: Items are color-coded by rarity (Common, Uncommon, Rare, Epic, Legendary)
- **Status Icons**: Checkmark for donated, circle for needed

## Save Location

Donation data is saved per-character at:
`Sun Haven/BepInEx/config/SunHavenMuseumUtilityTracker/[CharacterName]_donations.json`

## Item Rarity Colors

| Rarity | Color |
|--------|-------|
| Common | Gray |
| Uncommon | Green |
| Rare | Blue |
| Epic | Purple |
| Legendary | Gold |

## License

Feel free to use, modify, and distribute this mod.

## Credits

- Created by AzraelGodKing
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [Harmony](https://github.com/pardeike/Harmony)
