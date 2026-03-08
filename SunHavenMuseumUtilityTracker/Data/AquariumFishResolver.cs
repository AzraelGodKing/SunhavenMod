using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SunHavenMuseumUtilityTracker.Data
{
    /// <summary>
    /// Resolves aquarium fish item IDs from game data at runtime.
    /// Uses ItemInfoDatabase + FishData (setSeason) via reflection to avoid Sirenix dependency.
    /// </summary>
    public static class AquariumFishResolver
    {
        private static readonly Dictionary<string, List<(int GameItemId, string Name)>> _cache = new Dictionary<string, List<(int, string)>>(StringComparer.OrdinalIgnoreCase);
        private static bool _resolutionStarted;
        private static bool _resolutionComplete;
        private static int _pendingCallbacks;

        /// <summary>
        /// Gets resolved fish items for an aquarium bundle, or null if not available.
        /// </summary>
        public static List<(int GameItemId, string Name)> GetResolvedItems(string bundleId)
        {
            if (string.IsNullOrEmpty(bundleId)) return null;
            StartResolutionIfNeeded();
            lock (_cache)
            {
                return _cache.TryGetValue(bundleId, out var list) ? new List<(int, string)>(list) : null;
            }
        }

        public static bool IsComplete => _resolutionComplete;

        public static void StartResolutionIfNeeded()
        {
            if (_resolutionStarted) return;
            _resolutionStarted = true;

            try
            {
                var itemInfoDbType = AccessTools.TypeByName("Wish.ItemInfoDatabase");
                if (itemInfoDbType == null) return;

                var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", new[] { typeof(Type) });
                var instance = findMethod?.Invoke(null, new object[] { itemInfoDbType });
                if (instance == null) return;

                var allItemSellInfos = instance.GetType().GetField("allItemSellInfos", BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
                if (allItemSellInfos == null) return;

                var itemTypeFish = Enum.Parse(AccessTools.TypeByName("Wish.ItemType"), "Fish");
                var fishIds = new List<int>();

                var dict = allItemSellInfos as System.Collections.IDictionary;
                if (dict != null)
                {
                    foreach (System.Collections.DictionaryEntry kvp in dict)
                    {
                        var info = kvp.Value;
                        if (info != null)
                        {
                            var itemTypeField = info.GetType().GetField("itemType", BindingFlags.Public | BindingFlags.Instance);
                            if (itemTypeField != null && itemTypeField.GetValue(info)?.Equals(itemTypeFish) == true)
                                fishIds.Add(Convert.ToInt32(kvp.Key));
                        }
                    }
                }

                if (fishIds.Count == 0)
                {
                    _resolutionComplete = true;
                    return;
                }

                var databaseType = AccessTools.TypeByName("PSS.Database");
                if (databaseType == null)
                {
                    _resolutionComplete = true;
                    return;
                }

                var getDataMethod = databaseType.GetMethod("GetData", BindingFlags.Public | BindingFlags.Static);
                if (getDataMethod == null)
                {
                    _resolutionComplete = true;
                    return;
                }

                var fishDataType = AccessTools.TypeByName("Wish.FishData");
                if (fishDataType == null)
                {
                    _resolutionComplete = true;
                    return;
                }

                var getDataGeneric = getDataMethod.MakeGenericMethod(fishDataType);
                var callbackParam = getDataGeneric.GetParameters()[1];
                var handlerMethod = typeof(AquariumFishResolver).GetMethod(nameof(OnFishDataLoaded), BindingFlags.NonPublic | BindingFlags.Static);
                var handlerGeneric = handlerMethod.MakeGenericMethod(fishDataType);

                _pendingCallbacks = fishIds.Count;
                foreach (var id in fishIds)
                {
                    var handler = Delegate.CreateDelegate(callbackParam.ParameterType, null, handlerGeneric);
                    getDataGeneric.Invoke(null, new object[] { id, handler, null });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[AquariumFishResolver] Error: {ex.Message}");
                _resolutionComplete = true;
            }
        }

        private static void OnFishDataLoaded<T>(T data) where T : class
        {
            try
            {
                if (data == null) return;

                var hasSetSeason = (bool)(data.GetType().GetField("hasSetSeason", BindingFlags.Public | BindingFlags.Instance)?.GetValue(data) ?? false);
                if (!hasSetSeason) return;

                var setSeason = data.GetType().GetField("setSeason", BindingFlags.Public | BindingFlags.Instance)?.GetValue(data);
                var bundleId = setSeason?.ToString() switch
                {
                    "Spring" => "MuseumAquariumSpring",
                    "Summer" => "MuseumAquariumSummer",
                    "Fall" => "MuseumAquariumFall",
                    "Winter" => "MuseumAquariumWinter",
                    _ => null
                };
                if (string.IsNullOrEmpty(bundleId)) return;

                var idField = data.GetType().GetField("id", BindingFlags.Public | BindingFlags.Instance);
                var id = idField != null ? (int)idField.GetValue(data) : 0;
                var dataName = data.GetType().GetField("name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(data)?.ToString();
                var displayName = !string.IsNullOrEmpty(dataName) ? dataName : $"Item {id}";

                lock (_cache)
                {
                    if (!_cache.TryGetValue(bundleId, out var list))
                    {
                        list = new List<(int, string)>();
                        _cache[bundleId] = list;
                    }
                    list.Add((id, displayName));
                }
            }
            finally
            {
                if (System.Threading.Interlocked.Decrement(ref _pendingCallbacks) <= 0)
                    _resolutionComplete = true;
            }
        }

        private static string GetItemName(System.Collections.IDictionary dict, int id)
        {
            try
            {
                if (dict != null && dict.Contains(id))
                {
                    var info = dict[id];
                    if (info != null)
                    {
                        var nameField = info.GetType().GetField("name", BindingFlags.Public | BindingFlags.Instance);
                        return nameField?.GetValue(info)?.ToString() ?? $"Item {id}";
                    }
                }
            }
            catch { }
            return $"Item {id}";
        }
    }
}
