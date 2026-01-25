using System;

namespace TheVault.Patches
{
    /// <summary>
    /// Patches for the game's save/load system.
    /// </summary>
    public static class SaveLoadPatches
    {
        /// <summary>
        /// Called after the game saves.
        /// </summary>
        public static void OnGameSaved()
        {
            try
            {
                Plugin.Log?.LogInfo("Game saved - saving vault");
                Plugin.SaveVault();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error saving vault: {ex.Message}");
            }
        }

        /// <summary>
        /// Called after the game loads.
        /// Note: Main vault loading is handled by OnPlayerInitialized.
        /// </summary>
        public static void OnGameLoaded()
        {
            Plugin.Log?.LogInfo("Game loaded");
        }

        /// <summary>
        /// Called when returning to main menu.
        /// </summary>
        public static void OnReturnToMenu()
        {
            try
            {
                Plugin.Log?.LogInfo("Returning to menu");
                PlayerPatches.SaveAndReset();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error on return to menu: {ex.Message}");
            }
        }
    }
}
