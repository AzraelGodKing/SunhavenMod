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
        /// Patch NPCAI.AddRelationship (game uses float, not AddFriendship int).
        /// Applies bonuses (Human, Amari Dog) and drawback penalties (Demon).
        /// </summary>
        public static void ModifyRelationshipGain(ref float increase)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            if (increase <= 0f)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            float originalVal = increase;

            if (manager.HasBonus(BonusType.RelationshipGain))
            {
                float bonus = manager.GetBonusValue(BonusType.RelationshipGain);
                increase *= (1f + bonus / 100f);
            }

            if (AbilityConfig.EnableRacialDrawbacks != null && AbilityConfig.EnableRacialDrawbacks.Value)
            {
                var race = manager.GetPlayerRace();
                if (race.HasValue && race.Value == Race.Demon)
                {
                    float penalty = AbilityConfig.DemonDistrustedRelationshipPenalty.Value;
                    increase *= (1f - penalty / 100f);
                }
            }

            if (Mathf.Abs(increase - originalVal) > 0.001f)
                Plugin.Log.LogDebug($"RelationshipGain modified: {originalVal} -> {increase}");
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
