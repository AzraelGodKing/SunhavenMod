using System;

namespace TheVault.Modding
{
    /// <summary>
    /// Set by The Vault on startup. Null before the plugin runs or if The Vault is disabled.
    /// </summary>
    public static class VaultModApiBridge
    {
        /// <summary>Implementation provided by the main TheVault plugin.</summary>
        public static IVaultModApi Instance { get; set; }

        /// <summary>
        /// Fired when The Vault has loaded character data and is ready for dependent registrations.
        /// Soft dependencies can subscribe and register currencies/integrations at the right time.
        /// </summary>
        public static event Action OnVaultLoaded;

        /// <summary>
        /// Raised by The Vault when the underlying vault data load completes.
        /// </summary>
        public static void NotifyVaultLoaded()
        {
            OnVaultLoaded?.Invoke();
        }
    }
}
