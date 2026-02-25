using HarmonyLib;
using HavensBirthright.Abilities;
using UnityEngine;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Harmony patches for economy and social systems: relationship gains and shop buy prices.
    /// </summary>
    public static class EconomyPatches
    {
        /// <summary>
        /// Adjusts the relationship gain value before the game applies it (hooked into NPCAI.AddRelationship).
        /// If racial bonuses are on: multiplies gain by (1 + RelationshipGain bonus %). If drawbacks are on and
        /// player is Demon: multiplies gain by (1 - Demon distrust penalty %). Logs when the value changes.
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

            // Apply racial bonus (e.g. Human/Amari Dog) as a percentage increase.
            if (manager.HasBonus(BonusType.RelationshipGain))
            {
                float bonus = manager.GetBonusValue(BonusType.RelationshipGain);
                increase *= (1f + bonus / 100f);
            }

            // Apply Demon drawback: reduce relationship gain by configured penalty.
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
        /// Reduces the given price by the player's ShopDiscount racial bonus (percentage). Minimum price is 1.
        /// Only runs when racial bonuses are enabled and the player has the ShopDiscount bonus.
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

        /// <summary>
        /// Harmony prefix for shop BuyItem(ShopItemInfo2/ShopLoot2, int). Mutates the first argument's "price" field
        /// via ApplyShopDiscountToItemInfo so the discount is applied before the game charges the player.
        /// </summary>
        public static void OnBeforeShopBuyItem(object __0, int __1)
        {
            ApplyShopDiscountToItemInfo(__0);
        }

        /// <summary>
        /// Harmony prefix for shop BuyItem(ShopLoot2) (single-arg overload). Same as OnBeforeShopBuyItem: applies
        /// discount to the item's "price" field before the purchase runs.
        /// </summary>
        public static void OnBeforeShopBuyItemSingle(object __0)
        {
            ApplyShopDiscountToItemInfo(__0);
        }

        /// <summary>
        /// Uses reflection to read the "price" field from the given shop item object, runs ModifyBuyPrice on it,
        /// then writes the new price back so the game uses the discounted value.
        /// </summary>
        private static void ApplyShopDiscountToItemInfo(object itemInfo)
        {
            if (itemInfo == null) return;
            var t = itemInfo.GetType();
            var priceField = AccessTools.Field(t, "price");
            if (priceField == null) return;
            int price = (int)priceField.GetValue(itemInfo);
            ModifyBuyPrice(ref price);
            priceField.SetValue(itemInfo, price);
        }
    }
}
