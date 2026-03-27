# The Vault

> ## ⚠ YOUR VAULT SAVE — READ THIS
>
> **Normal updates to this mod are not meant to erase your vault.** Your per-character balances are stored in this mod’s encrypted vault data; we treat **preserving that data** as a requirement, and when the save format changes we intend to **migrate** old data forward instead of resetting it.
>
> **If a future release ever cannot keep compatibility** (unexpected game update, unavoidable format break, etc.), we will put a **large warning at the top of this README**, in the **Thunderstore/Nexus** description, and in the **changelog**, and we will tell you **exactly what to back up** before updating. Until you see that warning, you should **not** need to delete saves or expect a wiped vault from a routine mod update.
>
> Full policy and planned work: **[roadmap.md](./roadmap.md)**.

Sun Haven vault mod: **classic implementation** as `TheVault.dll` for `BepInEx/plugins/TheVault`. **Sources live in this folder** (`Plugin.cs`, `Patches/`, `Vault/`, `UI/`). The only supported build is **`TheVault/TheVault.csproj`**.

## Features (classic)

- Per-character encrypted vault (`VaultSaveSystem`, `VaultManager`)
- Auto-deposit, shop hooks, door/key checks, HUD (see legacy changelog for detail)
- Secret Gifts tracking in `SecretGifts.dat`

## Usage

- **Default main window:** Classic **IMGUI** vault window (legacy). The experimental **uGUI** panel has been disabled.
- **Ctrl+V** or **F8**: Open/close vault UI
- **F7**: Toggle HUD
- Select a row, enter a quantity, then **Withdraw** (vault → bag) or **Deposit** (bag → vault). A short **message** under the buttons shows success or what went wrong (not only the BepInEx log).
- **`[HUD] Density`** = **Normal**, **Compact**, or **Minimal** (tightest bar). **`[HUD] CompactMode = true`** still applies when Density is **Normal** (legacy; prefer Density). The in-game **Settings** panel has a **HUD density** row plus the legacy toggle.

## Debug (full vault inspector)

Enable **`[The Vault] FullVaultInspector = true`** in `BepInEx/config/com.azraelgodking.havendevtools.cfg`, or turn it on in **Haven Dev Tools** (F11) → **Azrael's Mods** → **The Vault**. The vault window Settings tab only shows on/off status for this mode (it is no longer stored in `thevault.cfg`).

When enabled:

- Category tabs list **every defined currency**, including **0** balance.
- A **Debug** tab shows a **text dump** of all vault stores (seasonal, community, keys, tickets, orbs, custom).
- The **HUD** shows **all** seasonal / key / special slots and **all** custom ids (including zeros).

## Custom currencies (other mods)

The vault UI includes a **Custom** tab for currencies registered at runtime. From another BepInEx plugin:

1. Add a dependency so your mod loads after The Vault: `[BepInDependency("com.azraelgodking.thevault")]`.
2. After the vault is ready (`TheVault.Api.VaultIntegration.IsVaultReady`), call `VaultIntegration.RegisterCustomCurrency(id, displayName, gameItemId, enableAutoDeposit)`.
   - `id`: short key, lowercase letters, digits, and underscore only (max 64). Do **not** include the `custom_` prefix; storage uses `custom_{id}`.
   - `gameItemId`: Sun Haven item id for withdraw and optional auto-deposit mapping; use `-1` for a balance with no inventory item.
   - `enableAutoDeposit`: only applies when `gameItemId > 0` and the player turns on the row toggle in the vault UI (same behavior as built-in currencies).

**Orphan balances:** If a mod registers a custom currency and is later removed, saved totals can remain under `CustomCurrencies` in the vault file. They still **load**; enable **full vault inspector** (Haven Dev Tools → The Vault) and open the **Debug** tab to see raw keys. Re-register the same short id if you want them back in the normal **Custom** tab with labels.

**Hooks for other mods:** `VaultManager` exposes `OnVaultLoaded` and `OnCurrencyChanged` (full currency id + old/new amounts). Subscribe via `Plugin.GetVaultManager()` after the vault exists.

**Thin API assembly:** reference **`TheVault.Abstractions.dll`** (same `plugins/TheVault` folder as `TheVault.dll`). At runtime use **`TheVault.Modding.VaultModApiBridge.Instance`** (`IVaultModApi`: `IsVaultReady`, `RegisterCustomCurrency`). This mirrors `VaultIntegration` without referencing the game or the main plugin assembly.

## Build

- `dotnet build TheVault/TheVault.csproj` → `TheVault.dll` plus **`TheVault.Abstractions.dll`** copied next to it under BepInEx `plugins/TheVault` (see the csproj `CopyToPlugins` targets). Ship **both** DLLs for players; dependents may reference only the abstractions assembly.
- Optional unit tests (no Unity): `dotnet test TheVault/Tests/TheVault.Tests.csproj -c Release`

## Version

**Released version string is `3.0.0`** (BepInEx, Thunderstore, Nexus) **until further notice** — do not bump for internal refactors only.

## Changelog

### 3.0.0 (ongoing)

- **Version:** Shipping **3.0.0** everywhere (BepInEx `PluginInfo`, Thunderstore manifest, `docs/versions.json`, site mod cards, Haven Dev Tools version checker list, and assembly `Version` / `AssemblyVersion` in `TheVault.csproj` + `TheVault.Abstractions.csproj`).
- **Main vault window:** Legacy **IMGUI** vault window is the default (uGUI panel disabled). Hotkeys and `Plugin.OpenVault` / `CloseVault` open the classic window; HUD hides while the vault window is open.
- **HUD:** Larger default footprint — config **`[HUD] Scale`** default **1.25**, wider max width (**1200px**), slightly taller screen height budget, bigger icon/padding/font baselines. IMGUI styles no longer recreate gradient/border textures every frame when only the layout scale changes (less GPU/CPU churn). Row lists are pooled to cut per-frame allocations. (Earlier in 3.0.0: clipping/offset fix and palette refresh.)
- **Performance:** Inventory **RemoveItem** / sweep paths resolve `MethodInfo` once per inventory type via plain reflection (avoids repeated HarmonyX “could not find method” warnings and reflection overhead). **`Player.Inventory`** is used directly when available.
- **Toast text:** Auto-deposit pickup toasts now show **`NNx ItemName`** by passing the actual game item name to the notification stack (instead of the generic "Vault" label).
- **Vault safety (RemoveItem hook):** vault deduction from `Inventory.RemoveItem` now only runs when a valid pre-remove inventory count is captured, and it is suppressed during auto-deposit/sweep inventory removals to prevent accidental vault decrements.
- **Full vault inspector:** toggled from **Haven Dev Tools** (`com.azraelgodking.havendevtools.cfg` → `[The Vault]` **FullVaultInspector**, or F11 → Azrael's Mods → The Vault). Removed from `com.azraelgodking.thevault.cfg`; The Vault Settings shows status only.
- **Custom tab & mod API:** **Custom** category in the vault UI; HUD shows non-zero custom balances after built-in groups. Third-party mods register currencies via `TheVault.Api.VaultIntegration.RegisterCustomCurrency` (see **Custom currencies** above).
- **Debug full vault:** optional **`[The Vault] FullVaultInspector`** in **Haven Dev Tools** config / Azrael's Mods panel: zeros visible in tabs, **Debug** tab raw dump of every vault dictionary, HUD shows all slots (former `thevault.cfg` key removed).
- **Roadmap & save policy:** [roadmap.md](./roadmap.md) documents priorities (players → maintenance → mod authors) and the **no-wipe / migrate-first** rule for vault saves; README leads with a **vault save notice** and the process if a rare breaking release is ever required.
- **Player UX blitz:** Tab-specific empty copy; in-window **status** for withdraw/deposit; **Deposit** for the selected row; **HUD** density presets; restored **Debug** tab wiring when FullVaultInspector is on.
- **Maintenance:** `VaultDataSanitizer` after load; `VaultCurrencyIds` used across **Plugin**, **VaultManager**, **ItemPatches**, **HUD**; **NUnit** tests under `Tests/` (including null-dictionary recovery).
- **HUD:** Built-in slots use **`VaultManager.GetCurrency`** (same as the vault window). Layout is **width-capped to the screen** (with edge inset), **wraps to extra rows** when needed, and keeps **stable order** (seasonal → keys → special → custom). Amount labels use **`CalcSize`-based widths** so **K / M** text is not clipped. **Keys/Tickets** save keys normalize to lowercase when casing would break lookups.

### 3.0.0 — Full rework of The Vault system

This release is a **complete rework** of how the mod is structured, built, and wired into Sun Haven. The previous V3-style rewrite (separate persistence stack, shared crypto helpers, and split source trees) was **retired**. The shipping mod again centers on the **classic, field-proven vault core**: `VaultManager` + `VaultSaveSystem`, per-character encrypted saves, the existing IMGUI vault UI/HUD, auto-deposit, shop and door integration, and Secret Gifts—implemented as **one coherent codebase** under [`TheVault/`](./).

**Architecture & project layout**

- **Single mod project:** [`TheVault.csproj`](TheVault.csproj) is the only build target; output remains `TheVault.dll` with GUID `com.azraelgodking.thevault`.
- **Sources live in `TheVault/`** (`Plugin.cs`, `Patches/`, `Vault/`, `UI/`). The old dual-layout (main + `TheVault-legacy` project compiling overlapping code) is gone; **`TheVault-Legacy.csproj` was removed** so there is no second DLL or sidecar plugin ID.
- **Shared utilities** limited to what the classic stack needs (e.g. version check, reflection helpers, icon cache).

**Game integration layer (Harmony / `Wish.*`)**

- Patch registration and hot paths were **rebased on real game types** where possible: `GameSave`, `Shop`, `Inventory`, `Item`, `MainMenuController`, `Player`, etc., instead of string-only `TypeByName` for those entry points.
- **Character load / vault identity:** `GameSavePatches` resolves the loaded character from typed `GameSaveData` / `CharacterData` (`characterName`, `fileName` fallback) instead of broad reflection over save blobs.
- **Economy & inventory:** Shop prefixes use typed `ShopItemInfo2` / `ShopLoot2` item ids; inventory sweep and removal paths prefer **`Wish.Inventory`** APIs when the instance is the real bag type.
- **Auto-deposit & pickups:** Item id resolution uses **`Wish.Item.ID()`** on the fast path; notifications go through **`Wish.NotificationStack`**; vault open/close uses **`Wish.PlayerInput`** for pause-style input blocking.
- **Noise reduction:** Verbose pickup/inventory discovery logging during patch setup was removed so startup logs stay usable.

**Build & dependencies**

- **Compile-time reference** to `Sirenix.Serialization.dll` from `Sun Haven_Data/Managed` (notification UI types inherit Odin/Sirenix bases). Runtime still only needs normal BepInEx + game assemblies.

**Version string**

- **Released version remains `3.0.0`** (BepInEx, Thunderstore, Nexus) until explicitly changed; this rework is shipped under that number by policy.
