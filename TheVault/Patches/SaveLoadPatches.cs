using System;

namespace TheVault.Patches
{
    /// <summary>
    /// Patches for the game's save/load system.
    /// Ensures vault data is saved and loaded alongside game saves.
    /// </summary>
    public static class SaveLoadPatches
    {
        /// <summary>
        /// Called after the game saves.
        /// Triggers vault data save to maintain synchronization.
        /// </summary>
        public static void OnGameSaved()
        {
            try
            {
                Plugin.Log?.LogInfo("Game saved - saving vault data");
                Plugin.SaveVault();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error saving vault on game save: {ex.Message}");
            }
        }

        /// <summary>
        /// Called after the game loads.
        /// Vault data should already be loaded by PlayerPatches,
        /// but this serves as a backup trigger.
        /// </summary>
        public static void OnGameLoaded()
        {
            try
            {
                Plugin.Log?.LogInfo("Game loaded - verifying vault data");

                // Vault should already be loaded by PlayerPatches.OnPlayerInitialized
                // This is just a safety net
                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager != null)
                {
                    var data = vaultManager.GetVaultData();
                    Plugin.Log?.LogInfo($"Vault verified: Player={data.PlayerName}, LastSaved={data.LastSaved}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error verifying vault on game load: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when returning to main menu.
        /// Saves vault and resets state for next session.
        /// </summary>
        public static void OnReturnToMenu()
        {
            try
            {
                Plugin.Log?.LogInfo("Returning to menu - saving vault and resetting state");
                Plugin.SaveVault();
                PlayerPatches.ResetVaultLoaded();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error on return to menu: {ex.Message}");
            }
        }
    }
}
