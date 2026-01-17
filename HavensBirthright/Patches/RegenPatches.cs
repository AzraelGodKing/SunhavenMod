using HarmonyLib;
using Wish;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Patches for health and mana regeneration
    /// Game uses GetStat(StatType.HealthRegen) and GetStat(StatType.ManaRegen)
    /// Base values: HealthRegen = 0.025f, ManaRegen = 0.2f
    /// </summary>
    public static class RegenPatches
    {
        /// <summary>
        /// Patch GetStat to modify regeneration stats
        /// HealthRegen: Angel, Water Elemental
        /// ManaRegen: Elf, Naga, Water Elemental
        /// </summary>
        [HarmonyPatch(typeof(Player), "GetStat")]
        [HarmonyPostfix]
        public static void ModifyRegenStats(StatType stat, ref float __result)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            switch (stat)
            {
                // Health regeneration (Angel, Water Elemental)
                case StatType.HealthRegen:
                    if (manager.HasBonus(BonusType.HealthRegen))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.HealthRegen);
                    }
                    break;

                // Mana regeneration (Elf, Naga, Water Elemental)
                case StatType.ManaRegen:
                    if (manager.HasBonus(BonusType.ManaRegen))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.ManaRegen);
                    }
                    break;
            }
        }
    }
}
