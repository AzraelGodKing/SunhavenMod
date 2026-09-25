namespace TheVault.Vault
{
    /// <summary>
    /// Pure save-gate for the AZR-238 failed-load path (no Unity/BepInEx).
    /// When load recovers no data, persistence must stay blocked until the player confirms Start Fresh.
    /// </summary>
    public static class VaultFailedLoadSavePolicy
    {
        /// <summary>
        /// Returns false while <paramref name="loadFailedNoRecoverableData"/> is latched,
        /// so autosave cannot rotate away a surviving <c>.backup</c>.
        /// </summary>
        public static bool AllowSave(bool loadFailedNoRecoverableData) =>
            !loadFailedNoRecoverableData;
    }
}
