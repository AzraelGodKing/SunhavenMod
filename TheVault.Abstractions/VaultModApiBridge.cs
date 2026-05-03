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
        /// Soft dependencies should subscribe in their plugin <c>Awake</c> (or after Chainloader confirms The Vault)
        /// and <b>unsubscribe in <c>OnDestroy</c></b> if they use instance handlers, to avoid duplicate callbacks if the
        /// plugin host reloads. The Vault raises this from the main game thread after save data is applied.
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
