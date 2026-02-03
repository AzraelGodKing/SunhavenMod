using SunHavenMuseumUtilityTracker.Patches;
using UnityEngine;

namespace SunHavenMuseumUtilityTracker.UI
{
    /// <summary>
    /// Debug mode for testing and inspecting museum progress.
    /// Activated with F10 key (only for development/testing).
    /// </summary>
    public class DebugMode : MonoBehaviour
    {
        private bool _debugEnabled = false;

        private void Update()
        {
            // F10 to toggle debug mode
            if (Input.GetKeyDown(KeyCode.F10))
            {
                _debugEnabled = !_debugEnabled;
                Plugin.Log?.LogInfo($"[DebugMode] Debug mode: {(_debugEnabled ? "ENABLED" : "DISABLED")}");

                if (_debugEnabled)
                {
                    // Run discovery when debug mode is enabled
                    MuseumPatches.DiscoverProgressKeyPatterns();
                    MuseumPatches.LogAllMappings();
                }
            }

            // F11 to force sync (only when debug enabled)
            if (_debugEnabled && Input.GetKeyDown(KeyCode.F11))
            {
                Plugin.Log?.LogInfo("[DebugMode] Force syncing with game progress...");
                MuseumPatches.SyncWithGameProgress();
            }
        }
    }
}
