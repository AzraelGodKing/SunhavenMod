namespace CropOptimizer.Integration
{
    internal sealed class VaultIntegration
    {
        public bool IsAvailable => VaultReflection.IsVaultPluginPresent;

        public bool TryRegisterProjectedValueCurrency()
        {
            if (!IsAvailable)
                return false;

            var bridgeType = VaultReflection.GetBridgeType();
            var instance = VaultReflection.GetBridgeInstance(bridgeType);
            if (!VaultReflection.IsVaultReady(instance))
                return false;

            return VaultReflection.TryRegisterCustomCurrency(
                instance,
                id: "crop_projected_value",
                displayName: "Projected Crop Value",
                gameItemId: -1,
                enableAutoDeposit: false);
        }
    }
}
