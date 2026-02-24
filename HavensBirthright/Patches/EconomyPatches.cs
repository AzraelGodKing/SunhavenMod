using HarmonyLib;
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
        /// Modifies price by discount (used by OnBeforeShopBuyItem).
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
        /// Prefix for Wish.Shop.BuyItem(ShopItemInfo2, int) and BuyItem(ShopLoot2, int). Applies shop discount to itemInfo.price.
        /// </summary>
        public static void OnBeforeShopBuyItem(object __0, int __1)
        {
            ApplyShopDiscountToItemInfo(__0);
        }

        /// <summary>
        /// Prefix for Wish.Shop.BuyItem(ShopLoot2). Applies shop discount to itemInfo.price.
        /// </summary>
        public static void OnBeforeShopBuyItemSingle(object __0)
        {
            ApplyShopDiscountToItemInfo(__0);
        }

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
