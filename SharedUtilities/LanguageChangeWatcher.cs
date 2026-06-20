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

        private static void OnSetLanguageAndCode(string languageName, string languageCode)
        {
            string code = string.IsNullOrWhiteSpace(languageCode)
                ? LocalizationManager.CurrentLanguageCode
                : languageCode;

            string normalized = ModLocalization.NormalizeLanguageCode(code);
            ModLocalization.OnGameLanguageChanged(normalized);
            LanguageChanged?.Invoke(normalized);
        }
    }
}
