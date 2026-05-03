using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SenpaisChest.Data
{
    internal static class ItemNamePatternMatch
    {
        /// <summary>
        /// Simple glob against item display names: <c>*</c> = any run, <c>?</c> = one character. Case-insensitive.
        /// </summary>
        internal static bool Matches(string itemName, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;

            Regex rx;
            try
            {
                rx = RegexCache.Instance.GetOrAdd(pattern.Trim());
            }
            catch (ArgumentException)
            {
                return false;
            }

            return itemName != null && rx.IsMatch(itemName);
        }

        /// <summary>Compile glob → anchored regex.</summary>
        private static Regex BuildRegex(string glob)
        {
            var sb = new StringBuilder("^");
            foreach (char c in glob)
            {
                switch (c)
                {
                    case '*':
                        sb.Append(".*");
                        break;
                    case '?':
                        sb.Append(".");
                        break;
                    default:
                        sb.Append(Regex.Escape(c.ToString()));
                        break;
                }
            }

            sb.Append('$');
            return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private sealed class RegexCache
        {
            internal static readonly RegexCache Instance = new RegexCache();

            private readonly Dictionary<string, Regex> _map = new Dictionary<string, Regex>(System.StringComparer.Ordinal);

            internal Regex GetOrAdd(string glob)
            {
                lock (_map)
                {
                    if (!_map.TryGetValue(glob, out var rx))
                    {
                        rx = BuildRegex(glob);
                        _map[glob] = rx;
                    }

                    return rx;
                }
            }
        }
    }
}
