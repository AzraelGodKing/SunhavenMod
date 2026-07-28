# The Vault - Changelog

## Unreleased

## Version 4.1.1

- **Fix:** `HasEnough` combines bag + vault; deposit/auto-deposit rolls back inventory when vault credit fails; menu `ResetState` clears auto-deposit guard flags.
- **Fix:** Load restores from `.backup` when the primary save file is missing or unreadable (crash during atomic save), instead of silently starting an empty vault. Tries candidates in order canonical → backup → legacy; quarantines corrupt primary when backup/legacy loads; resets re-encryption flag per candidate.
- **Fix (pt-BR):** Re-applied native-speaker Portuguese (Brazil) strings with correct UTF-8 encoding.
- **Fix:** `GetVaultAmount` supports registered `custom_` currencies in inventory merge paths.
- **Fix:** Shop currency deduction runs only after prefix-approved vault purchases.
- **Fix:** HUD recreated when destroyed without recreating the full vault window.
- **Fix:** Encryption mode and secret-gift hash logs downgraded to debug.

## Version 4.0.1

- **Fix:** Vault UI and HUD icons display again — `VaultUI`/`VaultHUD` now resolve `IconCache` to `SunhavenMods.Shared.IconCache` (C# namespace shadowing had them reading the removed legacy cache while load/register used shared). Removed duplicate `TheVault/UI/IconCache.cs`.
- **Performance:** Icon loading on character enter uses shared `SunhavenMods.Shared.IconCache` only (no duplicate legacy cache load). Vault HUD rebuilds row layout only when currency totals or HUD scale/density change. Character-context sync fallback runs ~0.75s instead of every frame; repeated `CurrentCharacter` fallback warning logs once per session.

## Version 2.0.0
- Added item icons from the game to Vault UI and HUD
- Icons are loaded from the game's database and cached for performance
- Fallback to text abbreviations while icons are loading or unavailable

## Version 1.0.4
- Added persistent HUD bar showing vault currency totals (toggle with F7)
- HUD displays abbreviated currency names with counts, hides when main vault is open
- Configurable HUD position (TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight)
- Moved Community Tokens to Special category for better organization
- Added input blocking when Vault or Debug UI is open (prevents game interaction)
- Added Steam Deck support with F8 as alternative keybind (no Ctrl required)

## Version 1.0.3
- Added Candy Corn Pieces support
- Added Mana Shard support
- Renamed "Tickets" category to "Special" to better reflect the variety of currencies
- All special currencies now use unified "special_" prefix internally

## Version 1.0.2
- Added Red Carnival Ticket support
- Renamed "Pirate" category to "Tickets" to accommodate all ticket types
- Improved Debug UI with dropdown menus for all vault currencies
- Debug UI now includes quick amount buttons for easier testing

## Version 1.0.1
- Fixed item duplication bug when withdrawing currencies from vault
- Added support for Pirate Currencies:
  - Doubloon
  - Black Bottle Cap

## Version 1.0.0
- Initial release
- Auto-deposit system for currencies when picked up
- Seamless shop integration (vault currencies used automatically)
- Door and chest integration for keys
- Per-character vault storage
- Auto-save every 5 minutes
- Supported currencies:
  - Seasonal Tokens (Spring, Summer, Fall, Winter)
  - Community Tokens
  - Keys (Copper, Iron, Adamant, Mithril, Sunite, Glorite, King's Lost Mine)
- Vault UI with Ctrl+V hotkey
- Quick withdraw buttons (-1, -5, -10)
- Per-currency auto-deposit toggles
