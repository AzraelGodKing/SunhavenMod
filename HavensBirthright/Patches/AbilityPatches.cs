using HavensBirthright.Abilities;
using HarmonyLib;
using SunhavenMods.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using Wish;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Patches for active racial abilities that intercept inventory operations.
    /// Currently implements:
    /// - Fire Elemental's Infernal Forge (auto-smelt ore into bars on pickup)
    /// </summary>
    public static class AbilityPatches
    {
        // ===================== ORE → BAR MAPPING =====================

        /// <summary>
        /// Hardcoded ore ID → bar ID mapping from game's item database.
        /// Ore IDs: 1100-1108, Bar IDs: 1200-1207
        /// </summary>
        internal static readonly Dictionary<int, int> OreToBarMap = new Dictionary<int, int>
        {
            { 1100, 1200 }, // Copper Ore → Copper Bar
            { 1101, 1201 }, // Iron Ore → Iron Bar
            { 1102, 1202 }, // Adamant Ore → Adamant Bar
            { 1103, 1203 }, // Mithril Ore → Mithril Bar
            { 1104, 1204 }, // Sunite Ore → Sunite Bar
            { 1105, 1205 }, // Gold Ore → Gold Bar
            { 1107, 1206 }, // Glorite Ore → Glorite Bar
            { 1108, 1207 }, // Elven Steel Ore → Elven Steel Bar
        };

        // ===================== TIERED MANA COSTS =====================

        /// <summary>
        /// Per-ore mana cost as percentage of max mana per bar smelted.
        /// Scales from cheap (copper) to expensive (elven steel).
        /// </summary>
        internal static readonly Dictionary<int, float> OreManaCostMap = new Dictionary<int, float>
        {
            { 1100, 1f },   // Copper Ore — 1% max mana
            { 1101, 2f },   // Iron Ore — 2%
            { 1105, 3f },   // Gold Ore — 3%
            { 1102, 4f },   // Adamant Ore — 4%
            { 1103, 5f },   // Mithril Ore — 5%
            { 1104, 6f },   // Sunite Ore — 6%
            { 1107, 7f },   // Glorite Ore — 7%
            { 1108, 8f },   // Elven Steel Ore — 8%
        };

        // ===================== REFLECTION CACHE =====================

        // Cached for fast item ID lookup
        private static bool _reflectionInitialized;
        private static bool _reflectionInitFailed;      // true if all lookups permanently failed
        private static int _reflectionInitAttempts;      // retry counter
        private const int MAX_REFLECTION_INIT_ATTEMPTS = 3;
        private static FieldInfo _cachedItemIdField;
        private static PropertyInfo _cachedItemIdProperty;
        private static MethodInfo _cachedItemIdMethod;

        // Cached for adding bars via int-based AddItem overload
        private static MethodInfo _cachedAddItemIntMethod;
        private static int _cachedAddItemArgCount; // 2 or 3 depending on overload found

        // Flag to prevent re-entrant calls when we call AddItem ourselves
        private static bool _isSmeltingItem;

        // One-time warning flag for generic Elemental race
        private static bool _genericElementalWarningLogged;

        // ===================== NOTIFICATION CACHE =====================
        // Matches TheVault's proven pattern (ItemPatches.cs:906-1085)

        private static object _notificationStackInstance;
        private static MethodInfo _sendNotificationMethod;
        private static bool _notificationInitialized;

        // Verbose logging helper
        private static bool IsVerbose => AbilityConfig.InfernalForgeVerboseLogging != null &&
                                         AbilityConfig.InfernalForgeVerboseLogging.Value;

        // ===================== NOTIFICATION SYSTEM =====================

        /// <summary>
        /// Initialize the notification system by finding the correct types and methods.
        /// Uses the 5-arg signature: SendNotification(string text, int id, int amount, bool unique, bool error)
        /// Matches TheVault's working pattern.
        /// </summary>
        private static void InitializeNotificationSystem()
        {
            if (_notificationInitialized) return;
            _notificationInitialized = true;

            try
            {
                var notifStackType = AccessTools.TypeByName("Wish.NotificationStack");
                if (notifStackType == null)
                {
                    Plugin.Log?.LogWarning("[AbilityPatches] NotificationStack type not found");
                    return;
                }

                // Cache the 5-arg SendNotification method
                _sendNotificationMethod = AccessTools.Method(notifStackType, "SendNotification",
                    new[] { typeof(string), typeof(int), typeof(int), typeof(bool), typeof(bool) });

                if (_sendNotificationMethod == null)
                {
                    Plugin.Log?.LogWarning("[AbilityPatches] SendNotification(string,int,int,bool,bool) not found");
                    return;
                }

                // Try to get instance now (may be null during init, retry at runtime)
                TryGetNotificationInstance(notifStackType);

                Plugin.Log?.LogInfo("[AbilityPatches] Notification system initialized" +
                    (_notificationStackInstance != null ? " (instance ready)" : " (instance deferred)"));
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[AbilityPatches] Notification init failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Try to get the NotificationStack instance using multiple fallback strategies.
        /// Called lazily — will retry if instance was null at init time.
        /// </summary>
        private static void TryGetNotificationInstance(Type notifStackType = null)
        {
            if (_notificationStackInstance != null) return;

            try
            {
                if (notifStackType == null)
                    notifStackType = AccessTools.TypeByName("Wish.NotificationStack");
                if (notifStackType == null) return;

                // Try SingletonBehaviour<NotificationStack>.Instance
                var singletonType = AccessTools.TypeByName("Wish.SingletonBehaviour`1");
                if (singletonType != null)
                {
                    var genericSingleton = singletonType.MakeGenericType(notifStackType);
                    var instanceProp = AccessTools.Property(genericSingleton, "Instance");
                    if (instanceProp != null)
                    {
                        _notificationStackInstance = instanceProp.GetValue(null);
                        if (_notificationStackInstance != null) return;
                    }
                }

                // Fallback: ReflectionHelper singleton lookup
                _notificationStackInstance = ReflectionHelper.GetSingletonInstance(notifStackType);
                if (_notificationStackInstance != null) return;

                // Fallback: FindObjectOfType
                var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", Type.EmptyTypes);
                if (findMethod != null)
                {
                    var genericFind = findMethod.MakeGenericMethod(notifStackType);
                    _notificationStackInstance = genericFind.Invoke(null, null);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[AbilityPatches] Notification instance lookup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a game notification using the cached 5-arg signature.
        /// Accessible from BirthrightRunner for toggle/ability notifications.
        /// </summary>
        /// <param name="text">Display text</param>
        /// <param name="id">Item/ability ID (0 for custom text notifications)</param>
        /// <param name="amount">Item quantity (0 for non-item notifications)</param>
        /// <param name="unique">Whether notification should be non-stackable</param>
        /// <param name="error">Whether this is an error notification</param>
        internal static void SendGameNotification(string text, int id = 0, int amount = 0, bool unique = false, bool error = false)
        {
            try
            {
                if (!_notificationInitialized)
                    InitializeNotificationSystem();

                if (_sendNotificationMethod == null) return;

                // Lazy retry for instance (may not be available at init time)
                if (_notificationStackInstance == null)
                    TryGetNotificationInstance();

                if (_notificationStackInstance == null) return;

                _sendNotificationMethod.Invoke(_notificationStackInstance,
                    new object[] { text, id, amount, unique, error });
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[AbilityPatches] Notification send failed: {ex.Message}");
            }
        }

        // ===================== REFLECTION CACHE INIT =====================

        /// <summary>
        /// Initialize reflection caches for Item ID lookup.
        /// Tries many field/property/method name variants, then brute-force scans if all fail.
        /// Sets _reflectionInitialized only AFTER at least one accessor is found.
        /// Retries up to MAX_REFLECTION_INIT_ATTEMPTS before giving up permanently.
        /// </summary>
        private static void InitializeReflectionCache()
        {
            if (_reflectionInitialized)
                return;
            if (_reflectionInitFailed)
                return;

            _reflectionInitAttempts++;
            if (_reflectionInitAttempts > MAX_REFLECTION_INIT_ATTEMPTS)
            {
                _reflectionInitFailed = true;
                Plugin.Log?.LogError($"[AbilityPatches] Reflection init failed after {MAX_REFLECTION_INIT_ATTEMPTS} attempts - giving up");
                return;
            }

            try
            {
                var itemType = AccessTools.TypeByName("Wish.Item");
                if (itemType == null)
                {
                    Plugin.Log?.LogWarning($"[AbilityPatches] Could not find Wish.Item type (attempt {_reflectionInitAttempts}/{MAX_REFLECTION_INIT_ATTEMPTS})");
                    return; // Will retry on next call
                }

                // Try many field name variants (fastest access)
                string[] fieldNames = { "id", "_id", "itemId", "_itemId", "ItemId", "m_id" };
                foreach (var name in fieldNames)
                {
                    _cachedItemIdField = AccessTools.Field(itemType, name);
                    if (_cachedItemIdField != null) break;
                }

                // Try many property name variants
                string[] propNames = { "id", "Id", "ID", "ItemID", "itemId", "ItemId" };
                foreach (var name in propNames)
                {
                    _cachedItemIdProperty = AccessTools.Property(itemType, name);
                    if (_cachedItemIdProperty != null) break;
                }

                // Try many method name variants
                string[] methodNames = { "ID", "GetID", "GetId", "GetItemID", "GetItemId" };
                foreach (var name in methodNames)
                {
                    _cachedItemIdMethod = AccessTools.Method(itemType, name);
                    if (_cachedItemIdMethod != null) break;
                }

                // Check if we found at least one accessor
                if (_cachedItemIdField == null && _cachedItemIdProperty == null && _cachedItemIdMethod == null)
                {
                    Plugin.Log?.LogWarning($"[AbilityPatches] ALL Item ID lookups failed (attempt {_reflectionInitAttempts}/{MAX_REFLECTION_INIT_ATTEMPTS})! Enumerating Wish.Item members containing 'id':");
                    foreach (var member in itemType.GetMembers(ReflectionHelper.AllBindingFlags))
                    {
                        if (member.Name.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Plugin.Log?.LogWarning($"  - {member.MemberType} {member.Name}");
                        }
                    }
                    return; // Will retry on next call
                }

                // SUCCESS: lock in the cache
                _reflectionInitialized = true;
                Plugin.Log?.LogInfo($"[AbilityPatches] Reflection cache initialized - " +
                    $"field:{_cachedItemIdField?.Name ?? "null"}, " +
                    $"prop:{_cachedItemIdProperty?.Name ?? "null"}, " +
                    $"method:{_cachedItemIdMethod?.Name ?? "null"}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[AbilityPatches] Reflection cache init failed (attempt {_reflectionInitAttempts}): {ex.Message}");
                // Do NOT set _reflectionInitialized — allow retry
            }
        }

        /// <summary>
        /// Get item ID using cached reflection. Tries field → property → method.
        /// Returns -1 if the ID cannot be determined.
        /// </summary>
        private static int GetItemId(object item)
        {
            if (item == null) return -1;

            InitializeReflectionCache();

            try
            {
                // Try cached field first (fastest)
                if (_cachedItemIdField != null)
                {
                    var result = _cachedItemIdField.GetValue(item);
                    if (result is int id) return id;
                }

                // Try cached property
                if (_cachedItemIdProperty != null)
                {
                    var result = _cachedItemIdProperty.GetValue(item);
                    if (result is int id) return id;
                }

                // Try cached method - Item.ID()
                if (_cachedItemIdMethod != null)
                {
                    var result = _cachedItemIdMethod.Invoke(item, null);
                    if (result is int id) return id;
                }

                // Brute-force fallback: scan all int-typed members with "id" in the name
                return GetItemIdSlow(item);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[AbilityPatches] GetItemId failed: {ex.Message}");
            }

            return -1;
        }

        /// <summary>
        /// Brute-force fallback: scans all int-typed fields and properties for anything
        /// containing "id" in the name. Caches the successful member for future calls.
        /// </summary>
        private static int GetItemIdSlow(object item)
        {
            try
            {
                var type = item.GetType();

                // Try fields
                foreach (var field in type.GetFields(ReflectionHelper.AllBindingFlags))
                {
                    if (field.FieldType == typeof(int) &&
                        field.Name.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        int val = (int)field.GetValue(item);
                        if (val > 0)
                        {
                            _cachedItemIdField = field;
                            Plugin.Log?.LogInfo($"[AbilityPatches] Slow path found item ID via field '{field.Name}' = {val}");
                            return val;
                        }
                    }
                }

                // Try properties
                foreach (var prop in type.GetProperties(ReflectionHelper.AllBindingFlags))
                {
                    if (prop.PropertyType == typeof(int) && prop.CanRead &&
                        prop.Name.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        int val = (int)prop.GetValue(item);
                        if (val > 0)
                        {
                            _cachedItemIdProperty = prop;
                            Plugin.Log?.LogInfo($"[AbilityPatches] Slow path found item ID via property '{prop.Name}' = {val}");
                            return val;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[AbilityPatches] GetItemIdSlow failed: {ex.Message}");
            }

            return -1;
        }

        /// <summary>
        /// Get or cache an int-based AddItem method on the inventory.
        /// Tries AddItem(int, int, bool) first, then AddItem(int, int).
        /// If neither exists, logs all available AddItem overloads.
        /// </summary>
        internal static MethodInfo GetAddItemIntMethod(object inventory)
        {
            if (_cachedAddItemIntMethod != null)
                return _cachedAddItemIntMethod;

            try
            {
                var invType = inventory.GetType();

                // Try 3-arg overload: AddItem(int, int, bool)
                _cachedAddItemIntMethod = AccessTools.Method(invType, "AddItem",
                    new[] { typeof(int), typeof(int), typeof(bool) });
                if (_cachedAddItemIntMethod != null)
                {
                    _cachedAddItemArgCount = 3;
                    Plugin.Log?.LogInfo("[AbilityPatches] Cached Inventory.AddItem(int, int, bool) method");
                    return _cachedAddItemIntMethod;
                }

                // Try 2-arg overload: AddItem(int, int)
                _cachedAddItemIntMethod = AccessTools.Method(invType, "AddItem",
                    new[] { typeof(int), typeof(int) });
                if (_cachedAddItemIntMethod != null)
                {
                    _cachedAddItemArgCount = 2;
                    Plugin.Log?.LogInfo("[AbilityPatches] Cached Inventory.AddItem(int, int) method (2-arg fallback)");
                    return _cachedAddItemIntMethod;
                }

                // If neither exists, log all AddItem overloads for diagnosis
                Plugin.Log?.LogWarning("[AbilityPatches] Could not find Inventory.AddItem(int,...) - enumerating all AddItem overloads:");
                foreach (var method in invType.GetMethods(ReflectionHelper.AllBindingFlags))
                {
                    if (method.Name == "AddItem")
                    {
                        var parameters = method.GetParameters();
                        string paramList = string.Join(", ", Array.ConvertAll(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                        Plugin.Log?.LogWarning($"  - AddItem({paramList})");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[AbilityPatches] Failed to cache AddItem method: {ex.Message}");
            }

            return _cachedAddItemIntMethod;
        }

        /// <summary>
        /// Invokes the cached AddItem method with the right number of arguments.
        /// Returns true on success, false on failure (caller should NOT skip original AddItem).
        /// </summary>
        internal static bool InvokeAddItem(MethodInfo addMethod, object inventory, int itemId, int amount, bool notify)
        {
            try
            {
                if (_cachedAddItemArgCount == 3)
                {
                    addMethod.Invoke(inventory, new object[] { itemId, amount, notify });
                    return true;
                }
                else if (_cachedAddItemArgCount == 2)
                {
                    addMethod.Invoke(inventory, new object[] { itemId, amount });
                    return true;
                }
                else
                {
                    Plugin.Log?.LogError($"[InfernalForge] InvokeAddItem failed: _cachedAddItemArgCount is {_cachedAddItemArgCount} (expected 2 or 3). Item {itemId} x{amount} NOT added!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[InfernalForge] InvokeAddItem threw: {ex.Message}. Item {itemId} x{amount} may be lost!");
                return false;
            }
        }

        // ===================== CACHE RESET METHODS =====================

        /// <summary>
        /// Resets the notification system cache. Call on scene transitions to prevent
        /// stale Unity object references (destroyed NotificationStack stored as C# object
        /// bypasses Unity's null override → MissingReferenceException on invoke).
        /// </summary>
        internal static void ResetNotificationCache()
        {
            _notificationStackInstance = null;
            _notificationInitialized = false;
            _sendNotificationMethod = null;
            Plugin.Log?.LogDebug("[AbilityPatches] Notification cache reset");
        }

        /// <summary>
        /// Resets the reflection cache for item ID and AddItem lookups.
        /// Call on scene transitions so lookups re-initialize with fresh game state.
        /// </summary>
        internal static void ResetReflectionCache()
        {
            _reflectionInitialized = false;
            _reflectionInitFailed = false;
            _reflectionInitAttempts = 0;
            _cachedAddItemIntMethod = null;
            _cachedAddItemArgCount = 0;
            _cachedItemIdField = null;
            _cachedItemIdProperty = null;
            _cachedItemIdMethod = null;
            _genericElementalWarningLogged = false;
            Plugin.Log?.LogDebug("[AbilityPatches] Reflection cache reset");
        }

        // ===================== MANA REGEN BLOCK PATCH =====================

        /// <summary>
        /// PREFIX patch for Player.AddMana(float, float).
        /// Blocks ALL mana regeneration while Infernal Forge is toggled ON.
        /// Only applies to Fire Elemental (Infernal Forge ability) — must NOT block
        /// mana for other races (e.g. Cat Amari food/drink/fishing mana restore).
        /// Returns false to skip the original AddMana method entirely.
        /// </summary>
        public static bool OnPlayerAddManaPrefix()
        {
            if (!RacialConfig.EnableRacialBonuses.Value) return true;
            if (!AbilityConfig.EnableInfernalForge.Value) return true;
            // Must be Fire Elemental (or generic Elemental) — Infernal Forge is Fire-only
            var manager = Plugin.GetRacialBonusManager();
            if (manager == null) return true;
            var race = manager.GetPlayerRace();
            if (!race.HasValue) return true;
            bool isFireElemental = race.Value == Race.FireElemental || race.Value == Race.Elemental;
            if (!isFireElemental) return true; // Never block mana for non-Fire races (Cat Amari, etc.)
            if (!ActiveAbilityManager.IsRuntimeEnabled(ActiveAbilityManager.InfernalForge)) return true;
            return false; // Block all mana regen (Fire Elemental only)
        }

        // ===================== INFERNAL FORGE PATCH =====================

        /// <summary>
        /// PREFIX patch for Inventory.AddItem(Item, int, int, bool, bool, bool).
        /// This is the method called by Wish.Pickup.goToPlayer() when items from the ground
        /// enter the player's inventory.
        ///
        /// For Fire Elementals with Infernal Forge enabled:
        /// - Detects ore items by ID
        /// - Checks mana threshold
        /// - Swaps ore for corresponding bar
        /// - Deducts mana cost
        /// - Returns false to skip original AddItem (ore never enters inventory)
        ///
        /// PERFORMANCE: This runs on EVERY item pickup so fast-path exits are critical.
        /// </summary>
        /// <returns>False to skip original (ore smelted to bar), True to let original run</returns>
        public static bool OnInventoryAddItemPrefix(
            object __instance,
            object item,
            int amount,
            int slot,
            bool sendNotification,
            bool specialItem,
            bool superSecretCheck)
        {
            // Fast-path: skip if we're currently adding a smelted bar (prevent recursion)
            if (_isSmeltingItem)
                return true;

            // Fast-path: skip if abilities are disabled
            if (!RacialConfig.EnableRacialBonuses.Value)
                return true;

            if (AbilityConfig.EnableActiveAbilities == null || !AbilityConfig.EnableActiveAbilities.Value)
                return true;

            if (!AbilityConfig.EnableInfernalForge.Value)
                return true;

            // Runtime toggle check (independent from config)
            if (!ActiveAbilityManager.IsRuntimeEnabled(ActiveAbilityManager.InfernalForge))
                return true;

            // Fast-path: check race before doing any reflection on the item
            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return true;

            var race = manager.GetPlayerRace();
            if (!race.HasValue)
                return true;

            // Allow both FireElemental and generic Elemental (variant detection fallback)
            bool isFireElemental = race.Value == Race.FireElemental;
            bool isGenericElemental = race.Value == Race.Elemental;

            if (!isFireElemental && !isGenericElemental)
                return true;

            if (isGenericElemental && !_genericElementalWarningLogged)
            {
                _genericElementalWarningLogged = true;
                Plugin.Log?.LogWarning("[InfernalForge] Race is generic Elemental (variant detection may have failed). " +
                    "Allowing Infernal Forge as fallback. Check body style name in logs.");
            }

            try
            {
                // Get item ID
                int itemId = GetItemId(item);
                if (itemId < 0)
                {
                    if (IsVerbose)
                        Plugin.Log?.LogInfo($"[InfernalForge] Could not determine item ID (returned {itemId})");
                    return true;
                }

                // Check if this is an ore
                if (!OreToBarMap.TryGetValue(itemId, out int barId))
                {
                    if (IsVerbose)
                        Plugin.Log?.LogInfo($"[InfernalForge] Item {itemId} is not an ore");
                    return true;
                }

                if (IsVerbose)
                    Plugin.Log?.LogInfo($"[InfernalForge] Detected ore {itemId} (amount: {amount}), bar would be {barId}");

                // Calculate how many bars we can produce (game uses 3 ore per bar)
                int orePerBar = AbilityConfig.InfernalForgeOrePerBar.Value;
                if (orePerBar < 1) orePerBar = 3; // Safety: prevent division by zero
                int barsProduced = amount / orePerBar;
                int leftoverOre = amount % orePerBar;

                // Not enough ore for even 1 bar — let it all enter inventory normally
                if (barsProduced <= 0)
                {
                    if (IsVerbose)
                        Plugin.Log?.LogInfo($"[InfernalForge] Not enough ore ({amount}) for 1 bar (need {orePerBar})");
                    return true;
                }

                // Check mana threshold
                var player = Player.Instance;
                if (player == null)
                {
                    if (IsVerbose)
                        Plugin.Log?.LogInfo("[InfernalForge] Player.Instance is null");
                    return true;
                }

                float maxMana = player.MaxMana;
                float currentMana = ReflectionHelper.TryGetValue<float>(player, "mana", 0f);
                float manaPercent = (currentMana / maxMana) * 100f;

                // Tiered mana cost per bar (lookup per ore type, default 3%)
                float costPercent = OreManaCostMap.TryGetValue(itemId, out float pct) ? pct : 3f;
                float costPerBar = maxMana * (costPercent / 100f);
                float totalManaCost = costPerBar * barsProduced;
                float newManaPercent = ((currentMana - totalManaCost) / maxMana) * 100f;

                if (newManaPercent < AbilityConfig.InfernalForgeManaThreshold.Value)
                {
                    // Try to smelt fewer bars if we can't afford all of them
                    int affordableBars = 0;
                    for (int i = barsProduced; i >= 1; i--)
                    {
                        float cost = costPerBar * i;
                        float newPercent = ((currentMana - cost) / maxMana) * 100f;
                        if (newPercent >= AbilityConfig.InfernalForgeManaThreshold.Value)
                        {
                            affordableBars = i;
                            break;
                        }
                    }

                    if (affordableBars <= 0)
                    {
                        if (IsVerbose)
                            Plugin.Log?.LogInfo($"[InfernalForge] Insufficient mana ({manaPercent:F0}%) to smelt any bars from {amount}x ore {itemId}");
                        return true; // Let ore enter inventory normally
                    }

                    // Adjust: smelt only what we can afford
                    leftoverOre = amount - (affordableBars * orePerBar);
                    barsProduced = affordableBars;
                    totalManaCost = costPerBar * barsProduced;
                }

                // Get the AddItem(int, int, bool) method to add bars/ore
                var addMethod = GetAddItemIntMethod(__instance);
                if (addMethod == null)
                {
                    if (IsVerbose)
                        Plugin.Log?.LogInfo("[InfernalForge] Could not find AddItem method on inventory");
                    return true; // Fallback: let ore enter inventory
                }

                // === SMELT: ore → bars ===

                // Deduct mana
                ReflectionHelper.SetInstanceValue(player, "mana", currentMana - totalManaCost);

                bool barSuccess = false;
                bool oreSuccess = true; // default true if no leftover
                _isSmeltingItem = true;
                try
                {
                    // Add bars to inventory
                    barSuccess = InvokeAddItem(addMethod, __instance, barId, barsProduced, sendNotification);

                    // Add leftover ore to inventory (not enough for a full bar)
                    if (leftoverOre > 0)
                    {
                        oreSuccess = InvokeAddItem(addMethod, __instance, itemId, leftoverOre, sendNotification);
                    }
                }
                finally
                {
                    _isSmeltingItem = false;
                }

                // If bar addition failed, ABORT: do NOT skip the original AddItem
                // Undo mana deduction so ore enters inventory normally (no loss)
                if (!barSuccess)
                {
                    Plugin.Log?.LogError("[InfernalForge] Bar addition failed - reverting to normal ore pickup");
                    ReflectionHelper.SetInstanceValue(player, "mana", currentMana);
                    return true; // Let original AddItem run
                }

                if (!oreSuccess)
                {
                    Plugin.Log?.LogWarning($"[InfernalForge] Leftover ore addition failed for {leftoverOre}x ore {itemId}");
                }

                // Send notification via cached 5-arg method
                string msg = leftoverOre > 0
                    ? $"Infernal Forge: {barsProduced} bar(s) from {barsProduced * orePerBar} ore, {leftoverOre} ore left over (-{totalManaCost:F0} Mana)"
                    : $"Infernal Forge: {barsProduced} bar(s) from {amount} ore (-{totalManaCost:F0} Mana)";
                SendGameNotification(msg, barId, barsProduced);

                Plugin.Log?.LogInfo($"[InfernalForge] Smelted {barsProduced * orePerBar}x ore {itemId} → {barsProduced}x bar {barId} (leftover: {leftoverOre}), cost {totalManaCost:F0} mana");

                // Skip original AddItem - we handled everything
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[InfernalForge] Error: {ex.Message}");
                Plugin.Log?.LogWarning($"[InfernalForge] Stack: {ex.StackTrace}");
                _isSmeltingItem = false;
                return true; // On error, let original run
            }
        }
    }
}
