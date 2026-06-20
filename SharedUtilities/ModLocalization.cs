using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using I2.Loc;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Per-mod string tables keyed by stable IDs, resolved against Sun Haven's active I2 language.
    /// Each mod DLL links its own copy and calls <see cref="Init"/> with that mod's embedded JSON.
    /// </summary>
    public static class ModLocalization
    {
        private static readonly string[] SupportedLanguageCodes =
        {
            "en", "da", "de", "es", "fr", "it", "ja", "ko", "nl", "pt", "pt-BR", "ru", "sv", "zh-CN", "zh-TW", "uk"
        };

        private static readonly Dictionary<string, string> LanguageAlias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "pt-br", "pt-BR" },
            { "pt_br", "pt-BR" },
            { "zh-cn", "zh-CN" },
            { "zh_cn", "zh-CN" },
            { "zh-tw", "zh-TW" },
            { "zh_tw", "zh-TW" }
        };

        private static string _modId;
        private static Dictionary<string, Dictionary<string, string>> _tables;
        private static ManualLogSource _log;
        private static bool _initialized;

        public static string CurrentLanguage { get; private set; } = "en";

        /// <summary>Raised when the player changes language in Sun Haven settings.</summary>
        public static event Action<string> LanguageChanged
        {
            add => LanguageChangeWatcher.LanguageChanged += value;
            remove => LanguageChangeWatcher.LanguageChanged -= value;
        }

        public static void Init(string modId, Dictionary<string, Dictionary<string, string>> tables, Harmony harmony, ManualLogSource log)
        {
            _modId = modId ?? string.Empty;
            _tables = tables ?? new Dictionary<string, Dictionary<string, string>>();
            _log = log;
            _initialized = true;

            RefreshCurrentLanguage();
            LanguageChangeWatcher.EnsurePatched(harmony);
        }

        internal static void OnGameLanguageChanged(string languageCode)
        {
            if (!_initialized)
                return;

            string normalized = NormalizeLanguageCode(languageCode);
            if (string.Equals(CurrentLanguage, normalized, StringComparison.OrdinalIgnoreCase))
                return;

            CurrentLanguage = normalized;
            _log?.LogDebug($"[{_modId}] Language changed to {CurrentLanguage}");
        }

        public static void RefreshCurrentLanguage()
        {
            try
            {
                string code = LocalizationManager.CurrentLanguageCode;
                if (!string.IsNullOrWhiteSpace(code))
                    CurrentLanguage = NormalizeLanguageCode(code);
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[{_modId}] Failed to read LocalizationManager.CurrentLanguageCode: {ex.Message}");
                CurrentLanguage = "en";
            }
        }

        public static string T(string key)
        {
            return TryT(key, out string value) ? value : key;
        }

        public static string T(string key, params object[] args)
        {
            string text = T(key);
            if (args == null || args.Length == 0)
                return text;

            try
            {
                return string.Format(CultureInfo.InvariantCulture, text, args);
            }
            catch (FormatException ex)
            {
                _log?.LogWarning($"[{_modId}] Format failed for key '{key}': {ex.Message}");
                return text;
            }
        }

        public static bool TryT(string key, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(key))
                return false;

            if (_tables == null || !_tables.TryGetValue(key, out Dictionary<string, string> translations) || translations == null)
                return false;

            if (TryGetForLanguage(translations, CurrentLanguage, out value))
                return true;

            if (!string.Equals(CurrentLanguage, "en", StringComparison.OrdinalIgnoreCase) &&
                TryGetForLanguage(translations, "en", out value))
                return true;

            return false;
        }

        private static bool TryGetForLanguage(Dictionary<string, string> translations, string languageCode, out string value)
        {
            value = null;
            if (translations == null)
                return false;

            string normalized = NormalizeLanguageCode(languageCode);
            if (translations.TryGetValue(normalized, out value) && !string.IsNullOrEmpty(value))
                return true;

            foreach (var pair in translations)
            {
                if (string.Equals(pair.Key, normalized, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(pair.Value))
                {
                    value = pair.Value;
                    return true;
                }
            }

            return false;
        }

        public static string NormalizeLanguageCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "en";

            string trimmed = code.Trim();
            if (LanguageAlias.TryGetValue(trimmed, out string alias))
                return alias;

            foreach (string supported in SupportedLanguageCodes)
            {
                if (string.Equals(supported, trimmed, StringComparison.OrdinalIgnoreCase))
                    return supported;
            }

            return "en";
        }

        /// <summary>
        /// Parses embedded localization JSON: { "key": { "en": "...", "fr": "..." } }.
        /// </summary>
        public static Dictionary<string, Dictionary<string, string>> ParseStringsJson(string json)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(json))
                return result;

            int pos = 0;
            var root = MinimalJsonParser.ParseObject(json, ref pos);
            if (root == null)
                return result;

            foreach (var entry in root)
            {
                if (entry.Value is Dictionary<string, object> langObj)
                {
                    var langDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var langEntry in langObj)
                    {
                        if (langEntry.Value is string s)
                            langDict[NormalizeLanguageCode(langEntry.Key)] = s;
                    }

                    if (langDict.Count > 0)
                        result[entry.Key] = langDict;
                }
            }

            return result;
        }

        public static Dictionary<string, Dictionary<string, string>> LoadEmbeddedStrings(Assembly assembly, string resourceName, ManualLogSource log = null)
        {
            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        log?.LogError($"Localization resource not found: {resourceName}");
                        return new Dictionary<string, Dictionary<string, string>>();
                    }

                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return ParseStringsJson(reader.ReadToEnd());
                    }
                }
            }
            catch (Exception ex)
            {
                log?.LogError($"Failed to load localization resource '{resourceName}': {ex.Message}");
                return new Dictionary<string, Dictionary<string, string>>();
            }
        }

        public static void Shutdown()
        {
            _initialized = false;
            _tables = null;
            _modId = null;
            _log = null;
            CurrentLanguage = "en";
        }
    }
}
