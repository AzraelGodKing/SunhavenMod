using System;
using System.Collections.Generic;
using System.Linq;
using Wish;

namespace HavenDevTools.Services
{
    internal static class NpcDictionaryHelper
    {
        internal static string ResolveNpcKey(IReadOnlyDictionary<string, NPCAI> npcs, string input)
        {
            if (npcs == null || string.IsNullOrWhiteSpace(input))
                return null;

            foreach (var pair in npcs)
            {
                if (pair.Key.Equals(input, StringComparison.OrdinalIgnoreCase))
                    return pair.Key;
            }

            return null;
        }
    }
}
