# The Vault

Sun Haven vault mod (per-character encrypted vault, IMGUI vault + HUD, auto-deposit, shop hooks). **Released version: 3.0.0.**

## Installation

- Install **BepInEx 5.x** for Sun Haven.
- Install this package with **r2modman / Thunderstore Mod Manager**, or unzip into your game folder so both DLLs end up in the same mod folder under `BepInEx/plugins/` (the manager usually creates something like `BepInEx/plugins/AzraelGodKing-TheVault/`).
- **Ship both** `TheVault.dll` and **`TheVault.Abstractions.dll`** together—the abstractions assembly must sit next to the main plugin.

## Usage

- **Ctrl+V** or **F8**: open/close vault UI  
- **F7**: toggle HUD  
- **Debug inspector** (all currencies / Debug tab): enable **`[The Vault] FullVaultInspector`** in `BepInEx/config/com.azraelgodking.havendevtools.cfg`, or **Haven Dev Tools** (F11) → **Azrael's Mods** → **The Vault**.

## Vault save data

Routine updates aim **not** to wipe your vault. If a build **cannot** keep compatibility, maintainers publish a clear warning at the **top of the GitHub README**, in **Thunderstore/Nexus** text, the **changelog** (what to back up), and an **in-game / log** notice when needed — see **`TheVault/SAVE_COMPATIBILITY.md`** in the repo.

## For mod authors

Reference **`TheVault.Abstractions.dll`** and use `TheVault.Modding.VaultModApiBridge.Instance` after the vault is ready. Full details: [repository README](https://github.com/AzraelGodKing/SunhavenMod/blob/main/TheVault/README.md).

## Links

- [Nexus Mods](https://www.nexusmods.com/sunhaven/mods/488)
- [Report Bugs on Discord](https://discord.gg/Vwh2y7qMXv)
