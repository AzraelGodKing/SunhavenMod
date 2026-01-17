using UnityEngine;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Patches for economy and social mechanics
    /// Handles relationship gains and shop discounts
    /// Note: Gold/Experience bonuses are now handled in StatPatches via GetStat
    /// These patches are registered manually in Plugin.cs using dynamic type lookup
    /// </summary>
    public static class EconomyPatches
    {
        /// <summary>
        /// Patch NPCAI.AddFriendship to modify relationship point gains
        /// Affects: Human (RelationshipBonus), Amari Dog (RelationshipBonus)
        /// </summary>
        public static void ModifyRelationshipGain(ref int val)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            // Only apply to positive gains (not relationship loss)
            if (val <= 0)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager != null && manager.HasBonus(BonusType.RelationshipGain))
            {
                float bonus = manager.GetBonusValue(BonusType.RelationshipGain);
                int originalVal = val;
                val = Mathf.RoundToInt(val * (1f + bonus / 100f));
                Plugin.Log.LogDebug($"RelationshipGain bonus applied: {originalVal} -> {val}");
            }
        }

        /// <summary>
        /// Patch ShopMenu.BuyItem to apply shop discounts
        /// Affects: Human (ShopDiscount)
        /// This patches the buy price calculation
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
                // Apply discount (reduce price)
                price = Mathf.RoundToInt(price * (1f - discount / 100f));
                // Ensure price doesn't go below 1
                if (price < 1) price = 1;
                Plugin.Log.LogDebug($"ShopDiscount applied: {originalPrice} -> {price}");
            }
        }
    }
}
