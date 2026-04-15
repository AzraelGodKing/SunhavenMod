using BepInEx.Bootstrap;
using TheVault.Modding;

namespace CropOptimizer.Integration
{
    internal sealed class VaultIntegration
    {
        public bool IsAvailable => Chainloader.PluginInfos.ContainsKey("com.azraelgodking.thevault");

        public bool TryRegisterProjectedValueCurrency()
        {
            if (!IsAvailable || VaultModApiBridge.Instance == null || !VaultModApiBridge.Instance.IsVaultReady)
                return false;

            return VaultModApiBridge.Instance.RegisterCustomCurrency(
                id: "crop_projected_value",
                displayName: "Projected Crop Value",
                gameItemId: -1,
                enableAutoDeposit: false);
        }
    }
}
