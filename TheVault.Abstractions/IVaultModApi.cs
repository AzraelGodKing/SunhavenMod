namespace TheVault.Modding
{
    /// <summary>
    /// Stable, game-agnostic surface for third-party mods. Filled at runtime by The Vault (<see cref="VaultModApiBridge.Instance"/>).
    /// Signatures may version slowly; check mod update notes when bumping The Vault major versions.
    /// </summary>
    public interface IVaultModApi
    {
        bool IsVaultReady { get; }

        /// <summary>Same rules as <c>TheVault.Api.VaultIntegration.RegisterCustomCurrency</c>.</summary>
        bool RegisterCustomCurrency(string id, string displayName, int gameItemId = -1, bool enableAutoDeposit = false);
    }
}
