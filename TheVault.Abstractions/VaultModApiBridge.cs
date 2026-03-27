namespace TheVault.Modding
{
    /// <summary>
    /// Set by The Vault on startup. Null before the plugin runs or if The Vault is disabled.
    /// </summary>
    public static class VaultModApiBridge
    {
        /// <summary>Implementation provided by the main TheVault plugin.</summary>
        public static IVaultModApi Instance { get; set; }
    }
}
