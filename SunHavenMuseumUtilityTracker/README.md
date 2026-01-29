# Sun Haven Museum Utility Tracker (S.M.U.T.)

A Sun Haven mod that helps you track which items you've donated to the museum and which ones you still need.

## Features

### Core Features
- **Manual Donation Tracking**: Mark items as donated or needed across all museum sections
- **Per-Character Saves**: Each character has their own separate donation tracking
- **Progress Statistics**: See completion percentages for sections, bundles, and overall
- **Item Icons**: Displays game item icons for easy identification
- **Search & Filter**: Search for specific items or filter to show only needed items

### Important Note on Tracking

**This mod uses manual tracking only.** Sun Haven's game code only tracks bundle completion (e.g., "Golden Bundle complete"), not individual item donations. Because the game doesn't store which specific items you've donated within incomplete bundles, automatic tracking of individual items is not possible.

Use the tracker UI to manually check off items as you donate them. Your progress is saved automatically per character.

### Museum Sections

#### The Hall of Gems
- Mana Bundle - Mana Drops
- Money Bundle - Coins, Mana Orbs, Tickets
- Golden Bundle - Golden Milk, Golden Egg, Golden Wool, and more
- Bars Bundle - Copper, Iron, Adamant, Mithril, Sunite, Gold, Glorite, Elven Steel
- Gems Bundle - Sapphire, Ruby, Amethyst, Diamond, Havenite, Black Diamond, Dizzite
- Nel'Vari Mines Bundle - Mana Shards, Dragon Scales
- Withergate Mines Bundle - Candy Corn, Rock Candy, Jawbreaker, Butterscotch gems

#### The Hall of Culture
- Spring/Summer/Fall/Winter Crops - Seasonal farming produce
- Nel'Vari & Withergate Crops - Region-specific plants
- Flowers Bundle - Honey Flower, Roses, Orchids, Tulips, and more
- Foraging Bundle - Logs, Fruits, Mushrooms, Seaweed, Beach finds
- Exploration Bundle - Rare finds like Phoenix Feather, Griffon Egg, Dragon Fang
- Combat Bundle - Monster trinkets and ancient weapons
- Alchemy Bundle - Potions of all types
- Nel'Vari Temple Bundle - Ancient books and tomes

#### Aquarium
- Fishing Bundle - Treasure items found while fishing
- Spring/Summer/Fall/Winter Fish Tanks - Seasonal fish
- Nel'Vari Fish Tank - Axolotl, Dragon Gulper, Crystal Tetra, and more
- Withergate Fish Tank - Kraken, Vampire Squid, Ghost Fish, and more
- Large Fish Tank - Common fish from all waters

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
3. Copy to `Sun Haven/BepInEx/plugins/`
4. Launch the game

## Configuration

After first launch, edit the config file at:
`Sun Haven/BepInEx/config/com.myleek.sunhavenmuseumutilitytracker.cfg`

| Setting | Default | Description |
|---------|---------|-------------|
| ToggleKey | C | Key to open/close tracker UI |
| RequireCtrl | true | Require Ctrl key with toggle key |

## UI Features

- **Beautiful Dark Theme**: Sun Haven-inspired visual design
- **Section Tabs**: Switch between Hall of Gems, Hall of Culture, and Aquarium
- **Color-Coded Sections**: Each museum section has its own accent color
- **Bundle Expansion**: Click bundles to show/hide their items
- **Search Box**: Quickly find items by name or rarity
- **Filter Toggle**: Show only needed items to focus on what's missing
- **Progress Bars**: Visual progress for current section with percentage
- **Rarity Colors**: Items are color-coded by rarity
- **Status Icons**: "DONATED" for completed, "X" for needed

## Item Rarity Colors

| Rarity | Color |
|--------|-------|
| Common | Gray |
| Uncommon | Green |
| Rare | Blue |
| Epic | Purple |
| Legendary | Gold |

## Save Location

Donation data is saved per-character at:
`Sun Haven/BepInEx/config/SunHavenMuseumUtilityTracker/Saves/[CharacterName]_donations.json`

## Debug Mode (Authorized Users Only)

Press **F10** to access debug tools if you're an authorized user. This includes:
- Sync with game progress (marks all items in COMPLETED bundles)
- View game progress data
- Diagnostic tools

## Version History

- **1.0.0** - Initial release with manual tracking, search, filter, and polished UI

## License

Feel free to use, modify, and distribute this mod.

## Credits

- Created by Myleek
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [Harmony](https://github.com/pardeike/Harmony)
