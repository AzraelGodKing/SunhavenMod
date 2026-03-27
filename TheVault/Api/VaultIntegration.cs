using System;
using System.Text.RegularExpressions;
using SunhavenMods.Shared;
using TheVault.Patches;
using TheVault.Vault;

namespace TheVault.Api
{
    /// <summary>
    /// Public hooks for other BepInEx mods to register currencies that appear under the vault **Custom** tab
    /// and use storage <c>custom_{id}</c> (same prefix as <see cref="VaultManager.GetCurrency"/>).
    /// </summary>
    public static class VaultIntegration
    {
        private static readonly Regex ValidId = new Regex("^[a-z0-9_]{1,64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// True after The Vault has constructed <see cref="VaultManager"/> (typically right after its plugin Awake).
        /// </summary>
        public static bool IsVaultReady => Plugin.GetVaultManager() != null;

        /// <summary>
        /// Register a mod-owned currency. Vault id: <c>custom_{id}</c> (do not pass the <c>custom_</c> prefix in <paramref name="id"/>).
        /// </summary>
        /// <param name="id">Short id: lowercase letters, digits, underscore only (e.g. <c>my_mod_soul_shard</c>).</param>
        /// <param name="displayName">Shown in the vault UI and HUD.</param>
        /// <param name="gameItemId">Sun Haven item id for withdraw / optional auto-deposit; use -1 for vault-only balance with no item.</param>
        /// <param name="enableAutoDeposit">If true and <paramref name="gameItemId"/> &gt; 0, pickups auto-deposit when global auto-deposit is on and the player enables the row toggle.</param>
        /// <returns>False if The Vault is not ready, id is invalid, or id was already registered.</returns>
        public static bool RegisterCustomCurrency(string id, string displayName, int gameItemId = -1, bool enableAutoDeposit = false)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = id;

            string normalized = NormalizeId(id);
            if (normalized == null || !ValidId.IsMatch(normalized))
            {
                Plugin.Log?.LogWarning($"[VaultIntegration] Invalid custom currency id '{id}'. Use lowercase letters, digits, underscore only (max 64).");
                return false;
            }

            var manager = Plugin.GetVaultManager();
            if (manager == null)
            {
                Plugin.Log?.LogWarning("[VaultIntegration] The Vault is not loaded yet. Use [BepInDependency(\"com.azraelgodking.thevault\")] and register from Awake, or Chainloader.OnPluginActivated.");
                return false;
            }

            if (manager.GetCurrencyDefinition(normalized) != null)
            {
                Plugin.Log?.LogWarning($"[VaultIntegration] Currency id '{normalized}' is already registered.");
                return false;
            }

            var def = new CurrencyDefinition(normalized, displayName.Trim(), CurrencyCategory.Custom, gameItemId);
            manager.RegisterCurrency(def);

            string fullCurrencyId = VaultCurrencyIds.FullCustom(normalized);
            if (gameItemId > 0)
            {
                IconCache.RegisterCurrency(fullCurrencyId, gameItemId);
                ItemPatches.RegisterItemCurrencyMapping(gameItemId, fullCurrencyId, enableAutoDeposit);
            }

            Plugin.Log?.LogInfo($"[VaultIntegration] Registered custom currency '{fullCurrencyId}' ({displayName}), itemId={gameItemId}, autoDeposit={enableAutoDeposit}");
            return true;
        }

        private static string NormalizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            id = id.Trim().ToLowerInvariant();
            if (id.StartsWith("custom_", StringComparison.Ordinal))
                id = id.Substring("custom_".Length);
            return string.IsNullOrEmpty(id) ? null : id;
        }
    }
}
