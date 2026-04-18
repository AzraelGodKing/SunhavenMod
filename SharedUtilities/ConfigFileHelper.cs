using System;
using System.IO;
using System.Reflection;
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

        /// <summary>
        /// Swap the plugin's inherited <see cref="BaseUnityPlugin.Config"/> property so it points
        /// at <paramref name="newConfig"/>. Without this, BepInEx Configuration Manager cannot
        /// discover config entries bound to a custom <see cref="ConfigFile"/> because it only
        /// scans each plugin's default <c>Config</c> property.
        /// </summary>
        public static bool ReplacePluginConfig(BaseUnityPlugin plugin, ConfigFile newConfig, Action<string> logWarning = null)
        {
            if (plugin == null || newConfig == null)
                return false;

            try
            {
                var type = typeof(BaseUnityPlugin);

                var prop = type.GetProperty("Config", BindingFlags.Instance | BindingFlags.Public);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(plugin, newConfig, null);
                    return true;
                }

                var backingField = type.GetField("<Config>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                if (backingField != null)
                {
                    backingField.SetValue(plugin, newConfig);
                    return true;
                }

                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (field.FieldType == typeof(ConfigFile))
                    {
                        field.SetValue(plugin, newConfig);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logWarning?.Invoke($"[Config] ReplacePluginConfig failed: {ex.Message}");
            }

            return false;
        }
    }
}
