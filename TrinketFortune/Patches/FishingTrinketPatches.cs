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
            // Patch bonus museum item selection (FishingRod.fishingMuseumItems.RandomItem<int>())
            var randomItemMethod = AccessTools.Method(typeof(Utilities), nameof(Utilities.RandomItem), new[] { typeof(IList<int>) });
            if (randomItemMethod != null)
            {
                var prefix = new HarmonyMethod(typeof(FishingTrinketPatches), nameof(RandomItem_Prefix));
                harmony.Patch(randomItemMethod, prefix: prefix);
                Plugin.Log.LogInfo("Patched Utilities.RandomItem<int> for fishing museum items");
            }
            else
            {
                Plugin.Log.LogWarning("Could not find Utilities.RandomItem for museum item bias");
            }

            // Patch main fish selection (FishSpawner.GetFish -> RandomFishArray.RandomItem)
            var randomFishMethod = typeof(RandomFishArray).GetMethod(nameof(RandomFishArray.RandomItem),
                BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float) }, null);
            if (randomFishMethod != null)
            {
                harmony.Patch(randomFishMethod, postfix: new HarmonyMethod(typeof(FishingTrinketPatches), nameof(RandomFishArray_RandomItem_Postfix)));
                Plugin.Log.LogInfo("Patched RandomFishArray.RandomItem for fish bias");
            }
            else
            {
                Plugin.Log.LogWarning("Could not find RandomFishArray.RandomItem");
            }
        }

        private static bool RandomItem_Prefix<T>(IList<T> list, ref T __result)
        {
            if (!Config.Enabled.Value) return true;
            if (typeof(T) != typeof(int)) return true;
            if (list == null || !ReferenceEquals(list, FishingRod.fishingMuseumItems)) return true;

            int pick = DonationHelper.PickBiasedFishingMuseumItem();
            if (pick == 0) return true;

            __result = (T)(object)pick;
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
