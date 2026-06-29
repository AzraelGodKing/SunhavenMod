using System;
using HarmonyLib;
using I2.Loc;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Harmony postfix on I2 LocalizationManager.SetLanguageAndCode — notifies mod UIs to refresh.
    /// </summary>
    public static class LanguageChangeWatcher
    {
        private static bool _patched;

        public static event Action<string> LanguageChanged;

        public static void EnsurePatched(Harmony harmony)
        {
            if (_patched || harmony == null)
                return;

            try
            {
                var postfix = AccessTools.Method(typeof(LanguageChangeWatcher), nameof(OnSetLanguageAndCode));
                harmony.Patch(
                    AccessTools.Method(typeof(LocalizationManager), nameof(LocalizationManager.SetLanguageAndCode)),
                    postfix: new HarmonyMethod(postfix));
                _patched = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to patch LocalizationManager.SetLanguageAndCode", ex);
            }
        }

        // Parameter names must match the original method signature exactly
        // (HarmonyX injects by name): SetLanguageAndCode(string LanguageName, string LanguageCode, ...).
        private static void OnSetLanguageAndCode(string LanguageName, string LanguageCode)
        {
            string code = string.IsNullOrWhiteSpace(LanguageCode)
                ? LocalizationManager.CurrentLanguageCode
                : LanguageCode;

            string normalized = ModLocalization.NormalizeLanguageCode(code);
            ModLocalization.OnGameLanguageChanged(normalized);
            LanguageChanged?.Invoke(normalized);
        }

        internal static void RaiseLanguageChanged(string languageCode)
        {
            string normalized = ModLocalization.NormalizeLanguageCode(languageCode);
            LanguageChanged?.Invoke(normalized);
        }
    }
}
