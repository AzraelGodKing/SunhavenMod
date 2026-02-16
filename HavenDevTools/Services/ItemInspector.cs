using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Wish;

namespace HavenDevTools.Services
{
    /// <summary>
    /// Service for inspecting and looking up item IDs in Sun Haven
    /// </summary>
    public class ItemInspector
    {
        private Dictionary<int, GameItemInfo> _itemCache = new Dictionary<int, GameItemInfo>();
        private Dictionary<string, int> _nameToIdCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private bool _cacheBuilt = false;

        public ItemInspector()
        {
            Plugin.Log?.LogInfo("[ItemInspector] Initialized");
        }

        /// <summary>
        /// Build the item cache from the game's item database
        /// </summary>
        public void BuildCache()
        {
            if (_cacheBuilt) return;

            try
            {
                // Try to get the ItemDatabase
                var databaseType = AccessTools.TypeByName("Wish.ItemDatabase") ?? AccessTools.TypeByName("ItemDatabase");
                if (databaseType == null)
                {
                    Plugin.Log?.LogWarning("[ItemInspector] Could not find ItemDatabase type");
                    return;
                }

                // Get the instance
                var instanceProp = databaseType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp == null)
                {
                    Plugin.Log?.LogWarning("[ItemInspector] Could not find ItemDatabase.Instance");
                    return;
                }

                var database = instanceProp.GetValue(null);
                if (database == null)
                {
                    Plugin.Log?.LogWarning("[ItemInspector] ItemDatabase.Instance is null");
                    return;
                }

                // Try to get the items dictionary/list
                var itemsField = databaseType.GetField("items", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (itemsField == null)
                {
                    itemsField = databaseType.GetField("_items", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (itemsField != null)
                {
                    var items = itemsField.GetValue(database);
                    if (items is IDictionary<int, object> itemDict)
                    {
                        foreach (var kvp in itemDict)
                        {
                            CacheItem(kvp.Key, kvp.Value);
                        }
                    }
                    else if (items is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            var idProp = item.GetType().GetProperty("id") ?? item.GetType().GetProperty("ID");
                            if (idProp != null)
                            {
                                int id = (int)idProp.GetValue(item);
                                CacheItem(id, item);
                            }
                        }
                    }
                }

                _cacheBuilt = true;
                Plugin.Log?.LogInfo($"[ItemInspector] Built cache with {_itemCache.Count} items");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[ItemInspector] Error building cache: {ex.Message}");
            }
        }

        private void CacheItem(int id, object item)
        {
            try
            {
                var type = item.GetType();
                var nameProp = type.GetProperty("name") ?? type.GetProperty("Name") ?? type.GetProperty("itemName");
                var descProp = type.GetProperty("description") ?? type.GetProperty("Description");
                var categoryProp = type.GetProperty("category") ?? type.GetProperty("Category");
                var sellPriceProp = type.GetProperty("sellPrice") ?? type.GetProperty("SellPrice");

                string name = nameProp?.GetValue(item)?.ToString() ?? "Unknown";
                string description = descProp?.GetValue(item)?.ToString() ?? "";
                string category = categoryProp?.GetValue(item)?.ToString() ?? "Unknown";
                int sellPrice = sellPriceProp != null ? Convert.ToInt32(sellPriceProp.GetValue(item)) : 0;

                var info = new GameItemInfo
                {
                    Id = id,
                    Name = name,
                    Description = description,
                    Category = category,
                    SellPrice = sellPrice
                };

                _itemCache[id] = info;
                if (!_nameToIdCache.ContainsKey(name))
                {
                    _nameToIdCache[name] = id;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[ItemInspector] Error caching item {id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get item info by ID
        /// </summary>
        public GameItemInfo GetItemById(int id)
        {
            if (!_cacheBuilt) BuildCache();

            if (_itemCache.TryGetValue(id, out var info))
            {
                return info;
            }

            return TryGetItemFromDatabase(id);
        }

        /// <summary>
        /// Search items by name (partial match)
        /// </summary>
        public List<GameItemInfo> SearchByName(string searchTerm)
        {
            if (!_cacheBuilt) BuildCache();

            var results = new List<GameItemInfo>();
            string lowerSearch = searchTerm.ToLower();

            foreach (var item in _itemCache.Values)
            {
                if (item.Name.ToLower().Contains(lowerSearch))
                {
                    results.Add(item);
                }
            }

            return results;
        }

        /// <summary>
        /// Get item ID by exact name
        /// </summary>
        public int? GetIdByName(string name)
        {
            if (!_cacheBuilt) BuildCache();

            if (_nameToIdCache.TryGetValue(name, out int id))
            {
                return id;
            }
            return null;
        }

        /// <summary>
        /// Get all cached items
        /// </summary>
        public IEnumerable<GameItemInfo> GetAllItems()
        {
            if (!_cacheBuilt) BuildCache();
            return _itemCache.Values;
        }

        /// <summary>
        /// Get the item currently held by the player
        /// </summary>
        public GameItemInfo GetHeldItem()
        {
            try
            {
                if (Player.Instance == null || Player.Instance.Inventory == null)
                    return null;

                var inventory = Player.Instance.Inventory;
                var invType = inventory.GetType();

                // Try to get selected slot from property or field
                int slotIndex = 0;
                var selectedSlotProp = invType.GetProperty("SelectedSlot") ?? invType.GetProperty("selectedSlot");
                if (selectedSlotProp != null)
                {
                    slotIndex = (int)selectedSlotProp.GetValue(inventory);
                }
                else
                {
                    var selectedSlotField = invType.GetField("selectedSlot", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (selectedSlotField != null)
                    {
                        slotIndex = (int)selectedSlotField.GetValue(inventory);
                    }
                    else
                    {
                        return null;
                    }
                }

                var getItemMethod = AccessTools.Method(inventory.GetType(), "GetItem", new[] { typeof(int) });
                if (getItemMethod != null)
                {
                    var item = getItemMethod.Invoke(inventory, new object[] { slotIndex });
                    if (item != null)
                    {
                        var idProp = item.GetType().GetProperty("id") ?? item.GetType().GetProperty("ID");
                        if (idProp != null)
                        {
                            int id = (int)idProp.GetValue(item);
                            return GetItemById(id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[ItemInspector] Error getting held item: {ex.Message}");
            }

            return null;
        }

        private GameItemInfo TryGetItemFromDatabase(int id)
        {
            try
            {
                var databaseType = AccessTools.TypeByName("Wish.ItemDatabase") ?? AccessTools.TypeByName("ItemDatabase");
                if (databaseType == null) return null;

                var getItemMethod = AccessTools.Method(databaseType, "GetItem", new[] { typeof(int) });
                if (getItemMethod == null) return null;

                var instanceProp = databaseType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                var database = instanceProp?.GetValue(null);
                if (database == null) return null;

                var item = getItemMethod.Invoke(database, new object[] { id });
                if (item != null)
                {
                    CacheItem(id, item);
                    return _itemCache.TryGetValue(id, out var info) ? info : null;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[ItemInspector] GetItemInfo: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Spawn an item to the player's inventory
        /// </summary>
        public bool SpawnItem(int itemId, int amount = 1)
        {
            try
            {
                if (Player.Instance == null || Player.Instance.Inventory == null)
                {
                    Plugin.Log?.LogWarning("[ItemInspector] Player or inventory not available");
                    return false;
                }

                var inventory = Player.Instance.Inventory;
                var addMethod = AccessTools.Method(inventory.GetType(), "AddItem",
                    new[] { typeof(int), typeof(int), typeof(bool) });

                if (addMethod != null)
                {
                    addMethod.Invoke(inventory, new object[] { itemId, amount, true });
                    Plugin.Log?.LogInfo($"[ItemInspector] Spawned {amount}x item {itemId}");
                    return true;
                }
                else
                {
                    Plugin.Log?.LogWarning("[ItemInspector] Could not find AddItem method");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[ItemInspector] Spawn error: {ex.Message}");
            }

            return false;
        }

        public int CacheCount => _itemCache.Count;
        public bool IsCacheBuilt => _cacheBuilt;
    }

    public class GameItemInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public int SellPrice { get; set; }

        public override string ToString()
        {
            return $"[{Id}] {Name} ({Category}) - {SellPrice}g";
        }
    }
}
