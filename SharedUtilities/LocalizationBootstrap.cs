using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace SunhavenMods.Shared
{
    /// <summary>One-line localization setup for mod plugins.</summary>
    public static class LocalizationBootstrap
    {
        public static void Init(string pluginGuid, Harmony harmony, ManualLogSource log, Assembly assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            var json = ModLocalization.LoadEmbeddedStrings(
                assembly,
                $"{pluginGuid}.Localization.strings.json");
            ModLocalization.Init(pluginGuid, json, harmony, log);
        }
    }
}
