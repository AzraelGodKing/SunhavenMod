using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SunhavenMods.Shared;
using Wish;

namespace HavenDevTools.Services
{
    /// <summary>
    /// Item search (shared ItemSearch) and spawn. Search + Spawn for Haven Dev Tools.
    /// </summary>
    public class ItemInspector
    {
        private static MethodInfo _cachedAddItemMethod;
        private static int _addItemArgCount;

        public ItemInspector()
        {
            Plugin.Log?.LogInfo("[ItemInspector] Initialized");
        }

        /// <summary>
        /// Search items by name or ID. Uses shared ItemSearch.
        /// </summary>
        public static List<KeyValuePair<int, string>> SearchItems(string query, int maxResults = 50)
        {
            return ItemSearch.SearchItems(query ?? "", maxResults);
        }

        /// <summary>
        /// Get item name by ID. Uses shared ItemSearch.
        /// </summary>
        public static string GetItemName(int itemId)
        {
            return ItemSearch.GetItemName(itemId);
        }

        /// <summary>
        /// Spawn item to player inventory. Tries AddItem(int,int,bool) then AddItem(int,int).
        /// </summary>
        public bool SpawnItem(int itemId, int amount = 1)
        {
            try
            {
                if (Player.Instance == null)
                {
                    Plugin.Log?.LogWarning("[ItemInspector] Player.Instance is null");
                    return false;
                }

                var inventory = Player.Instance.Inventory;
                if (inventory == null)
                {
                    Plugin.Log?.LogWarning("[ItemInspector] Player.Inventory is null");
                    return false;
                }

                var method = GetAddItemMethod(inventory);
                if (method == null)
                {
                    Plugin.Log?.LogWarning("[ItemInspector] AddItem method not found");
                    return false;
                }

                if (_addItemArgCount == 3)
                    method.Invoke(inventory, new object[] { itemId, amount, true });
                else
                    method.Invoke(inventory, new object[] { itemId, amount });

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[ItemInspector] Spawn error: {ex.Message}");
                return false;
            }
        }

        private static MethodInfo GetAddItemMethod(object inventory)
        {
            if (_cachedAddItemMethod != null) return _cachedAddItemMethod;
            try
            {
                var invType = inventory.GetType();
                _cachedAddItemMethod = AccessTools.Method(invType, "AddItem", new[] { typeof(int), typeof(int), typeof(bool) });
                if (_cachedAddItemMethod != null)
                {
                    _addItemArgCount = 3;
                    return _cachedAddItemMethod;
                }
                _cachedAddItemMethod = AccessTools.Method(invType, "AddItem", new[] { typeof(int), typeof(int) });
                if (_cachedAddItemMethod != null)
                {
                    _addItemArgCount = 2;
                    return _cachedAddItemMethod;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Get held item info (for display).
        /// </summary>
        public (int id, string name)? GetHeldItem()
        {
            try
            {
                if (Player.Instance?.Inventory == null) return null;
                var inv = Player.Instance.Inventory;
                var invType = inv.GetType();

                int slotIndex = 0;
                var slotProp = invType.GetProperty("SelectedSlot") ?? invType.GetProperty("selectedSlot");
                if (slotProp != null)
                    slotIndex = (int)slotProp.GetValue(inv);
                else
                {
                    var slotField = invType.GetField("selectedSlot", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (slotField != null)
                        slotIndex = (int)slotField.GetValue(inv);
                    else return null;
                }

                var getItemMethod = AccessTools.Method(invType, "GetItem", new[] { typeof(int) });
                if (getItemMethod == null) return null;

                var item = getItemMethod.Invoke(inv, new object[] { slotIndex });
                if (item == null) return null;

                var idProp = item.GetType().GetProperty("id") ?? item.GetType().GetProperty("ID");
                if (idProp == null) return null;

                int id = (int)idProp.GetValue(item);
                string name = GetItemName(id) ?? $"Item {id}";
                return (id, name);
            }
            catch { return null; }
        }
    }
}
