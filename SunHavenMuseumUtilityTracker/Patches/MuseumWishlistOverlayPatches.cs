using HarmonyLib;
using SunHavenMuseumUtilityTracker.UI;
using Wish;

namespace SunHavenMuseumUtilityTracker.Patches
{
    /// <summary>
    /// Shop and gift UI overlays for undonated museum items.
    /// </summary>
    internal static class MuseumWishlistOverlayPatches
    {
        internal static void ApplyPatches(Harmony harmony)
        {
            var itemImageLateUpdate = AccessTools.Method(typeof(ItemImage), "LateUpdate");
            if (itemImageLateUpdate != null)
            {
                harmony.Patch(itemImageLateUpdate, postfix: new HarmonyMethod(typeof(MuseumWishlistOverlayPatches), nameof(ItemImage_LateUpdate_Postfix)));
                Plugin.Log?.LogInfo("Patched ItemImage.LateUpdate for museum wishlist overlay");
            }
            else
            {
                Plugin.Log?.LogWarning("Could not find ItemImage.LateUpdate for museum wishlist overlay");
            }

            var itemImageOnDisable = AccessTools.Method(typeof(ItemImage), "OnDisable");
            if (itemImageOnDisable != null)
            {
                harmony.Patch(itemImageOnDisable, postfix: new HarmonyMethod(typeof(MuseumWishlistOverlayPatches), nameof(ItemImage_OnDisable_Postfix)));
            }

            var itemImageSelect = AccessTools.Method(typeof(ItemImage), "Select");
            if (itemImageSelect != null)
            {
                harmony.Patch(itemImageSelect, postfix: new HarmonyMethod(typeof(MuseumWishlistOverlayPatches), nameof(ItemImage_Select_Postfix)));
                Plugin.Log?.LogInfo("Patched ItemImage.Select for museum wishlist tooltip");
            }

            var itemIconLateUpdate = AccessTools.Method(typeof(ItemIcon), "LateUpdate");
            if (itemIconLateUpdate != null)
            {
                harmony.Patch(itemIconLateUpdate, postfix: new HarmonyMethod(typeof(MuseumWishlistOverlayPatches), nameof(ItemIcon_LateUpdate_Postfix)));
                Plugin.Log?.LogInfo("Patched ItemIcon.LateUpdate for museum wishlist overlay");
            }

            var itemIconOnDisable = AccessTools.Method(typeof(ItemIcon), "OnDisable");
            if (itemIconOnDisable != null)
            {
                harmony.Patch(itemIconOnDisable, postfix: new HarmonyMethod(typeof(MuseumWishlistOverlayPatches), nameof(ItemIcon_OnDisable_Postfix)));
            }

            var itemIconSetupTooltip = AccessTools.Method(typeof(ItemIcon), "SetupTooltip", new[] { typeof(bool) });
            if (itemIconSetupTooltip != null)
            {
                harmony.Patch(itemIconSetupTooltip, postfix: new HarmonyMethod(typeof(MuseumWishlistOverlayPatches), nameof(ItemIcon_SetupTooltip_Postfix)));
                Plugin.Log?.LogInfo("Patched ItemIcon.SetupTooltip for museum wishlist tooltip");
            }
        }

        internal static void SubscribeDonationRefresh()
        {
            var donationManager = Plugin.GetDonationManager();
            if (donationManager == null)
                return;

            donationManager.OnDonationsChanged -= OnDonationsChanged;
            donationManager.OnDonationsChanged += OnDonationsChanged;
        }

        private static void OnDonationsChanged()
        {
            MuseumWishlistService.InvalidateCaches();
            MuseumWishlistBadgeHost.RefreshAllLabels();
            MuseumWishlistTooltipHelper.RefreshTooltipCache();
        }

        private static void ItemImage_LateUpdate_Postfix(ItemImage __instance)
        {
            MuseumWishlistBadgeHost.UpdateItemImage(__instance);
        }

        private static void ItemImage_OnDisable_Postfix(ItemImage __instance)
        {
            MuseumWishlistBadgeHost.HideItemImage(__instance);
        }

        private static void ItemImage_Select_Postfix(ItemImage __instance)
        {
            MuseumWishlistTooltipHelper.TryAppendForItemImage(__instance);
        }

        private static void ItemIcon_LateUpdate_Postfix(ItemIcon __instance)
        {
            MuseumWishlistBadgeHost.UpdateItemIcon(__instance);
        }

        private static void ItemIcon_OnDisable_Postfix(ItemIcon __instance)
        {
            MuseumWishlistBadgeHost.HideItemIcon(__instance);
        }

        private static void ItemIcon_SetupTooltip_Postfix(ItemIcon __instance)
        {
            MuseumWishlistTooltipHelper.TryAppendForItemIcon(__instance);
        }
    }
}
