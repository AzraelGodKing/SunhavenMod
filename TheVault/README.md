# The Vault

> ## ⚠ YOUR VAULT SAVE — READ THIS
>
> **Normal updates to this mod are not meant to erase your vault.** Your per-character balances are stored in this mod’s encrypted vault data; we treat **preserving that data** as a requirement, and when the save format changes we intend to **migrate** old data forward instead of resetting it.
>
> **If a release ever cannot keep compatibility** (unexpected game update, unavoidable format break, etc.), maintainers follow **[SAVE_COMPATIBILITY.md](./SAVE_COMPATIBILITY.md)**: a **large warning at the top of this README**, the same message in **Thunderstore/Nexus** descriptions (via `docs/versions.json`), **changelog** backup steps, and an **in-game + BepInEx log** warning when needed. Until you see that warning, you should **not** need to delete saves or expect a wiped vault from a routine mod update.
>

> ## ⛔ BREAKING — VAULT SAVE MAY NOT CARRY FORWARD
>
> **This version cannot preserve your existing vault automatically.** Before launching the game with this build:
>
> Best to manually empty the vault before upgrading to the new version. 
>
> There is no way to restore old vault files at the moment.
>

**The Vault 3** is a Sun Haven BepInEx mod: per-character vault, encrypted saves, HUD, auto-deposit, and shop/door integration. Ship **`TheVault.dll`** and **`TheVault.Abstractions.dll`** in `BepInEx/plugins/TheVault`. Sources and the supported build live in this folder (**`TheVault.csproj`** only).

## Features

- Per-character encrypted vault (`VaultSaveSystem`, `VaultManager`)
- Auto-deposit, shop hooks, door/key checks, on-screen HUD
- Secret Gifts tracking in `SecretGifts.dat`
- **Custom** tab and runtime currency registration for other mods (`VaultIntegration` / `TheVault.Abstractions`)

## Usage

- **Main window:** IMGUI vault UI (**Ctrl+V** or **F8**). The HUD hides while the window is open.
- **F7:** Toggle HUD.
- Select a row, enter a quantity, then **Withdraw** (vault → bag) or **Deposit** (bag → vault). Status text under the buttons reports success or errors (not only the BepInEx log).
- **`[HUD] Density`:** **Normal**, **Compact**, or **Minimal**. **`[HUD] CompactMode`** still narrows the bar when Density is **Normal**. The in-game **Settings** tab shows HUD density and the compact toggle.

## Full vault inspector

Enable **`[The Vault] FullVaultInspector = true`** in `BepInEx/config/com.azraelgodking.havendevtools.cfg`, or in **Haven Dev Tools** (F11) → **Azrael's Mods** → **The Vault**. The vault **Settings** tab shows whether this mode is on (it is not stored in `thevault.cfg`).

When enabled:

- Tabs list **every defined currency**, including **0** balance.
- A **Debug** tab shows a **text dump** of all vault stores (seasonal, community, keys, tickets, orbs, custom).
- The **HUD** lists **all** seasonal / key / special slots and **all** custom ids (including zeros).

## Custom currencies (other mods)

The vault UI includes a **Custom** tab for currencies registered at runtime. From another BepInEx plugin:

1. Add a dependency so your mod loads after The Vault: `[BepInDependency("com.azraelgodking.thevault")]`.
2. After the vault is ready (`TheVault.Api.VaultIntegration.IsVaultReady`), call `VaultIntegration.RegisterCustomCurrency(id, displayName, gameItemId, enableAutoDeposit)`.
   - `id`: short key, lowercase letters, digits, and underscore only (max 64). Do **not** include the `custom_` prefix; storage uses `custom_{id}`.
   - `gameItemId`: Sun Haven item id for withdraw and optional auto-deposit mapping; use `-1` for a balance with no inventory item.
   - `enableAutoDeposit`: only applies when `gameItemId > 0` and the player turns on the row toggle in the vault UI (same behavior as built-in currencies).

**Orphan balances:** If a mod registers a custom currency and is later removed, saved totals can remain under `CustomCurrencies` in the vault file. They still **load**; enable **full vault inspector** (Haven Dev Tools → The Vault) and open the **Debug** tab to see raw keys. Re-register the same short id if you want them back in the normal **Custom** tab with labels.

**Hooks for other mods:** `VaultManager` exposes `OnVaultLoaded` and `OnCurrencyChanged` (full currency id + old/new amounts). Subscribe via `Plugin.GetVaultManager()` after the vault exists.

**Thin API assembly:** reference **`TheVault.Abstractions.dll`** (same folder as `TheVault.dll`). At runtime use **`TheVault.Modding.VaultModApiBridge.Instance`** (`IVaultModApi`: `IsVaultReady`, `RegisterCustomCurrency`). This mirrors `VaultIntegration` without referencing the game or the main plugin assembly.

## Build

- `dotnet build TheVault/TheVault.csproj` → `TheVault.dll` and **`TheVault.Abstractions.dll`** copied next to each other under `BepInEx/plugins/TheVault` (see the csproj `CopyToPlugins` targets). **Distribute both** to players; dependent mods may reference only the abstractions assembly.
- Optional unit tests (no Unity): `dotnet test TheVault/Tests/TheVault.Tests.csproj -c Release`

## Links

- [Nexus Mods](https://www.nexusmods.com/sunhaven/mods/488)
- [Discord — bugs & discussion](https://discord.gg/Vwh2y7qMXv)

## Version

**Released version is `3.0.0`** (BepInEx, Thunderstore, Nexus) **until it is explicitly bumped** — not every internal change warrants a version bump.

## Changelog

### 3.0.0

**The Vault 3** updates the mod for current Sun Haven builds: one plugin assembly, tighter game integration (typed `Wish.*` / game types where it matters), HUD and vault UI polish, safer inventory/vault bookkeeping, pickup toasts that show **`NN× ItemName`**, and a **Custom** tab plus **`TheVault.Abstractions`** for other mods. Full vault inspection is controlled from **Haven Dev Tools**, not `thevault.cfg`.

Highlights:

- **Vault UI & HUD:** IMGUI vault window; HUD density presets, improved layout and scaling, less per-frame churn; width-capped, multi-row HUD with stable currency order.
- **Integration:** Harmony patches aligned with real game types (`GameSave`, `Shop`, `Inventory`, `Player`, etc.); character/vault identity from typed save data; shop and inventory paths use **`Wish.*`** APIs on the fast path where applicable.
- **Reliability:** `RemoveItem` vault deduction only when a valid pre-remove count is known; suppressed during auto-deposit/sweep paths; cached reflection for inventory removal to cut noise and cost.
- **Modding:** `VaultIntegration.RegisterCustomCurrency`, `OnVaultLoaded` / `OnCurrencyChanged`, and **`TheVault.Abstractions.dll`** for dependents.
- **Tooling & releases:** Thunderstore package includes icon, readme, both DLLs; CI Nexus upload uses **`Nexus-Mods/upload-action@v1.0.0-beta.3`** (API `data` envelope). **`Version` / `AssemblyVersion`** in **`TheVault.csproj`** and **`TheVault.Abstractions.csproj`** match **`docs/versions.json`**.
- **Maintenance:** `VaultDataSanitizer` after load; shared `VaultCurrencyIds`; **NUnit** tests under `Tests/`.
- **Save compatibility policy:** **[SAVE_COMPATIBILITY.md](./SAVE_COMPATIBILITY.md)** documents the maintainer checklist (README callout, `docs/versions.json` `vault_save_breaking` / `vault_breaking_banner`, Thunderstore readme, changelog backup steps, **`VaultSaveCompatibility.ThisReleaseMayBreakVaultSaves`**). Release CI prepends the banner to store descriptions and syncs **`manifest.json`** `description`.

**Project layout:** Everything lives under [`TheVault/`](./) (`Plugin.cs`, `Patches/`, `Vault/`, `UI/`). Output is **`TheVault.dll`** with GUID `com.azraelgodking.thevault`, plus **`TheVault.Abstractions.dll`**.

**Build note:** Compile-time reference to `Sirenix.Serialization.dll` from `Sun Haven_Data/Managed` (notification-related UI bases). Runtime needs BepInEx and the game as usual.
