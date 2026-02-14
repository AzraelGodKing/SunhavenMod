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
            return _smartChests.ContainsKey(chestId) && _smartChests[chestId].IsEnabled;
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
            catch { }
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
            catch { }
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
            catch { }
            return false;
        }

        #endregion

        #region Scan Engine

        public void ExecuteScan(int maxItemsPerScan, bool enableNotifications)
        {
            if (_smartChests.Count == 0)
                return;

            InitReflection();

            var inventories = ChestManager.inventories;
            var associatedChests = ChestManager.associatedChests;

            if (inventories == null || inventories.Count == 0)
                return;

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

            foreach (var smartChestEntry in _smartChests)
            {
                if (totalMoved >= maxItemsPerScan)
                    break;

                var smartData = smartChestEntry.Value;
                if (!smartData.IsEnabled || smartData.Rules.Count == 0)
                    continue;

                if (!chestLookup.TryGetValue(smartData.ChestId, out var targetPair))
                    continue;

                var targetInventory = targetPair.Key;
                var targetChest = targetPair.Value;

                if (IsChestInUse(targetChest))
                    continue;

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

                    int moved = TransferMatchingItems(sourceInventory, sourceChest,
                        targetInventory, targetChest, smartData.Rules,
                        maxItemsPerScan - totalMoved, enableNotifications);

                    if (moved > 0)
                    {
                        totalMoved += moved;
                        modifiedChests.Add(sourceChest);
                        modifiedChests.Add(targetChest);
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
                Plugin.Log?.LogInfo($"Smart Chest scan complete: moved {totalMoved} item stacks");
            }
        }

        private int TransferMatchingItems(Inventory sourceInv, Chest sourceChest,
            Inventory targetInv, Chest targetChest,
            List<SmartChestRule> rules, int maxItems, bool notify)
        {
            int moved = 0;
            var sourceItems = sourceInv.Items;
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
                    continue;

                if (amountToAccept <= 0)
                    continue;

                TransferItemData(sourceInv, i, targetInv, slotData.item, amountToAccept);

                if (notify)
                {
                    var sellInfo = GetItemSellInfo(slotData.id);
                    var name = sellInfo?.name ?? $"Item {slotData.id}";
                    SendNotification($"Smart Chest: {name} x{amountToAccept}");
                }

                moved++;
            }

            return moved;
        }

        private void TransferItemData(Inventory sourceInv, int sourceSlot,
            Inventory targetInv, Item item, int amount)
        {
            var sourceItems = sourceInv.Items;

            // Remove from source
            if (sourceItems[sourceSlot].amount > amount)
            {
                sourceItems[sourceSlot].amount -= amount;
            }
            else
            {
                sourceItems[sourceSlot].RemoveItem();
            }

            // Add to target
            var targetItems = targetInv.Items;
            int targetCount = Mathf.Min(targetInv.maxSlots, targetItems.Count);
            int remaining = amount;

            // Stack on existing matching items
            for (int i = 0; i < targetCount && remaining > 0; i++)
            {
                if (targetItems[i].item != null && targetItems[i].item.Equals(item) && targetItems[i].amount > 0)
                {
                    var sellInfo = GetItemSellInfo(item.ID());
                    int stackSize = sellInfo?.stackSize ?? 50;
                    int space = stackSize - targetItems[i].amount;
                    if (space > 0)
                    {
                        int toAdd = Mathf.Min(space, remaining);
                        targetItems[i].amount += toAdd;
                        remaining -= toAdd;
                    }
                }
            }

            // Place remaining in empty slots
            for (int i = 0; i < targetCount && remaining > 0; i++)
            {
                if (targetItems[i].id <= 0 || targetItems[i].item.ID() == 0)
                {
                    var sellInfo = GetItemSellInfo(item.ID());
                    int stackSize = sellInfo?.stackSize ?? 50;
                    int toAdd = Mathf.Min(stackSize, remaining);

                    targetItems[i].item = item.DeepCloneItem();
                    targetItems[i].id = item.ID();
                    targetItems[i].amount = toAdd;
                    remaining -= toAdd;
                }
            }
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

        #endregion
    }
}
