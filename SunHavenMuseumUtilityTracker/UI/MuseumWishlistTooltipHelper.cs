using System;
using System.Reflection;
using HarmonyLib;
using SunhavenMods.Shared;
using Wish;

namespace SunHavenMuseumUtilityTracker.UI
{
    /// <summary>
    /// Appends a museum-needed line to the vanilla item tooltip after hover.
    /// </summary>
    internal static class MuseumWishlistTooltipHelper
    {
        private static FieldInfo _descriptionTmpField;
        private static string _cachedTooltipLine;
        private static string _cachedTooltipKey;

        internal static void TryAppendForItemImage(ItemImage itemImage)
        {
            if (itemImage == null || !MuseumWishlistService.ShouldDecorateShopItem(itemImage))
                return;

            TryAppend(MuseumWishlistService.GetGameItemId(itemImage));
        }

        internal static void TryAppendForItemIcon(ItemIcon itemIcon)
        {
            if (itemIcon == null || !MuseumWishlistService.ShouldDecorateGiftIcon(itemIcon))
                return;

            TryAppend(MuseumWishlistService.GetGameItemId(itemIcon));
        }

        internal static void RefreshTooltipCache()
        {
            _cachedTooltipLine = null;
        }

        private static void TryAppend(int gameItemId)
        {
            if (!MuseumWishlistService.NeedsMuseumBadge(gameItemId))
                return;

            var tooltip = Tooltip.Instance;
            if (tooltip == null || !tooltip.gameObject.activeSelf)
                return;

            try
            {
                EnsureField();
                if (_descriptionTmpField == null)
                    return;

                object tmp = _descriptionTmpField.GetValue(tooltip);
                if (tmp == null)
                    return;

                PropertyInfo textProp = tmp.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                if (textProp == null)
                    return;

                string line = GetTooltipLine();
                string current = textProp.GetValue(tmp) as string ?? string.Empty;
                if (current.IndexOf(line, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;

                textProp.SetValue(tmp, current + "\n<size=80%><color=#D98C40><i>" + line + "</i></color></size>");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[MuseumWishlistTooltip] Append failed: {ex.Message}");
            }
        }

        private static void EnsureField()
        {
            if (_descriptionTmpField != null)
                return;

            _descriptionTmpField = AccessTools.Field(typeof(Tooltip), "_descriptionTMP");
        }

        private static string GetTooltipLine()
        {
            const string key = "smut.wishlist.tooltip";
            if (_cachedTooltipLine != null && _cachedTooltipKey == key)
                return _cachedTooltipLine;

            _cachedTooltipKey = key;
            _cachedTooltipLine = ModLocalization.T(key);
            if (string.IsNullOrWhiteSpace(_cachedTooltipLine))
                _cachedTooltipLine = "Needed for the museum";

            return _cachedTooltipLine;
        }
    }
}
