using HavensBirthright.Abilities;
using UnityEngine;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Patches for economy and social mechanics.
    /// Handles relationship gains, shop discounts, and drawback penalties.
    /// </summary>
    public static class EconomyPatches
    {
        /// <summary>
        /// Patch NPCAI.AddFriendship to modify relationship point gains.
        /// Applies bonuses (Human, Amari Dog) and drawback penalties (Demon).
        /// </summary>
        public static void ModifyRelationshipGain(ref int val)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            // Only apply to positive gains (not relationship loss)
            if (val <= 0)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            int originalVal = val;

            // Apply racial bonus (Human, Amari Dog)
            if (manager.HasBonus(BonusType.RelationshipGain))
            {
                float bonus = manager.GetBonusValue(BonusType.RelationshipGain);
                val = Mathf.RoundToInt(val * (1f + bonus / 100f));
            }

            // Apply Demon drawback: reduced relationship gain
            if (AbilityConfig.EnableRacialDrawbacks != null && AbilityConfig.EnableRacialDrawbacks.Value)
            {
                var race = manager.GetPlayerRace();
                if (race.HasValue && race.Value == Race.Demon)
                {
                    float penalty = AbilityConfig.DemonDistrustedRelationshipPenalty.Value;
                    val = Mathf.RoundToInt(val * (1f - penalty / 100f));
                }
            }

            if (val != originalVal)
                Plugin.Log.LogDebug($"RelationshipGain modified: {originalVal} -> {val}");
        }

        /// <summary>
        /// Patch ShopMenu.BuyItem to apply shop discounts.
        /// Affects: Human (ShopDiscount)
        /// </summary>
        public static void ModifyBuyPrice(ref int price)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager != null && manager.HasBonus(BonusType.ShopDiscount))
            {
                float discount = manager.GetBonusValue(BonusType.ShopDiscount);
                int originalPrice = price;
                price = Mathf.RoundToInt(price * (1f - discount / 100f));
                if (price < 1) price = 1;
                Plugin.Log.LogDebug($"ShopDiscount applied: {originalPrice} -> {price}");
            }
        }
    }
}
