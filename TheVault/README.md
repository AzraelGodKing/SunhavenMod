# The Vault

A Sun Haven mod that provides secure, automatic currency storage with a persistent HUD display.

## Features

### Core Features
- **Auto-Deposit**: Tokens and keys automatically go to your vault when picked up
- **Persistent HUD Bar**: Always-visible display showing all your vault currencies with actual game icons
- **Native Game Icons**: Loads real item sprites directly from the game's database
- **Compact Number Format**: Large values display as K/M format (1K, 50K, 1.5M)
- **Seamless Shopping**: Shops automatically use your vault balance - no withdrawing needed
- **Door & Chest Integration**: Keys work directly from your vault for locked doors and chests
- **Per-Character Vaults**: Each character has their own separate vault
- **Steam Deck Ready**: F8 alternate key with no modifier required

### Supported Currencies

**Seasonal Tokens**
- Spring Token, Summer Token, Fall Token, Winter Token

**Keys**
- Copper Key, Iron Key, Adamant Key, Mithril Key, Sunite Key, Glorite Key, King's Lost Mine Key

**Special**
- Community Token, Doubloon, Black Bottle Cap, Red Carnival Ticket, Candy Corn Pieces, Mana Shard

## Controls

| Action | Key |
|--------|-----|
| Open/Close Vault | `Ctrl + V` |
| Open/Close Vault (Steam Deck) | `F8` |
| Close Vault | `Escape` |
| Toggle HUD | In-game config |

## Installation

1. Install [BepInEx 5.x](https://github.com/BepInEx/BepInEx) for Sun Haven
2. Download TheVault.dll from releases
3. Copy `TheVault.dll` to `Sun Haven/BepInEx/plugins/TheVault/`
4. Launch the game

## Configuration

After first launch, edit the config file at:
`Sun Haven/BepInEx/config/com.azraelgodking.thevault.cfg`

| Setting | Default | Description |
|---------|---------|-------------|
| ToggleKey | V | Key to open/close vault UI |
| RequireCtrlModifier | true | Require Ctrl with toggle key |
| AltToggleKey | F8 | Alternative key (no modifier) |
| EnableHUD | true | Show persistent HUD bar |
| HUDPosition | TopLeft | HUD screen position |
| EnableAutoSave | true | Auto-save vault data |
| AutoSaveInterval | 300 | Auto-save interval (seconds) |

## How It Works

- **Pickup Interception**: Currencies are intercepted after pickup and deposited to your vault
- **Shop Integration**: Shops check your vault balance when purchasing with tokens/keys
- **Key Integration**: Locked doors and chests automatically consume keys from your vault
- **Save System**: Vault data auto-saves every 5 minutes and on game save

## Changelog

### v2.0.3
- **Performance Fix**: Fixed lag when picking up vault items by adding reflection caching and item name caching

### v2.0.2
- **UI Fix**: Fixed VaultUI not opening (was opening then immediately closing due to double-toggle bug)

## License

Feel free to use, modify, and distribute this mod.

## Credits

- Created by AzraelGodKing
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [Harmony](https://github.com/pardeike/Harmony)
