using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace SunhavenMods.Shared
{
    public static class ConfigFileHelper
    {
        public static ConfigFile CreateNamedConfig(string pluginGuid, string configFileName, Action<string> logWarning = null)
        {
            string configPath = Path.Combine(Paths.ConfigPath, configFileName);
            string legacyPath = Path.Combine(Paths.ConfigPath, $"{pluginGuid}.cfg");

            try
            {
                if (!File.Exists(configPath) && File.Exists(legacyPath))
                    File.Copy(legacyPath, configPath);
            }
            catch (Exception ex)
            {
                logWarning?.Invoke($"[Config] Migration to {configFileName} failed: {ex.Message}");
            }

            return new ConfigFile(configPath, true);
        }
    }
}
