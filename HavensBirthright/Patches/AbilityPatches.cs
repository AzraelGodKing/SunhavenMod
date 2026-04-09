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

        private static bool _isSmeltingItem;

        private static bool _genericElementalWarningLogged;

        private static bool IsVerbose => AbilityConfig.InfernalForgeVerboseLogging != null &&
                                         AbilityConfig.InfernalForgeVerboseLogging.Value;

        internal static void SendGameNotification(string text, int id = 0, int amount = 0, bool unique = false, bool error = false)
            => GameApis.SendGameNotification(text, id, amount, unique, error);

        internal static MethodInfo GetAddItemIntMethod(object inventory) => GameApis.GetAddItemIntMethod(inventory);

        internal static bool InvokeAddItem(MethodInfo addMethod, object inventory, int itemId, int amount, bool notify)
            => GameApis.InvokeAddItem(addMethod, inventory, itemId, amount, notify);

        /// <summary>Legacy split-reset: both map to <see cref="GameApis"/> item/notification caches.</summary>
        internal static void ResetNotificationCache() => GameApis.ResetItemAndNotificationCaches();

        internal static void ResetReflectionCache() => _genericElementalWarningLogged = false;

        private static int GetItemId(object item) => GameApis.GetItemId(item);

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
            Race infernalRace = ElementalVariantResolver.ResolveElementalAbilityRace(race.Value);
            bool infernalManaLockRace = infernalRace == Race.FireElemental || infernalRace == Race.Elemental;
            if (!infernalManaLockRace) return true; // Never block mana for Water Elemental, other races, etc.
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

            Race eff = ElementalVariantResolver.ResolveElementalAbilityRace(race.Value);
            bool isFireElemental = eff == Race.FireElemental;
            bool isGenericElemental = eff == Race.Elemental;

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
