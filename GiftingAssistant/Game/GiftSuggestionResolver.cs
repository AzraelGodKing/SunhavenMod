using System.Collections.Generic;

using System.Text;

using GiftingAssistant.Data;

using SunhavenMods.Shared;

using UnityEngine;



namespace GiftingAssistant.Game

{

    /// <summary>

    /// Resolves which gifts to surface for an NPC: the player's preferred picks when set,

    /// otherwise the game's loved-then-liked lists. Roster rows show all preferred icons;

    /// Sun Haven Todo uses one random preferred (or loved/liked) icon via <see cref="ResolvePrimaryIconId"/>.

    /// </summary>

    public static class GiftSuggestionResolver

    {

        private sealed class IconPickCacheEntry

        {

            public string Signature;

            public int IconId;

        }



        private static readonly Dictionary<string, IconPickCacheEntry> PrimaryIconCache =

            new Dictionary<string, IconPickCacheEntry>(System.StringComparer.OrdinalIgnoreCase);



        /// <summary>Clears cached primary icon picks (window open, new in-game day).</summary>

        public static void ClearPrimaryIconCache()

        {

            PrimaryIconCache.Clear();

        }



        /// <summary>Drops the cached pick for one NPC (+Todo republish).</summary>

        public static void RefreshPrimaryIconForNpc(string npcName)

        {

            string key = GiftGameData.NormalizeNpcName(npcName);

            if (!string.IsNullOrEmpty(key))

                PrimaryIconCache.Remove(key);

        }



        /// <summary>All preferred gift IDs for roster row display; empty when none are set.</summary>

        public static List<int> ResolveDisplayGiftIds(GiftRosterEntry entry)

        {

            var result = new List<int>();

            if (entry?.PreferredGiftIds == null || entry.PreferredGiftIds.Count == 0)

                return result;



            foreach (int id in entry.PreferredGiftIds)

            {

                if (id > 0 && !result.Contains(id))

                    result.Add(id);

            }

            return result;

        }



        /// <summary>Preferred gift IDs if the player selected any, else loved-then-liked from the game.</summary>

        public static List<int> ResolveGiftIds(GiftRosterEntry entry)

        {

            var result = new List<int>();

            if (entry == null)

                return result;



            if (entry.PreferredGiftIds != null && entry.PreferredGiftIds.Count > 0)

            {

                foreach (int id in entry.PreferredGiftIds)

                {

                    if (id > 0 && !result.Contains(id))

                        result.Add(id);

                }

                return result;

            }



            var info = GiftGameData.GetNpcInfo(entry.NpcName);

            if (info != null)

            {

                AddRange(result, info.LovedItemIds);

                AddRange(result, info.LikedItemIds);

            }

            return result;

        }



        /// <summary>

        /// One random gift ID for Sun Haven Todo task icons only. Random among preferred gifts

        /// when several are set; otherwise random loved, then random liked. Cached per NPC until cleared.

        /// </summary>

        public static int ResolvePrimaryIconId(GiftRosterEntry entry, bool forceRefresh = false)

        {

            if (entry == null || string.IsNullOrEmpty(entry.NpcName))

                return -1;



            string key = GiftGameData.NormalizeNpcName(entry.NpcName);

            if (string.IsNullOrEmpty(key))

                return -1;



            string signature = BuildGiftSignature(entry);

            if (!forceRefresh &&

                PrimaryIconCache.TryGetValue(key, out IconPickCacheEntry cached) &&

                cached.Signature == signature)

                return cached.IconId;



            int iconId = PickRandomIconId(entry);

            PrimaryIconCache[key] = new IconPickCacheEntry { Signature = signature, IconId = iconId };

            return iconId;

        }



        /// <summary>Comma-separated names of up to <paramref name="max"/> suggested gifts.</summary>

        public static string ResolveGiftNames(GiftRosterEntry entry, int max)

        {

            var ids = ResolveGiftIds(entry);

            if (ids.Count == 0)

                return "";



            var sb = new StringBuilder();

            int count = ids.Count < max ? ids.Count : max;

            for (int i = 0; i < count; i++)

            {

                if (sb.Length > 0)

                    sb.Append(", ");

                sb.Append(ItemSearch.GetItemName(ids[i]) ?? $"#{ids[i]}");

            }

            if (ids.Count > count)

                sb.Append(", ...");

            return sb.ToString();

        }



        private static string BuildGiftSignature(GiftRosterEntry entry)

        {

            var sb = new StringBuilder();

            if (entry.PreferredGiftIds != null && entry.PreferredGiftIds.Count > 0)

            {

                sb.Append("p:");

                AppendIds(sb, entry.PreferredGiftIds);

            }

            else

            {

                sb.Append("a:");

                var info = GiftGameData.GetNpcInfo(entry.NpcName);

                if (info != null)

                {

                    AppendIds(sb, info.LovedItemIds);

                    sb.Append('|');

                    AppendIds(sb, info.LikedItemIds);

                }

            }



            return sb.ToString();

        }



        private static void AppendIds(StringBuilder sb, IList<int> ids)

        {

            if (ids == null)

                return;

            foreach (int id in ids)

            {

                if (id <= 0)

                    continue;

                sb.Append(id);

                sb.Append(',');

            }

        }



        private static int PickRandomIconId(GiftRosterEntry entry)

        {

            if (entry.PreferredGiftIds != null && entry.PreferredGiftIds.Count > 0)

            {

                int preferred = PickRandomFromList(entry.PreferredGiftIds);

                if (preferred > 0)

                    return preferred;

            }



            var info = GiftGameData.GetNpcInfo(entry.NpcName);

            if (info == null)

                return -1;



            int loved = PickRandomFromList(info.LovedItemIds);

            if (loved > 0)

                return loved;



            return PickRandomFromList(info.LikedItemIds);

        }



        private static int PickRandomFromList(IList<int> ids)

        {

            if (ids == null || ids.Count == 0)

                return -1;



            var valid = new List<int>();

            foreach (int id in ids)

            {

                if (id > 0 && !valid.Contains(id))

                    valid.Add(id);

            }



            if (valid.Count == 0)

                return -1;

            if (valid.Count == 1)

                return valid[0];



            return valid[Random.Range(0, valid.Count)];

        }



        private static void AddRange(List<int> destination, List<int> source)

        {

            if (source == null)

                return;

            foreach (int id in source)

            {

                if (id > 0 && !destination.Contains(id))

                    destination.Add(id);

            }

        }

    }

}


