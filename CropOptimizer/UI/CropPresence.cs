using System;
using System.Reflection;
using CropOptimizer.Patches;
using HarmonyLib;
using UnityEngine;

namespace CropOptimizer.UI
{
    /// <summary>Filters scene <c>Wish.Crop</c> instances down to planted, active crops worth tracking.</summary>
    internal static class CropPresence
    {
        private static PropertyInfo _placedProp;
        private static bool _resolved;

        public static bool IsTrackable(Component crop)
        {
            if (crop == null)
                return false;

            GameObject go = crop.gameObject;
            if (go == null || !go.activeInHierarchy)
                return false;

            if (crop is Behaviour behaviour && !behaviour.enabled)
                return false;

            EnsureResolved();
            if (_placedProp != null)
            {
                try
                {
                    if (_placedProp.GetValue(crop) is bool placed && !placed)
                        return false;
                }
                catch
                {
                }
            }

            return CropGrowthPatch.TryGetTooltipHarvestItemId(crop, out int itemId) && itemId > 0;
        }

        /// <summary>Looser gate for hover / highlights — only excludes inactive or unplaced crops.</summary>
        public static bool IsPresent(Component crop)
        {
            if (crop == null)
                return false;

            GameObject go = crop.gameObject;
            if (go == null || !go.activeInHierarchy)
                return false;

            if (crop is Behaviour behaviour && !behaviour.enabled)
                return false;

            EnsureResolved();
            if (_placedProp != null)
            {
                try
                {
                    if (_placedProp.GetValue(crop) is bool placed && !placed)
                        return false;
                }
                catch
                {
                }
            }

            return true;
        }

        private static void EnsureResolved()
        {
            if (_resolved)
                return;
            _resolved = true;

            Type cropType = AccessTools.TypeByName("Wish.Crop");
            if (cropType == null)
                return;

            _placedProp = AccessTools.Property(cropType, "Placed") ?? AccessTools.Property(cropType, "placed");
        }
    }
}
