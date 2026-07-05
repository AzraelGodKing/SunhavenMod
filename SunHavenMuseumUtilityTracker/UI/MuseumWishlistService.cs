using SunHavenMuseumUtilityTracker.Data;
using Wish;

namespace SunHavenMuseumUtilityTracker.UI
{
    /// <summary>
    /// Read-only museum donation checks for shop/gift UI badges.
    /// Same integration as MuseumTodoIntegration and TrinketFortune DonationHelper.
    /// </summary>
    internal static class MuseumWishlistService
    {
        internal static bool ShopBadgesEnabled => Plugin.StaticWishlistShopBadges;
        internal static bool GiftBadgesEnabled => Plugin.StaticWishlistGiftBadges;

        internal static bool NeedsMuseumBadge(int gameItemId)
        {
            if (gameItemId <= 0)
                return false;

            var donationManager = Plugin.GetDonationManager();
            if (donationManager == null || !donationManager.IsLoaded)
                return false;

            if (MuseumContent.FindByGameItemId(gameItemId) == null)
                return false;

            return !donationManager.HasDonatedByGameId(gameItemId);
        }

        internal static bool ShouldDecorateShopItem(ItemImage itemImage)
        {
            if (!ShopBadgesEnabled || itemImage == null || !itemImage.isActiveAndEnabled)
                return false;

            if (itemImage.ShopItem)
                return true;

            return IsNpcGiftListContext(itemImage.transform);
        }

        internal static bool ShouldDecorateGiftIcon(ItemIcon itemIcon)
        {
            if (!GiftBadgesEnabled || itemIcon == null || !itemIcon.isActiveAndEnabled)
                return false;

            if (IsGiftInventoryOpen())
                return true;

            return IsNpcGiftListContext(itemIcon.transform);
        }

        internal static int GetGameItemId(ItemImage itemImage)
        {
            if (itemImage?.Item == null)
                return 0;
            return itemImage.Item.ID();
        }

        internal static int GetGameItemId(ItemIcon itemIcon)
        {
            if (itemIcon?.item == null)
                return 0;
            return itemIcon.item.ID();
        }

        private static bool IsGiftInventoryOpen()
        {
            var player = Player.Instance;
            if (player?.Inventory is PlayerInventory playerInventory)
                return playerInventory.GiftMode;
            return false;
        }

        private static bool IsNpcGiftListContext(UnityEngine.Transform transform)
        {
            while (transform != null)
            {
                string name = transform.name;
                if (name.IndexOf("Gift", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Love", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Like", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Social", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Relationship", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("NPC", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                transform = transform.parent;
            }

            return false;
        }
    }
}
