using System;
using BepInEx.Configuration;
using HavenDevTools.Config;

namespace HavenDevTools.Services
{
    /// <summary>
    /// Reloads HavenDevTools config from disk and re-applies values.
    /// </summary>
    public static class ConfigReloader
    {
        public static void ReloadHavenDevToolsConfig()
        {
            try
            {
                if (Plugin.ConfigFile == null) return;

                Plugin.ConfigFile.Reload();
                ModConfig.Initialize(Plugin.ConfigFile);
                Plugin.StaticToggleKey = ModConfig.ToggleKey.Value;
                Plugin.StaticOverlayToggleKey = ModConfig.OverlayToggleKey.Value;
                Plugin.Log?.LogInfo("[ConfigReloader] HavenDevTools config reloaded");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[ConfigReloader] Error: {ex.Message}");
            }
        }
    }
}
