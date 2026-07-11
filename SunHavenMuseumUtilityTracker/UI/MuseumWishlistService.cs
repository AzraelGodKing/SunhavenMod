using System.Collections.Generic;
using SunHavenMuseumUtilityTracker.Data;
using UnityEngine;
using Wish;

namespace SunHavenMuseumUtilityTracker.UI
{
    /// <summary>
    /// Read-only museum donation checks for shop/gift UI badges.
    /// Hot paths are called from ItemImage/ItemIcon LateUpdate — keep them allocation-light
    /// and avoid per-frame parent walks / full museum scans.
    /// </summary>
    internal static class MuseumWishlistService
    {
        private static HashSet<int> _museumGameItemIds;
        private static readonly Dictionary<int, bool> _needsBadgeCache = new Dictionary<int, bool>(256);
        private static int _giftModeFrame = -1;
        private static bool _giftModeCached;

        internal static bool ShopBadgesEnabled => Plugin.StaticWishlistShopBadges;
        internal static bool GiftBadgesEnabled => Plugin.StaticWishlistGiftBadges;

        internal static void InvalidateCaches()
        {
            _needsBadgeCache.Clear();
            _museumGameItemIds = null;
        }

        internal static bool NeedsMuseumBadge(int gameItemId)
        {
            if (gameItemId <= 0)
                return false;

            if (_needsBadgeCache.TryGetValue(gameItemId, out bool cached))
                return cached;

            bool needs = ComputeNeedsMuseumBadge(gameItemId);
            _needsBadgeCache[gameItemId] = needs;
            return needs;
        }

        private static bool ComputeNeedsMuseumBadge(int gameItemId)
        {
            if (!IsKnownMuseumGameItem(gameItemId))
                return false;

            var donationManager = Plugin.GetDonationManager();
            if (donationManager == null || !donationManager.IsLoaded)
                return false;

            return !donationManager.HasDonatedByGameId(gameItemId);
        }

        private static bool IsKnownMuseumGameItem(int gameItemId)
        {
            EnsureMuseumGameItemIndex();
            return _museumGameItemIds.Contains(gameItemId);
        }

        private static void EnsureMuseumGameItemIndex()
        {
            if (_museumGameItemIds != null)
                return;

            _museumGameItemIds = new HashSet<int>();
            foreach (var item in MuseumContent.GetAllItems())
            {
                if (item.GameItemId > 0)
                    _museumGameItemIds.Add(item.GameItemId);
            }
        }

        /// <summary>Shop slots only — no parent-name heuristics (those caused LateUpdate lag).</summary>
        internal static bool ShouldDecorateShopItem(ItemImage itemImage)
        {
            if (!ShopBadgesEnabled || itemImage == null || !itemImage.isActiveAndEnabled)
                return false;

            return itemImage.ShopItem;
        }

        /// <summary>Gift inventory only — no parent-name heuristics (those caused LateUpdate lag).</summary>
        internal static bool ShouldDecorateGiftIcon(ItemIcon itemIcon)
        {
            if (!GiftBadgesEnabled || itemIcon == null || !itemIcon.isActiveAndEnabled)
                return false;

            return IsGiftInventoryOpen();
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
            int frame = Time.frameCount;
            if (frame == _giftModeFrame)
                return _giftModeCached;

            _giftModeFrame = frame;
            _giftModeCached = false;

            var player = Player.Instance;
            if (player?.Inventory is PlayerInventory playerInventory)
                _giftModeCached = playerInventory.GiftMode;

            return _giftModeCached;
        }
    }
}
