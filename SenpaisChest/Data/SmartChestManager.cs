using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Wish;

namespace SenpaisChest.Data
{
    public class SmartChestManager
    {
        private readonly Dictionary<string, SmartChestData> _smartChests = new Dictionary<string, SmartChestData>();
        private string _characterName = "";
        private bool _isDirty;

        // Cache for ItemCategory lookups (since ItemSellInfo doesn't expose it)
        private readonly Dictionary<int, string> _categoryCache = new Dictionary<int, string>();

        // Reflection cache
        private static FieldInfo _chestDataField;
        private static object _itemInfoDbInstance;
        private static FieldInfo _allItemSellInfosField;
        private static object _notificationStackInstance;
        private static MethodInfo _sendNotificationMethod;
        private static MethodInfo _databaseGetDataMethod;
        private static bool _reflectionInitialized;

        // Museum donation check cache (S.M.U.T. integration via reflection)
        private static bool _museumReflectionInitialized;
        private static bool _museumModAvailable;
        private static MethodInfo _getDonationManagerMethod;
        private static MethodInfo _hasDonatedByGameIdMethod;
        private static MethodInfo _findByGameItemIdMethod;
        private static PropertyInfo _isLoadedProperty;

        public bool IsDirty => _isDirty;

        public void LoadData(SmartChestSaveData data)
        {
            _smartChests.Clear();
            _categoryCache.Clear();

            if (data == null)
                return;

            _characterName = data.CharacterName;

            foreach (var chest in data.Chests)
            {
                _smartChests[chest.ChestId] = chest;
            }

            Plugin.Log?.LogInfo($"Loaded {_smartChests.Count} smart chest configurations");
        }

        public SmartChestSaveData GetSaveData()
        {
            return new SmartChestSaveData
            {
                CharacterName = _characterName,
                Chests = _smartChests.Values.ToList()
            };
        }

        public void SetCharacterName(string name)
        {
            _characterName = name;
        }

        public void MarkClean()
        {
            _isDirty = false;
        }

        public void MarkDirty()
        {
            _isDirty = true;
        }

        public SmartChestData GetOrCreateSmartChest(string chestId, string chestName)
        {
            if (_smartChests.TryGetValue(chestId, out var existing))
            {
                existing.ChestName = chestName;
                return existing;
            }

            var data = new SmartChestData(chestId, chestName);
            _smartChests[chestId] = data;
            _isDirty = true;
            return data;
        }

        public SmartChestData GetSmartChest(string chestId)
        {
            _smartChests.TryGetValue(chestId, out var data);
            return data;
        }

        public bool IsSmartChest(string chestId)
        {
            return _smartChests.ContainsKey(chestId)
                && _smartChests[chestId].IsEnabled
                && _smartChests[chestId].Rules.Count > 0;
        }

        public void RemoveSmartChest(string chestId)
        {
            if (_smartChests.Remove(chestId))
            {
                _isDirty = true;
            }
        }

        public static string GetChestId(Chest chest)
        {
            if (chest == null)
                return null;

            var decoration = chest as Decoration;
            if (decoration == null)
                return null;

            var pos = decoration.Position;
            return $"{pos.x}_{pos.y}_{pos.z}";
        }

        #region Reflection Helpers

        private static void InitReflection()
        {
            if (_reflectionInitialized)
                return;

            try
            {
                // Cache Chest.data field
                _chestDataField = typeof(Chest).GetField("data",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                // Cache Database.GetData<ItemData> via reflection
                var databaseType = AccessTools.TypeByName("Wish.Database") ?? AccessTools.TypeByName("Database");
                if (databaseType != null)
                {
                    var getDataGeneric = databaseType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name == "GetData" && m.IsGenericMethod && m.GetParameters().Length == 3);
                    if (getDataGeneric != null)
                    {
                        _databaseGetDataMethod = getDataGeneric.MakeGenericMethod(typeof(ItemData));
                    }
                }

                _reflectionInitialized = true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to initialize reflection: {ex.Message}");
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
            catch
            {
                return null;
            }
        }

        private static ItemSellInfo GetItemSellInfo(int itemId)
        {
            try
            {
                if (_itemInfoDbInstance == null || (_itemInfoDbInstance is UnityEngine.Object obj && obj == null))
                {
                    _itemInfoDbInstance = GetSingletonInstance("Wish.ItemInfoDatabase");
                    if (_itemInfoDbInstance != null)
                    {
                        _allItemSellInfosField = _itemInfoDbInstance.GetType()
                            .GetField("allItemSellInfos", BindingFlags.Public | BindingFlags.Instance);
                    }
                }

                if (_itemInfoDbInstance == null || _allItemSellInfosField == null)
                    return null;

                var dict = _allItemSellInfosField.GetValue(_itemInfoDbInstance) as Dictionary<int, ItemSellInfo>;
                if (dict != null && dict.ContainsKey(itemId))
                    return dict[itemId];
            }
            catch (Exception ex) { Plugin.Log?.LogDebug($"[SmartChestManager] GetItemSellInfo: {ex.Message}"); }
            return null;
        }

        private static void SendNotification(string message)
        {
            try
            {
                if (_notificationStackInstance == null || (_notificationStackInstance is UnityEngine.Object obj && obj == null))
                {
                    _notificationStackInstance = GetSingletonInstance("Wish.NotificationStack");
                    if (_notificationStackInstance != null)
                    {
                        _sendNotificationMethod = _notificationStackInstance.GetType()
                            .GetMethod("SendNotification", new[] { typeof(string), typeof(int), typeof(int), typeof(bool), typeof(bool) });
                    }
                }

                _sendNotificationMethod?.Invoke(_notificationStackInstance,
                    new object[] { message, 0, 0, true, false });
            }
            catch (Exception ex) { Plugin.Log?.LogDebug($"[SmartChestManager] SendNotification: {ex.Message}"); }
        }

        private bool IsChestInUse(Chest chest)
        {
            try
            {
                InitReflection();
                if (_chestDataField != null)
                {
                    var chestData = _chestDataField.GetValue(chest) as ChestData;
                    return chestData?.inUse ?? false;
                }
            }
            catch (Exception ex) { Plugin.Log?.LogDebug($"[SmartChestManager] IsChestInUse: {ex.Message}"); }
            return false;
        }

        /// <summary>
        /// Searches the item database by name or ID. Returns results as (ID, Name) pairs
        /// sorted with exact matches first, then starts-with, then contains.
        /// </summary>
        public static List<KeyValuePair<int, string>> SearchItems(string query, int maxResults = 20)
        {
            var results = new List<KeyValuePair<int, string>>();
            if (string.IsNullOrEmpty(query) || query.Length < 2)
                return results;

            try
            {
                if (_itemInfoDbInstance == null || (_itemInfoDbInstance is UnityEngine.Object obj && obj == null))
                {
                    _itemInfoDbInstance = GetSingletonInstance("Wish.ItemInfoDatabase");
                    if (_itemInfoDbInstance != null)
                    {
                        _allItemSellInfosField = _itemInfoDbInstance.GetType()
                            .GetField("allItemSellInfos", BindingFlags.Public | BindingFlags.Instance);
                    }
                }

                if (_itemInfoDbInstance == null || _allItemSellInfosField == null)
                    return results;

                var dict = _allItemSellInfosField.GetValue(_itemInfoDbInstance) as Dictionary<int, ItemSellInfo>;
                if (dict == null)
                    return results;

                string queryLower = query.ToLowerInvariant();
                bool isNumeric = int.TryParse(query, out int queryId);

                // Collect matches into buckets: exact, starts-with, contains
                var exact = new List<KeyValuePair<int, string>>();
                var startsWith = new List<KeyValuePair<int, string>>();
                var contains = new List<KeyValuePair<int, string>>();

                foreach (var kvp in dict)
                {
                    if (kvp.Value == null || string.IsNullOrEmpty(kvp.Value.name))
                        continue;

                    var entry = new KeyValuePair<int, string>(kvp.Key, kvp.Value.name);
                    string nameLower = kvp.Value.name.ToLowerInvariant();

                    // Exact ID match
                    if (isNumeric && kvp.Key == queryId)
                    {
                        exact.Add(entry);
                        continue;
                    }

                    // Exact name match
                    if (nameLower == queryLower)
                    {
                        exact.Add(entry);
                    }
                    else if (nameLower.StartsWith(queryLower))
                    {
                        startsWith.Add(entry);
                    }
                    else if (nameLower.Contains(queryLower))
                    {
                        contains.Add(entry);
                    }
                    else if (isNumeric && kvp.Key.ToString().Contains(query))
                    {
                        contains.Add(entry);
                    }
                }

                // Sort each bucket alphabetically by name
                startsWith.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));
                contains.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));

                results.AddRange(exact);
                results.AddRange(startsWith);
                results.AddRange(contains);

                if (results.Count > maxResults)
                    results.RemoveRange(maxResults, results.Count - maxResults);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Error searching items: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Gets a single item's name by ID. Returns null if not found.
        /// </summary>
        public static string GetItemName(int itemId)
        {
            var info = GetItemSellInfo(itemId);
            return info?.name;
        }

        #endregion

        #region Scan Engine

        public void ExecuteScan(int maxItemsPerScan, bool enableNotifications)
        {
            if (_smartChests.Count == 0)
            {
                Plugin.Log?.LogDebug("[Scan] No smart chests configured, skipping scan");
                return;
            }

            InitReflection();

            var inventories = ChestManager.inventories;
            var associatedChests = ChestManager.associatedChests;

            if (inventories == null || inventories.Count == 0)
            {
                Plugin.Log?.LogDebug("[Scan] No inventories loaded, skipping scan");
                return;
            }

            if (associatedChests == null || associatedChests.Count == 0)
            {
                Plugin.Log?.LogDebug("[Scan] No associated chests found, skipping scan");
                return;
            }

            Plugin.Log?.LogInfo($"[Scan] Starting scan: {_smartChests.Count} smart chest(s), {associatedChests.Count} associated chest(s)");

            int totalMoved = 0;
            var modifiedChests = new HashSet<Chest>();

            // Build a lookup from chestId to (Inventory, Chest)
            var chestLookup = new Dictionary<string, KeyValuePair<Inventory, Chest>>();
            foreach (var kvp in associatedChests)
            {
                var chestId = GetChestId(kvp.Value);
                if (chestId != null && !chestLookup.ContainsKey(chestId))
                {
                    chestLookup[chestId] = new KeyValuePair<Inventory, Chest>(kvp.Key, kvp.Value);
                }
            }

            Plugin.Log?.LogDebug($"[Scan] Built chest lookup: {chestLookup.Count} unique chests");

            foreach (var smartChestEntry in _smartChests)
            {
                if (totalMoved >= maxItemsPerScan)
                    break;

                var smartData = smartChestEntry.Value;
                if (!smartData.IsEnabled || smartData.Rules.Count == 0)
                {
                    Plugin.Log?.LogDebug($"[Scan] Skipping '{smartData.ChestName}' (enabled={smartData.IsEnabled}, rules={smartData.Rules.Count})");
                    continue;
                }

                if (!chestLookup.TryGetValue(smartData.ChestId, out var targetPair))
                {
                    Plugin.Log?.LogWarning($"[Scan] Smart chest '{smartData.ChestName}' (id={smartData.ChestId}) not found in loaded chests");
                    continue;
                }

                var targetInventory = targetPair.Key;
                var targetChest = targetPair.Value;

                if (IsChestInUse(targetChest))
                {
                    Plugin.Log?.LogDebug($"[Scan] Target chest '{smartData.ChestName}' is in use, skipping");
                    continue;
                }

                Plugin.Log?.LogInfo($"[Scan] Scanning for items matching '{smartData.ChestName}' ({smartData.Rules.Count} rules)");

                int sourcesScanned = 0;
                foreach (var sourceEntry in chestLookup)
                {
                    if (totalMoved >= maxItemsPerScan)
                        break;

                    if (sourceEntry.Key == smartData.ChestId)
                        continue;

                    var sourceInventory = sourceEntry.Value.Key;
                    var sourceChest = sourceEntry.Value.Value;

                    if (IsChestInUse(sourceChest))
                        continue;

                    if (IsSmartChest(sourceEntry.Key))
                        continue;

                    sourcesScanned++;
                    int moved = TransferMatchingItems(sourceInventory, sourceChest,
                        targetInventory, targetChest, smartData.Rules,
                        maxItemsPerScan - totalMoved);

                    if (moved > 0)
                    {
                        totalMoved += moved;
                        modifiedChests.Add(sourceChest);
                        modifiedChests.Add(targetChest);
                    }
                }

                Plugin.Log?.LogDebug($"[Scan] Scanned {sourcesScanned} source chests for '{smartData.ChestName}'");
            }

            // Phase 2: Eject non-matching items from smart chests
            foreach (var smartChestEntry in _smartChests)
            {
                if (totalMoved >= maxItemsPerScan)
                    break;

                var smartData = smartChestEntry.Value;
                if (!smartData.IsEnabled || smartData.Rules.Count == 0)
                    continue;

                if (!chestLookup.TryGetValue(smartData.ChestId, out var smartPair))
                    continue;

                var smartInventory = smartPair.Key;
                var smartChest = smartPair.Value;

                if (IsChestInUse(smartChest))
                    continue;

                var smartItems = smartInventory.Items;
                if (smartItems == null)
                    continue;

                int slotCount = Mathf.Min(smartInventory.maxSlots, smartItems.Count);

                for (int i = slotCount - 1; i >= 0 && totalMoved < maxItemsPerScan; i--)
                {
                    var slotData = smartItems[i];
                    if (slotData.id <= 0 || slotData.amount <= 0)
                        continue;

                    // Item matches this chest's rules — it belongs here
                    if (MatchesAnyRule(slotData.id, smartData.Rules))
                        continue;

                    var sellInfo = GetItemSellInfo(slotData.id);
                    var itemName = sellInfo?.name ?? $"Item {slotData.id}";

                    // Try to find another smart chest that wants this item
                    bool ejected = false;
                    foreach (var otherEntry in _smartChests)
                    {
                        if (otherEntry.Key == smartData.ChestId)
                            continue;
                        if (!otherEntry.Value.IsEnabled || otherEntry.Value.Rules.Count == 0)
                            continue;
                        if (!MatchesAnyRule(slotData.id, otherEntry.Value.Rules))
                            continue;
                        if (!chestLookup.TryGetValue(otherEntry.Value.ChestId, out var destPair))
                            continue;
                        if (IsChestInUse(destPair.Value))
                            continue;

                        int amountToAccept;
                        if (!destPair.Key.CanAcceptItem(slotData.item, slotData.amount, out amountToAccept) || amountToAccept <= 0)
                            continue;

                        Plugin.Log?.LogInfo($"[Scan] Ejecting {itemName} x{amountToAccept} from '{smartData.ChestName}' to smart chest '{otherEntry.Value.ChestName}'");
                        TransferItemData(smartInventory, i, destPair.Key, slotData.item, amountToAccept);
                        modifiedChests.Add(smartChest);
                        modifiedChests.Add(destPair.Value);
                        totalMoved++;
                        ejected = true;
                        break;
                    }

                    if (ejected)
                        continue;

                    // No matching smart chest — send to any normal chest with space
                    foreach (var normalEntry in chestLookup)
                    {
                        if (IsSmartChest(normalEntry.Key))
                            continue;
                        if (IsChestInUse(normalEntry.Value.Value))
                            continue;

                        int amountToAccept;
                        if (!normalEntry.Value.Key.CanAcceptItem(slotData.item, slotData.amount, out amountToAccept) || amountToAccept <= 0)
                            continue;

                        Plugin.Log?.LogInfo($"[Scan] Ejecting {itemName} x{amountToAccept} from '{smartData.ChestName}' to normal chest");
                        TransferItemData(smartInventory, i, normalEntry.Value.Key, slotData.item, amountToAccept);
                        modifiedChests.Add(smartChest);
                        modifiedChests.Add(normalEntry.Value.Value);
                        totalMoved++;
                        break;
                    }
                }
            }

            // Persist changes for all modified chests
            foreach (var chest in modifiedChests)
            {
                try
                {
                    chest.SaveMeta();
                    chest.SendNewMeta(((Decoration)chest).meta);
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogError($"Error saving chest meta: {ex.Message}");
                }
            }

            if (totalMoved > 0)
            {
                Plugin.Log?.LogInfo($"[Scan] Complete: sorted {totalMoved} item stack(s)");
                if (enableNotifications)
                {
                    SendNotification("Senpai's Chest: Sorted");
                }
            }
            else
            {
                Plugin.Log?.LogInfo("[Scan] Complete: no items to sort");
            }
        }

        private int TransferMatchingItems(Inventory sourceInv, Chest sourceChest,
            Inventory targetInv, Chest targetChest,
            List<SmartChestRule> rules, int maxItems)
        {
            int moved = 0;
            var sourceItems = sourceInv.Items;
            if (sourceItems == null)
                return 0;

            int count = Mathf.Min(sourceInv.maxSlots, sourceItems.Count);

            for (int i = count - 1; i >= 0 && moved < maxItems; i--)
            {
                var slotData = sourceItems[i];
                if (slotData.id <= 0 || slotData.amount <= 0)
                    continue;

                if (!MatchesAnyRule(slotData.id, rules))
                    continue;

                int amountToAccept;
                if (!targetInv.CanAcceptItem(slotData.item, slotData.amount, out amountToAccept))
                {
                    Plugin.Log?.LogDebug($"[Scan] Target cannot accept item {slotData.id} (amount={slotData.amount})");
                    continue;
                }

                if (amountToAccept <= 0)
                    continue;

                var sellInfo = GetItemSellInfo(slotData.id);
                var itemName = sellInfo?.name ?? $"Item {slotData.id}";
                Plugin.Log?.LogInfo($"[Scan] Moving {itemName} x{amountToAccept}");

                TransferItemData(sourceInv, i, targetInv, slotData.item, amountToAccept);
                moved++;
            }

            return moved;
        }

        private void TransferItemData(Inventory sourceInv, int sourceSlot,
            Inventory targetInv, Item item, int amount)
        {
            // Use the game's own Inventory API — handles stacking, empty slots, and UI updates
            targetInv.AddItem(item, amount, false);
            sourceInv.RemoveItemAt(sourceSlot, amount);
        }

        #endregion

        #region Rule Matching

        private bool MatchesAnyRule(int itemId, List<SmartChestRule> rules)
        {
            foreach (var rule in rules)
            {
                if (MatchesRule(itemId, rule))
                    return true;
            }
            return false;
        }

        private bool MatchesRule(int itemId, SmartChestRule rule)
        {
            switch (rule.Type)
            {
                case RuleType.ByItemId:
                    return itemId == rule.ItemId;

                case RuleType.ByItemType:
                    var sellInfo = GetItemSellInfo(itemId);
                    if (sellInfo == null) return false;
                    return sellInfo.itemType.ToString() == rule.ItemTypeName;

                case RuleType.ByProperty:
                    if (rule.PropertyName == "isNotDonated")
                        return IsMuseumItemNotDonated(itemId);

                    var info = GetItemSellInfo(itemId);
                    if (info == null) return false;
                    switch (rule.PropertyName)
                    {
                        case "isGem": return info.isGem;
                        case "isForageable": return info.isForageable;
                        case "isAnimalProduct": return info.isAnimalProduct;
                        case "isMeal": return info.isMeal;
                        case "isFruit": return info.isFruit;
                        case "isArtisanryItem": return info.isArtisanryItem;
                        case "isPotion": return info.isPotion;
                        default: return false;
                    }

                case RuleType.ByCategory:
                    return MatchesCategory(itemId, rule.CategoryName);

                default:
                    return false;
            }
        }

        private bool MatchesCategory(int itemId, string categoryName)
        {
            if (_categoryCache.TryGetValue(itemId, out var cached))
            {
                return cached == categoryName;
            }

            // Use reflection to call Database.GetData<ItemData>
            bool matched = false;
            try
            {
                if (_databaseGetDataMethod != null)
                {
                    Action<ItemData> callback = itemData =>
                    {
                        if (itemData != null)
                        {
                            _categoryCache[itemId] = itemData.category.ToString();
                            matched = itemData.category.ToString() == categoryName;
                        }
                    };

                    _databaseGetDataMethod.Invoke(null, new object[] { itemId, callback, null });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Failed to lookup category for item {itemId}: {ex.Message}");
            }

            return matched;
        }

        /// <summary>
        /// Checks if an item is a museum item that hasn't been donated yet.
        /// Uses reflection to access S.M.U.T. (soft dependency).
        /// </summary>
        private static bool IsMuseumItemNotDonated(int itemId)
        {
            if (!_museumReflectionInitialized)
            {
                _museumReflectionInitialized = true;
                try
                {
                    var pluginType = AccessTools.TypeByName("SunHavenMuseumUtilityTracker.Plugin");
                    if (pluginType != null)
                    {
                        _getDonationManagerMethod = pluginType.GetMethod("GetDonationManager",
                            BindingFlags.Public | BindingFlags.Static);

                        var contentType = AccessTools.TypeByName("SunHavenMuseumUtilityTracker.Data.MuseumContent");
                        if (contentType != null)
                        {
                            _findByGameItemIdMethod = contentType.GetMethod("FindByGameItemId",
                                BindingFlags.Public | BindingFlags.Static);
                        }

                        _museumModAvailable = _getDonationManagerMethod != null && _findByGameItemIdMethod != null;

                        if (_museumModAvailable)
                            Plugin.Log?.LogInfo("[Museum] S.M.U.T. integration available for isNotDonated rule");
                        else
                            Plugin.Log?.LogWarning("[Museum] S.M.U.T. found but missing expected methods");
                    }
                    else
                    {
                        Plugin.Log?.LogInfo("[Museum] S.M.U.T. not installed — isNotDonated rule will not match any items");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"[Museum] Failed to initialize S.M.U.T. reflection: {ex.Message}");
                }
            }

            if (!_museumModAvailable)
                return false;

            try
            {
                // Check if it's a museum item
                var museumItem = _findByGameItemIdMethod.Invoke(null, new object[] { itemId });
                if (museumItem == null)
                    return false;

                // Get the donation manager (re-fetch each time in case it was recreated)
                var donationManager = _getDonationManagerMethod.Invoke(null, null);
                if (donationManager == null)
                    return false;

                // Cache the HasDonatedByGameId method if needed
                if (_hasDonatedByGameIdMethod == null)
                {
                    _hasDonatedByGameIdMethod = donationManager.GetType().GetMethod("HasDonatedByGameId",
                        BindingFlags.Public | BindingFlags.Instance);
                    if (_hasDonatedByGameIdMethod == null)
                        return false;

                    // Also get IsLoaded property
                    _isLoadedProperty = donationManager.GetType().GetProperty("IsLoaded",
                        BindingFlags.Public | BindingFlags.Instance);
                }

                // Check if donation data is loaded
                if (_isLoadedProperty != null)
                {
                    var isLoaded = (bool)_isLoadedProperty.GetValue(donationManager);
                    if (!isLoaded)
                        return false;
                }

                // Check if NOT donated
                var isDonated = (bool)_hasDonatedByGameIdMethod.Invoke(donationManager, new object[] { itemId });
                return !isDonated;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}