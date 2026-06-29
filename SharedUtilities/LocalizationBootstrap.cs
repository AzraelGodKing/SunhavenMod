using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace SunhavenMods.Shared
{
    /// <summary>One-line localization setup for mod plugins.</summary>
    public static class LocalizationBootstrap
    {
        public static ConfigEntry<bool> BindForceEnglish(ConfigFile config)
        {
            var entry = config.Bind(
                "Localization",
                "ForceEnglish",
                false,
                "Keep this mod's UI in English and ignore Sun Haven's in-game language setting.");

            ApplyForceEnglish(entry.Value);
            entry.SettingChanged += (_, __) => ApplyForceEnglish(entry.Value);
            return entry;
        }

        private static void ApplyForceEnglish(bool forceEnglish)
        {
            ModLocalization.SetForceEnglish(forceEnglish);
            LanguageChangeWatcher.RaiseLanguageChanged(ModLocalization.CurrentLanguage);
        }

        public static void Init(string pluginGuid, Harmony harmony, ManualLogSource log, Assembly assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();
            var json = ModLocalization.LoadEmbeddedStrings(
                assembly,
                $"{pluginGuid}.Localization.strings.json",
                log);
            ModLocalization.Init(pluginGuid, json, harmony, log);
        }

        /// <summary>
        /// Loads embedded strings when tables are missing (e.g. first init used the wrong assembly).
        /// Safe to call from persistent UI that survives plugin teardown.
        /// </summary>
        public static void EnsureInitialized(string pluginGuid, Harmony harmony, ManualLogSource log, Assembly assembly = null)
        {
            if (ModLocalization.IsReady)
                return;

            Init(pluginGuid, harmony, log, assembly);
        }
    }
}
