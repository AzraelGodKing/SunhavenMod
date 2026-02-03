# Sun Haven Museum Utility Tracker (S.M.U.T.)

A Sun Haven mod that helps you track which items you've donated to the museum and which ones you still need.

## Features

### Core Features
- **Real-Time Auto-Tracking**: Items are automatically tracked when you donate them to museum bundles in-game
- **Sync with Game**: Click "Sync with Game" to import all completed bundles from your save file
- **Game Progress Display**: Bundle headers show both your tracked progress and the game's recorded donation count `[Game: X]`
- **Per-Character Saves**: Each character has their own separate donation tracking
- **Progress Statistics**: See completion percentages for sections, bundles, and overall
- **Item Icons**: Displays game item icons for easy identification
- **Search & Filter**: Search for specific items or filter to show only needed items

### Smart Tracking

S.M.U.T. now features intelligent tracking that works with the game:

1. **Real-Time Tracking**: When you donate items to museum bundles, they're automatically marked in your tracker
2. **Sync Button**: Import all completed bundles from the game's save data with one click
3. **Dual Progress Display**: See both `(tracked/total)` and `[Game: X]` to compare your tracker with the game's records
4. **Manual Override**: You can still manually check/uncheck items if needed

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
| Open/Close Tracker (Alt) | `F7` (great for Steam Deck) |
| Close Tracker | `Escape` |
| Toggle Item Donated | Click checkbox |
| Expand/Collapse Bundle | Click bundle header |
| Sync with Game | Click "Sync with Game" button |

## Installation

1. Install [BepInEx 5.x](https://github.com/BepInEx/BepInEx) for Sun Haven
2. Download SunHavenMuseumUtilityTracker.dll from releases
3. Copy to `Sun Haven/BepInEx/plugins/`
4. Launch the game

## Configuration

After first launch, edit the config file at:
`Sun Haven/BepInEx/config/com.azraelgodking.sunhavenmuseumutilitytracker.cfg`

| Setting | Default | Description |
|---------|---------|-------------|
| ToggleKey | C | Key to open/close tracker UI |
| RequireCtrl | true | Require Ctrl key with toggle key |
| AltToggleKey | F7 | Alternative toggle key (no modifier required) |

## UI Features

- **Warm Parchment Theme**: Sun Haven-inspired visual design
- **Section Tabs**: Switch between Hall of Gems, Hall of Culture, and Aquarium
- **Color-Coded Sections**: Each museum section has its own accent color
- **Bundle Expansion**: Click bundles to show/hide their items
- **Search Box**: Quickly find items by name or rarity
- **Filter Toggle**: Show only needed items to focus on what's missing
- **Progress Bars**: Visual progress for current section with percentage
- **Rarity Colors**: Items are color-coded by rarity
- **Game Progress**: Shows `[Game: X]` alongside your tracked counts
- **Sync Button**: One-click sync with game's completed bundles

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

## Debug Mode

Press **F10** to access debug tools. This includes:
- Discover game progress key patterns
- View detailed logging
- Force sync operations

## Version History

- **1.1.0** - Added real-time auto-tracking, Sync with Game button, game progress display, performance optimizations
- **1.0.0** - Initial release with manual tracking, search, filter, and polished UI

## Technical Notes

- Real-time tracking works by patching `HungryMonster.SaveInventory()` to detect museum bundle donations
- Game progress is read from `GameSave.GetProgressBoolWorld()` and `GetProgressIntWorld()` (world-level, not character-level)
- The game only stores bundle completion status and donation counts, not which specific items were donated
- Reflection caching ensures minimal performance impact

## License

Feel free to use, modify, and distribute this mod.

## Credits

- Created by AzraelGodking
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [Harmony](https://github.com/pardeike/Harmony)
