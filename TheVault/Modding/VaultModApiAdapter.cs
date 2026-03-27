using TheVault.Api;
using TheVault.Modding;

namespace TheVault.Integration
{
    /// <summary>
    /// Bridges <see cref="IVaultModApi"/> to the in-DLL <see cref="VaultIntegration"/> API.
    /// </summary>
    internal sealed class VaultModApiAdapter : global::TheVault.Modding.IVaultModApi
    {
        public bool IsVaultReady => VaultIntegration.IsVaultReady;

        public bool RegisterCustomCurrency(string id, string displayName, int gameItemId = -1, bool enableAutoDeposit = false)
        {
            return VaultIntegration.RegisterCustomCurrency(id, displayName, gameItemId, enableAutoDeposit);
        }
    }
}
