using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CropOptimizer.Data;
using HarmonyLib;
using UnityEngine;

namespace CropOptimizer.Patches
{
    internal static class CropGrowthPatch
    {
        private static CropForecast _forecast;
        private static bool _growthMembersResolved;
        private static MemberInfo _daysLeftMember;
        private static MemberInfo _currentStageMember;
        private static MemberInfo _totalGrowthMember;
        private static MemberInfo _growthDaysMember;

        private static bool _itemMembersResolved;
        private static MemberInfo _itemIdMember;
        private static Type _itemDatabaseType;
        private static MethodInfo _itemDatabaseGetItemMethod;
        private static object _itemDatabaseItemsContainer;
        private static MethodInfo _dictTryGetValueMethod;
        private static PropertyInfo _dictIndexerProperty;
        private static Type _itemInfoDatabaseType;
        private static PropertyInfo _itemInfoDatabaseInstanceProperty;
        private static FieldInfo _allItemSellInfosField;

        private static bool _qualityMembersResolved;
        private static MemberInfo _cropDataMember;
        private static MemberInfo _qualityMember;

        private static bool _loggedEtaFallback;
        private static bool _loggedSellFallback;
        private static bool _loggedQualityFallback;

        public static void Apply(Harmony harmony, CropForecast forecast)
        {
            _forecast = forecast;
            Type cropType = AccessTools.TypeByName("Wish.Crop");
            if (cropType == null)
            {
                Plugin.Log?.LogWarning("[CropGrowthPatch] Wish.Crop not found");
                return;
            }

            // Sun Haven's Wish.Crop does not expose UpdateGrowth/GrowCrop; growth runs through
            // SetMeta/SetCropSprite/Water/Grow. Patch every resolved entry point once.
            var methodsToPatch = new List<MethodInfo>();
            void TryAdd(MethodInfo m)
            {
                if (m != null)
                    methodsToPatch.Add(m);
            }

            TryAdd(AccessTools.Method(cropType, "SetCropSprite", Type.EmptyTypes));
            var decorationDataType = AccessTools.TypeByName("Wish.DecorationPositionData");
            if (decorationDataType != null)
                TryAdd(AccessTools.Method(cropType, "SetMeta", new[] { decorationDataType }));
            TryAdd(AccessTools.Method(cropType, "Water", Type.EmptyTypes));
            TryAdd(AccessTools.Method(cropType, "Grow", new[] { typeof(float) }));
            // Legacy / alternate builds (harmless if absent)
            TryAdd(AccessTools.Method(cropType, "UpdateGrowth", Type.EmptyTypes));
            TryAdd(AccessTools.Method(cropType, "GrowCrop", Type.EmptyTypes));

            methodsToPatch = methodsToPatch
                .GroupBy(m => m.MetadataToken)
                .Select(g => g.First())
                .ToList();

            if (methodsToPatch.Count == 0)
            {
                Plugin.Log?.LogError("[CropGrowthPatch] No Crop methods found to patch — forecast will stay empty.");
                return;
            }

            var postfix = AccessTools.Method(typeof(CropGrowthPatch), nameof(OnAfterCropGrowth));
            if (postfix == null)
            {
                Plugin.Log?.LogWarning("[CropGrowthPatch] Postfix method not found");
                return;
            }

            int patched = 0;
            foreach (var method in methodsToPatch)
            {
                try
                {
                    harmony.Patch(method, postfix: new HarmonyMethod(postfix));
                    patched++;
                    Plugin.Log?.LogInfo($"[CropGrowthPatch] Patched {cropType.Name}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})");
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"[CropGrowthPatch] Skipped {method.Name}: {ex.Message}");
                }
            }

            if (patched == 0)
                Plugin.Log?.LogError("[CropGrowthPatch] No Harmony patches applied — check game version compatibility.");
        }

        private static void OnAfterCropGrowth(object __instance)
        {
            if (__instance == null || _forecast == null)
                return;

            try
            {
                var unityObj = __instance as UnityEngine.Object;
                if (unityObj == null)
                    return;

                int id = unityObj.GetInstanceID();
                float etaHours = TryResolveEtaHours(__instance, out bool etaResolved) ? Mathf.Max(0f, _resolvedEtaHoursCache) : 24f;
                float qualityMultiplier = TryResolveQualityMultiplier(__instance, out bool qualityResolved) ? _resolvedQualityMultiplierCache : 1f;
                int projectedSellGold = TryResolveProjectedSellGold(__instance, qualityMultiplier, out bool sellResolved) ? _resolvedProjectedSellGoldCache : 0;

                if (!etaResolved && !_loggedEtaFallback)
                {
                    _loggedEtaFallback = true;
                    Plugin.Log?.LogDebug("[CropGrowthPatch] ETA reflection fallback active; using default 24h for unresolved crops.");
                }
                if (!qualityResolved && !_loggedQualityFallback)
                {
                    _loggedQualityFallback = true;
                    Plugin.Log?.LogDebug("[CropGrowthPatch] Quality reflection fallback active; using default multiplier 1.0.");
                }
                if (!sellResolved && !_loggedSellFallback)
                {
                    _loggedSellFallback = true;
                    Plugin.Log?.LogDebug("[CropGrowthPatch] Sell value reflection fallback active; using default 0g for unresolved crops.");
                }

                _forecast.UpdateCropState(id, etaHours, qualityMultiplier, projectedSellGold);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[CropGrowthPatch] Update failed: {ex.Message}");
            }
        }

        private static float _resolvedEtaHoursCache;
        private static float _resolvedQualityMultiplierCache;
        private static int _resolvedProjectedSellGoldCache;

        private static bool TryResolveEtaHours(object cropInstance, out bool resolved)
        {
            resolved = false;
            _resolvedEtaHoursCache = 24f;
            if (cropInstance == null)
                return false;

            try
            {
                EnsureGrowthMembers(cropInstance.GetType());

                float daysLeft;
                if (TryReadNumericMember(cropInstance, _daysLeftMember, out daysLeft))
                {
                    _resolvedEtaHoursCache = Mathf.Max(0f, daysLeft * 24f);
                    resolved = true;
                    return true;
                }

                float currentStage;
                float totalGrowth;
                if (TryReadNumericMember(cropInstance, _currentStageMember, out currentStage) &&
                    TryReadNumericMember(cropInstance, _totalGrowthMember, out totalGrowth))
                {
                    _resolvedEtaHoursCache = Mathf.Max(0f, (totalGrowth - currentStage) * 24f);
                    resolved = true;
                    return true;
                }

                float growthDays;
                if (TryReadNumericMember(cropInstance, _growthDaysMember, out growthDays))
                {
                    _resolvedEtaHoursCache = Mathf.Max(0f, growthDays * 24f);
                    resolved = true;
                    return true;
                }
            }
            catch
            {
                // per-value fallback handled by caller
            }
            return false;
        }

        private static bool TryResolveQualityMultiplier(object cropInstance, out bool resolved)
        {
            resolved = false;
            _resolvedQualityMultiplierCache = 1f;
            if (cropInstance == null)
                return false;

            try
            {
                EnsureQualityMembers(cropInstance.GetType());
                object qualityValue = null;
                if (!TryReadMemberValue(cropInstance, _qualityMember, out qualityValue) || qualityValue == null)
                {
                    if (!TryReadMemberValue(cropInstance, _cropDataMember, out object cropDataObj) || cropDataObj == null)
                        return false;
                    if (!TryReadMemberValue(cropDataObj, _qualityMember, out qualityValue) || qualityValue == null)
                        return false;
                }

                _resolvedQualityMultiplierCache = MapQualityMultiplier(qualityValue);
                resolved = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryResolveProjectedSellGold(object cropInstance, float qualityMultiplier, out bool resolved)
        {
            resolved = false;
            _resolvedProjectedSellGoldCache = 0;
            if (cropInstance == null)
                return false;

            try
            {
                EnsureItemMembers(cropInstance.GetType());
                if (!TryReadIntMember(cropInstance, _itemIdMember, out int itemId) || itemId <= 0)
                    return false;

                if (!TryGetBaseSellPrice(itemId, out int baseSellPrice))
                    return false;

                _resolvedProjectedSellGoldCache = Mathf.Max(0, Mathf.RoundToInt(baseSellPrice * Mathf.Max(0f, qualityMultiplier)));
                resolved = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureGrowthMembers(Type cropType)
        {
            if (_growthMembersResolved || cropType == null)
                return;
            _growthMembersResolved = true;
            _daysLeftMember = FindMember(cropType, "daysLeft", "DaysLeft", "remainingDays", "daysRemaining");
            _currentStageMember = FindMember(cropType, "currentGrowthStage", "growthStage", "_growthStage", "_stage", "stage");
            _totalGrowthMember = FindMember(cropType, "totalGrowthTime", "daysToGrow");
            _growthDaysMember = FindMember(cropType, "growthDays");
        }

        private static void EnsureQualityMembers(Type cropType)
        {
            if (_qualityMembersResolved || cropType == null)
                return;
            _qualityMembersResolved = true;
            _qualityMember = FindMember(cropType, "quality", "cropQuality", "_quality");
            _cropDataMember = FindMember(cropType, "data", "Data", "_data");
            if (_qualityMember == null)
            {
                if (_cropDataMember != null && TryReadMemberType(_cropDataMember, out Type dataType))
                    _qualityMember = FindMember(dataType, "quality", "cropQuality", "_quality");
            }
        }

        private static void EnsureItemMembers(Type cropType)
        {
            if (_itemMembersResolved || cropType == null)
                return;
            _itemMembersResolved = true;

            // Do not use bare "id" on Crop — it resolves to Decoration.id (world decoration id), not harvest item id.
            _itemIdMember = FindMember(cropType, "itemID", "_itemId", "cropItemId", "ItemID");
            if (_itemIdMember == null)
            {
                var cropItemMember = FindMember(cropType, "_cropItem", "cropItem", "item");
                if (cropItemMember != null && TryReadMemberType(cropItemMember, out Type itemType))
                    _itemIdMember = FindMember(itemType, "id", "itemID", "ItemID", "_itemId");
            }

            _itemDatabaseType = AccessTools.TypeByName("Wish.ItemDatabase");
            if (_itemDatabaseType != null)
            {
                _itemDatabaseGetItemMethod = AccessTools.Method(_itemDatabaseType, "GetItem", new[] { typeof(int) })
                                         ?? AccessTools.Method(_itemDatabaseType, "GetItemData", new[] { typeof(int) });

                var itemsField = AccessTools.Field(_itemDatabaseType, "Items")
                               ?? AccessTools.Field(_itemDatabaseType, "items")
                               ?? AccessTools.Field(_itemDatabaseType, "_items");
                if (itemsField != null)
                {
                    _itemDatabaseItemsContainer = itemsField.GetValue(null);
                    if (_itemDatabaseItemsContainer != null)
                    {
                        Type containerType = _itemDatabaseItemsContainer.GetType();
                        _dictTryGetValueMethod = containerType.GetMethod("TryGetValue", BindingFlags.Instance | BindingFlags.Public);
                        _dictIndexerProperty = containerType.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public);
                    }
                }
            }

            _itemInfoDatabaseType = AccessTools.TypeByName("Wish.ItemInfoDatabase");
            if (_itemInfoDatabaseType != null)
            {
                _itemInfoDatabaseInstanceProperty = AccessTools.Property(_itemInfoDatabaseType, "Instance");
                _allItemSellInfosField = AccessTools.Field(_itemInfoDatabaseType, "allItemSellInfos");
            }
        }

        private static bool TryGetBaseSellPrice(int itemId, out int sellPrice)
        {
            sellPrice = 0;
            try
            {
                if (_itemDatabaseGetItemMethod != null)
                {
                    object itemData = _itemDatabaseGetItemMethod.Invoke(null, new object[] { itemId });
                    if (TryExtractSellPrice(itemData, out sellPrice))
                        return true;
                }

                if (_itemDatabaseItemsContainer != null)
                {
                    object itemData = null;
                    if (_dictTryGetValueMethod != null)
                    {
                        var args = new object[] { itemId, null };
                        bool found = (bool)_dictTryGetValueMethod.Invoke(_itemDatabaseItemsContainer, args);
                        if (found)
                            itemData = args[1];
                    }
                    else if (_dictIndexerProperty != null)
                    {
                        itemData = _dictIndexerProperty.GetValue(_itemDatabaseItemsContainer, new object[] { itemId });
                    }
                    if (TryExtractSellPrice(itemData, out sellPrice))
                        return true;
                }

                if (_itemInfoDatabaseInstanceProperty != null && _allItemSellInfosField != null)
                {
                    object instance = _itemInfoDatabaseInstanceProperty.GetValue(null);
                    object dict = instance != null ? _allItemSellInfosField.GetValue(instance) : null;
                    if (dict != null)
                    {
                        var tryGetValue = dict.GetType().GetMethod("TryGetValue", BindingFlags.Instance | BindingFlags.Public);
                        if (tryGetValue != null)
                        {
                            var args = new object[] { itemId, null };
                            bool found = (bool)tryGetValue.Invoke(dict, args);
                            if (found && TryExtractSellPrice(args[1], out sellPrice))
                                return true;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryExtractSellPrice(object itemData, out int sellPrice)
        {
            sellPrice = 0;
            if (itemData == null)
                return false;

            var type = itemData.GetType();
            var member = FindMember(type, "sellPrice", "sellGold", "sell", "price", "Price");
            if (member == null)
                return false;

            object raw = GetMemberValue(itemData, member);
            if (raw == null)
                return false;

            try
            {
                if (raw is float f)
                    sellPrice = Mathf.Max(0, Mathf.RoundToInt(f));
                else if (raw is double d)
                    sellPrice = Mathf.Max(0, Mathf.RoundToInt((float)d));
                else
                    sellPrice = Mathf.Max(0, Mathf.RoundToInt(Convert.ToSingle(raw)));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static float MapQualityMultiplier(object qualityValue)
        {
            if (qualityValue == null)
                return 1f;

            string text = qualityValue.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (text.IndexOf("gold", StringComparison.OrdinalIgnoreCase) >= 0)
                    return 2f;
                if (text.IndexOf("silver", StringComparison.OrdinalIgnoreCase) >= 0)
                    return 1.5f;
            }

            if (TryConvertToInt(qualityValue, out int code))
            {
                if (code >= 2) return 2f;
                if (code == 1) return 1.5f;
            }

            return 1f;
        }

        private static MemberInfo FindMember(Type type, params string[] names)
        {
            if (type == null || names == null)
                return null;
            foreach (var name in names)
            {
                var field = AccessTools.Field(type, name);
                if (field != null) return field;
                var prop = AccessTools.Property(type, name);
                if (prop != null) return prop;
            }
            return null;
        }

        private static bool TryReadMemberType(MemberInfo member, out Type type)
        {
            type = null;
            if (member is FieldInfo fi)
            {
                type = fi.FieldType;
                return type != null;
            }
            if (member is PropertyInfo pi)
            {
                type = pi.PropertyType;
                return type != null;
            }
            return false;
        }

        private static bool TryReadMemberValue(object instance, MemberInfo member, out object value)
        {
            value = null;
            if (instance == null || member == null)
                return false;
            value = GetMemberValue(instance, member);
            return value != null;
        }

        private static object GetMemberValue(object instance, MemberInfo member)
        {
            try
            {
                if (member is FieldInfo fi)
                    return fi.GetValue(instance);
                if (member is PropertyInfo pi)
                    return pi.GetValue(instance, null);
            }
            catch
            {
            }
            return null;
        }

        private static bool TryReadNumericMember(object instance, MemberInfo member, out float value)
        {
            value = 0f;
            if (!TryReadMemberValue(instance, member, out object raw))
                return false;
            try
            {
                value = Convert.ToSingle(raw);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadIntMember(object instance, MemberInfo member, out int value)
        {
            value = 0;
            if (!TryReadMemberValue(instance, member, out object raw))
                return false;
            return TryConvertToInt(raw, out value);
        }

        private static bool TryConvertToInt(object raw, out int value)
        {
            value = 0;
            if (raw == null)
                return false;
            try
            {
                value = Convert.ToInt32(raw);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
