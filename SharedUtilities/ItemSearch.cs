using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Wish;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Shared item search using the game's ItemInfoDatabase.allItemSellInfos.
    /// Used by Senpai's Chest (search only) and Haven Dev Tools (search + spawn).
    /// Uses the same singleton + typed dictionary approach as SmartChestManager.
    /// </summary>
    public static class ItemSearch
    {
        private static readonly ManualLogSource _log = Logger.CreateLogSource("ItemSearch");

        private static object _dbInstance;
        private static FieldInfo _dictField;

        /// <summary>
        /// Format item for display: "Item Name (#Item ID)"
        /// </summary>
        public static string FormatDisplay(string name, int itemId)
        {
            if (string.IsNullOrEmpty(name)) return $"#{itemId}";
            return $"{name} (#{itemId})";
        }

        /// <summary>
        /// Search items by name or ID. Requires 2+ chars.
        /// Returns (id, name) pairs: exact match first, then starts-with, then contains.
        /// </summary>
        public static List<KeyValuePair<int, string>> SearchItems(string query, int maxResults = 50)
        {
            var results = new List<KeyValuePair<int, string>>();
            if (string.IsNullOrEmpty(query) || query.Trim().Length < 2)
                return results;

            try
            {
                var dict = GetItemDictionary();
                if (dict == null) return results;

                string queryLower = query.Trim().ToLowerInvariant();
                bool isNumeric = int.TryParse(query.Trim(), out int queryId);

                var bufferExact = new List<KeyValuePair<int, string>>();
                var bufferStartsWith = new List<KeyValuePair<int, string>>();
                var bufferContains = new List<KeyValuePair<int, string>>();

                foreach (var kvp in dict)
                {
                    int id = kvp.Key;
                    string name = kvp.Value?.name;
                    if (string.IsNullOrEmpty(name)) continue;

                    var entry = new KeyValuePair<int, string>(id, name);
                    string nameLower = name.ToLowerInvariant();

                    if (isNumeric && id == queryId)
                    {
                        bufferExact.Add(entry);
                        continue;
                    }
                    if (nameLower == queryLower)
                        bufferExact.Add(entry);
                    else if (nameLower.StartsWith(queryLower))
                        bufferStartsWith.Add(entry);
                    else if (nameLower.Contains(queryLower))
                        bufferContains.Add(entry);
                    else if (isNumeric && id.ToString().Contains(query.Trim()))
                        bufferContains.Add(entry);
                }

                bufferStartsWith.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));
                bufferContains.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));

                results.AddRange(bufferExact);
                results.AddRange(bufferStartsWith);
                results.AddRange(bufferContains);

                if (results.Count > maxResults)
                    results.RemoveRange(maxResults, results.Count - maxResults);
            }
            catch (Exception ex)
            {
                _log?.LogDebug($"[ItemSearch] SearchItems error: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Get item name by ID. Returns null if not found.
        /// </summary>
        public static string GetItemName(int itemId)
        {
            try
            {
                var dict = GetItemDictionary();
                if (dict == null || !dict.ContainsKey(itemId)) return null;
                return dict[itemId]?.name;
            }
            catch (Exception ex)
            {
                _log?.LogDebug($"[ItemSearch] GetItemName({itemId}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get the full ItemSellInfo for an item by ID. Returns null if not found.
        /// Used by SmartChestManager for rule matching (isGem, itemType, etc.).
        /// </summary>
        public static ItemSellInfo GetItemSellInfo(int itemId)
        {
            try
            {
                var dict = GetItemDictionary();
                if (dict != null && dict.TryGetValue(itemId, out var info))
                    return info;
            }
            catch (Exception ex)
            {
                _log?.LogDebug($"[ItemSearch] GetItemSellInfo({itemId}): {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Get the typed item dictionary from ItemInfoDatabase singleton.
        /// Mirrors SmartChestManager.GetItemSellInfo() approach exactly.
        /// </summary>
        private static Dictionary<int, ItemSellInfo> GetItemDictionary()
        {
            try
            {
                if (_dbInstance == null || (_dbInstance is UnityEngine.Object obj && obj == null))
                {
                    _dbInstance = null;
                    _dictField = null;

                    _dbInstance = GetSingletonInstance("Wish.ItemInfoDatabase");
                    if (_dbInstance != null)
                    {
                        _dictField = _dbInstance.GetType()
                            .GetField("allItemSellInfos", BindingFlags.Public | BindingFlags.Instance);
                    }
                }

                if (_dbInstance == null || _dictField == null)
                    return null;

                return _dictField.GetValue(_dbInstance) as Dictionary<int, ItemSellInfo>;
            }
            catch (Exception ex)
            {
                _log?.LogDebug($"[ItemSearch] GetItemDictionary: {ex.Message}");
                return null;
            }
        }

        private static object GetSingletonInstance(string typeName)
        {
            try
            {
                var singletonType = AccessTools.TypeByName("Wish.SingletonBehaviour`1");
                if (singletonType == null)
                    return null;

                var targetType = AccessTools.TypeByName(typeName);
                if (targetType == null)
                    return null;

                var constructedType = singletonType.MakeGenericType(targetType);
                var instanceProp = constructedType.GetProperty("Instance",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                return instanceProp?.GetValue(null);
            }
            catch (Exception ex)
            {
                _log?.LogDebug($"[ItemSearch] GetSingletonInstance({typeName}): {ex.Message}");
                return null;
            }
        }
    }
}
