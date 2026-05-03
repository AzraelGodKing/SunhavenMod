using TheVault.Modding;
using UnityEngine.SceneManagement;

namespace TheVault
{
    /// <summary>Scene transitions + <see cref="VaultModApiBridge.OnVaultLoaded"/> wire-up (split from <see cref="Plugin"/> for maintainability).</summary>
    public partial class Plugin
    {
        private static void OnVaultDataLoaded()
        {
            try
            {
                VaultModApiBridge.NotifyVaultLoaded();
            }
            catch (System.Exception ex)
            {
                Log?.LogWarning($"[The Vault] Failed to raise OnVaultLoaded bridge event: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when a new scene is loaded.
        /// We only care about detecting menu scenes to reset vault state.
        /// Actual vault loading is handled by OnPlayerInitialized.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                Log.LogDebug($"[SceneChange] Scene loaded: '{scene.name}' (mode: {mode})");

                string sceneLower = scene.name.ToLowerInvariant();
                bool isMenuScene = sceneLower.Contains("menu") || sceneLower.Contains("title");

                if (isMenuScene && !_wasInMenuScene)
                {
                    Log.LogInfo($"Menu scene detected: {scene.name}");
                    Patches.PlayerPatches.SaveAndReset();
                }
                _wasInMenuScene = isMenuScene;
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Error in OnSceneLoaded: {ex.Message}");
            }
        }
    }
}
