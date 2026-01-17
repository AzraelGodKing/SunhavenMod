using HarmonyLib;
using UnityEngine;
using Wish;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Patches for economy and social mechanics
    /// Handles gold gains (already covered in PlayerPatches via AddMoney)
    /// This file handles relationship gains and luck bonuses
    /// </summary>
    public static class EconomyPatches
    {
        /// <summary>
        /// Patch GetStat to modify luck and bonus experience stats
        /// StatTypes that affect luck/economy: GoldGain, BonusExperience
        /// </summary>
        [HarmonyPatch(typeof(Player), "GetStat")]
        [HarmonyPostfix]
        public static void ModifyEconomyStats(StatType stat, ref float __result)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            switch (stat)
            {
                // Gold gain bonus (Demon) - affects all gold sources
                case StatType.GoldGain:
                    if (manager.HasBonus(BonusType.GoldFind))
                    {
                        float bonus = manager.GetBonusValue(BonusType.GoldFind);
                        __result += bonus / 100f;
                    }
                    break;

                // Experience bonus (Human)
                case StatType.BonusExperience:
                case StatType.BonusFarmingEXP:
                case StatType.BonusWoodcuttingEXP:
                    if (manager.HasBonus(BonusType.ExperienceGain))
                    {
                        float bonus = manager.GetBonusValue(BonusType.ExperienceGain);
                        __result += bonus / 100f;
                    }
                    break;
            }
        }

        /// <summary>
        /// Note: Shop discounts and relationship gains may require finding additional
        /// classes in the game code. These are placeholder hooks that can be
        /// uncommented and adjusted once the correct classes are identified.
        ///
        /// Look for:
        /// - Shop/Store classes for pricing
        /// - NPCAI or Relationship classes for friendship points
        /// - Craft/CraftingStation classes for crafting speed
        /// </summary>

        // Example: If you find a shop price calculation method, uncomment and adjust:
        // [HarmonyPatch(typeof(ShopMenu), "CalculateBuyPrice")]
        // [HarmonyPostfix]
        // public static void ModifyShopPrice(ref int __result)
        // {
        //     if (!RacialConfig.EnableRacialBonuses.Value)
        //         return;
        //
        //     var manager = Plugin.GetRacialBonusManager();
        //     if (manager != null && manager.HasBonus(BonusType.ShopDiscount))
        //     {
        //         float discount = manager.GetBonusValue(BonusType.ShopDiscount);
        //         __result = Mathf.RoundToInt(__result * (1f - discount / 100f));
        //     }
        // }

        // Example: If you find relationship point addition, uncomment and adjust:
        // [HarmonyPatch(typeof(NPCAI), "AddRelationshipPoints")]
        // [HarmonyPrefix]
        // public static void ModifyRelationshipGain(ref int points)
        // {
        //     if (!RacialConfig.EnableRacialBonuses.Value)
        //         return;
        //
        //     var manager = Plugin.GetRacialBonusManager();
        //     if (manager != null && manager.HasBonus(BonusType.RelationshipGain))
        //     {
        //         float bonus = manager.GetBonusValue(BonusType.RelationshipGain);
        //         points = Mathf.RoundToInt(points * (1f + bonus / 100f));
        //     }
        // }
    }
}
