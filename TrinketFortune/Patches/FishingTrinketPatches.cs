using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Wish;

namespace TrinketFortune.Patches
{
    /// <summary>
    /// Harmony patches for fishing loot / trinket bias.
    /// - Utilities.RandomItem: bias bonus museum items (5% fishing drop) toward unowned
    /// - RandomFishArray.RandomItem: bias main fish spawn toward unowned aquarium fish
    /// </summary>
    public static class FishingTrinketPatches
    {
        public static void ApplyPatches(Harmony harmony)
        {
            TryPatchUtilitiesRandomItem(harmony);
            TryPatchRandomFishArray(harmony);
        }

        private static void TryPatchUtilitiesRandomItem(Harmony harmony)
        {
            // Patch bonus museum item selection (FishingRod.fishingMuseumItems.RandomItem<int>()).
            // HarmonyX cannot bind a generic prefix's `ref T __result` to closed RandomItem<int>
            // (IL compile: "Cannot assign method return type System.Int32 to __result type").
            MethodInfo randomItemMethod = null;
            foreach (var mi in typeof(Utilities).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (mi.Name != nameof(Utilities.RandomItem) || !mi.IsGenericMethodDefinition)
                    continue;
                var parms = mi.GetParameters();
                if (parms.Length != 1)
                    continue;
                var p0 = parms[0].ParameterType;
                if (!p0.IsGenericType || p0.GetGenericTypeDefinition() != typeof(IList<>))
                    continue;
                try
                {
                    randomItemMethod = mi.MakeGenericMethod(typeof(int));
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogDebug($"[TrinketFortune] RandomItem<int> close failed: {ex.Message}");
                }
                if (randomItemMethod != null)
                    break;
            }

            if (randomItemMethod == null)
            {
                Plugin.Log.LogWarning("Could not find Utilities.RandomItem for museum item bias");
                return;
            }

            try
            {
                var prefix = new HarmonyMethod(typeof(FishingTrinketPatches), nameof(RandomItemInt_Prefix));
                harmony.Patch(randomItemMethod, prefix: prefix);
                Plugin.Log.LogInfo("Patched Utilities.RandomItem<int> for fishing museum items");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to patch Utilities.RandomItem<int>: {ex.Message}");
            }
        }

        private static void TryPatchRandomFishArray(Harmony harmony)
        {
            var randomFishMethod = typeof(RandomFishArray).GetMethod(nameof(RandomFishArray.RandomItem),
                BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float) }, null);
            if (randomFishMethod == null)
            {
                Plugin.Log.LogWarning("Could not find RandomFishArray.RandomItem");
                return;
            }

            try
            {
                harmony.Patch(randomFishMethod, postfix: new HarmonyMethod(typeof(FishingTrinketPatches), nameof(RandomFishArray_RandomItem_Postfix)));
                Plugin.Log.LogInfo("Patched RandomFishArray.RandomItem for fish bias");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to patch RandomFishArray.RandomItem: {ex.Message}");
            }
        }

        /// <summary>
        /// Closed prefix for <c>Utilities.RandomItem&lt;int&gt;</c>. HarmonyX needs a concrete
        /// <c>ref int __result</c> — an open generic <c>ref T __result</c> fails IL compile.
        /// Parameter name <c>list</c> matches Wish.Utilities.RandomItem.
        /// </summary>
        private static bool RandomItemInt_Prefix(IList<int> list, ref int __result)
        {
            if (!Config.Enabled.Value) return true;
            if (list == null || !ReferenceEquals(list, FishingRod.fishingMuseumItems)) return true;

            int pick = DonationHelper.PickBiasedFishingMuseumItem();
            if (pick == 0) return true;

            __result = pick;
            return false;
        }

        private static void RandomFishArray_RandomItem_Postfix(RandomFishArray __instance, float level, ref FishData __result)
        {
            var biased = DonationHelper.PickBiasedFish(__instance, level);
            if (biased != null)
                __result = biased;
        }
    }
}
