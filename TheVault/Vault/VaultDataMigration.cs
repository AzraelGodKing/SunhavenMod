namespace TheVault.Vault
{
    /// <summary>
    /// Centralized version migration for vault save payloads.
    /// Kept separate from VaultSaveSystem so migration rules are unit-testable.
    /// </summary>
    public static class VaultDataMigration
    {
        public static VaultData Migrate(VaultData data, System.Action<string> logInfo = null)
        {
            data = data ?? new VaultData();

            if (data.Version < 1)
            {
                data.Version = 1;
                logInfo?.Invoke("Migrated vault data to version 1");
            }

            if (data.Version < 2)
            {
                data.Version = 2;
                logInfo?.Invoke("Migrated vault data to version 2 (no transform; reserves the version for future field changes)");
            }

            return data;
        }
    }
}
