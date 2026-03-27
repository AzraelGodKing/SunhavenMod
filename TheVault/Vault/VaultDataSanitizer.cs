using System;
using System.Collections.Generic;

namespace TheVault.Vault
{
    /// <summary>
    /// Load-time normalization: non-destructive fixes (e.g. negative counts). Returns number of corrections.
    /// </summary>
    public static class VaultDataSanitizer
    {
        public static int Sanitize(VaultData data)
        {
            if (data == null) return 0;

            int fixes = 0;

            data.SeasonalTokens ??= new Dictionary<SeasonalTokenType, int>();
            data.CommunityTokens ??= new Dictionary<string, int>();
            data.Keys ??= new Dictionary<string, int>();
            data.CustomCurrencies ??= new Dictionary<string, int>();
            data.Tickets ??= new Dictionary<string, int>();
            data.Orbs ??= new Dictionary<string, int>();

            foreach (SeasonalTokenType key in Enum.GetValues(typeof(SeasonalTokenType)))
            {
                if (!data.SeasonalTokens.ContainsKey(key))
                    data.SeasonalTokens[key] = 0;
            }

            foreach (var key in new List<SeasonalTokenType>(data.SeasonalTokens.Keys))
            {
                if (data.SeasonalTokens[key] < 0)
                {
                    data.SeasonalTokens[key] = 0;
                    fixes++;
                }
            }

            fixes += ClampStringKeyedDict(data.CommunityTokens);
            fixes += NormalizeDictKeysLower(data.Keys);
            fixes += NormalizeDictKeysLower(data.Tickets);
            fixes += ClampStringKeyedDict(data.Keys);
            fixes += ClampStringKeyedDict(data.CustomCurrencies);
            fixes += ClampStringKeyedDict(data.Tickets);
            fixes += ClampStringKeyedDict(data.Orbs);

            return fixes;
        }

        /// <summary>
        /// Lowercase Keys/Tickets entries and merge duplicate keys so lookups match vault ids (e.g. <c>manashard</c>).
        /// </summary>
        private static int NormalizeDictKeysLower(Dictionary<string, int> dict)
        {
            if (dict == null || dict.Count == 0) return 0;

            var merged = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var kvp in dict)
            {
                string k = string.IsNullOrEmpty(kvp.Key) ? "" : kvp.Key.ToLowerInvariant();
                if (merged.ContainsKey(k)) merged[k] += kvp.Value;
                else merged[k] = kvp.Value;
            }

            bool unchanged = merged.Count == dict.Count;
            if (unchanged)
            {
                foreach (var kvp in dict)
                {
                    string canon = string.IsNullOrEmpty(kvp.Key) ? "" : kvp.Key.ToLowerInvariant();
                    if (kvp.Key != canon || merged[canon] != kvp.Value)
                    {
                        unchanged = false;
                        break;
                    }
                }
            }

            if (unchanged) return 0;

            dict.Clear();
            foreach (var kvp in merged)
                dict[kvp.Key] = kvp.Value;
            return 1;
        }

        private static int ClampStringKeyedDict(Dictionary<string, int> dict)
        {
            int fixes = 0;
            var keys = new List<string>(dict.Keys);
            foreach (string k in keys)
            {
                if (dict[k] < 0)
                {
                    dict[k] = 0;
                    fixes++;
                }
            }

            return fixes;
        }
    }
}
