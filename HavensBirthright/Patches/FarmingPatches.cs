using HarmonyLib;
using UnityEngine;
using Wish;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Patches for farming and gathering mechanics
    /// Hooks into Player.GetStat for skill-related stats
    /// StatTypes: FarmingSkill, MiningSkill, FishingSkill, ExplorationSkill, SmithingSkill
    /// Also: MiningCrit, WoodcuttingCrit, ExtraForageableChance
    /// </summary>
    public static class FarmingPatches
    {
        /// <summary>
        /// Patch GetStat to modify farming/gathering skill stats
        /// These affect: FarmingSkillLevel, MiningSkillLevel, FishingSkillLevel, etc.
        /// </summary>
        [HarmonyPatch(typeof(Player), "GetStat")]
        [HarmonyPostfix]
        public static void ModifySkillStats(StatType stat, ref float __result)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            switch (stat)
            {
                // Farming skill bonus (Elf)
                case StatType.FarmingSkill:
                    if (manager.HasBonus(BonusType.FarmingSpeed))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.FarmingSpeed);
                    }
                    break;

                // Mining skill bonus (Elemental)
                case StatType.MiningSkill:
                    if (manager.HasBonus(BonusType.MiningSpeed))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.MiningSpeed);
                    }
                    break;

                // Mining crit bonus (extra ore chance)
                case StatType.MiningCrit:
                    if (manager.HasBonus(BonusType.MiningYield))
                    {
                        float bonus = manager.GetBonusValue(BonusType.MiningYield);
                        __result += bonus / 100f;
                    }
                    break;

                // Woodcutting crit bonus (Amari)
                case StatType.WoodcuttingCrit:
                    if (manager.HasBonus(BonusType.WoodcuttingSpeed))
                    {
                        float bonus = manager.GetBonusValue(BonusType.WoodcuttingSpeed);
                        __result += bonus / 100f;
                    }
                    break;

                // Fishing skill bonus (Naga)
                case StatType.FishingSkill:
                    if (manager.HasBonus(BonusType.FishingSpeed))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.FishingSpeed);
                    }
                    if (manager.HasBonus(BonusType.FishingLuck))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.FishingLuck);
                    }
                    break;

                // Exploration/Foraging skill (Elf)
                case StatType.ExplorationSkill:
                    if (manager.HasBonus(BonusType.ForagingChance))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.ForagingChance);
                    }
                    break;

                // Extra forageable chance (Elf)
                case StatType.ExtraForageableChance:
                    if (manager.HasBonus(BonusType.ForagingChance))
                    {
                        float bonus = manager.GetBonusValue(BonusType.ForagingChance);
                        __result += bonus / 100f;
                    }
                    break;

                // Smithing/Crafting skill (Amari)
                case StatType.SmithingSkill:
                    if (manager.HasBonus(BonusType.CraftingSpeed))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.CraftingSpeed);
                    }
                    break;
            }
        }

        /// <summary>
        /// Patch FishingSkillLevel getter for fishing bonuses (Naga, Water Elemental)
        /// Game calculates: Professions[ProfessionType.Fishing].level + GetStat(StatType.FishingSkill)
        /// </summary>
        [HarmonyPatch(typeof(Player), "get_FishingSkillLevel")]
        [HarmonyPostfix]
        public static void ModifyFishingLevel(ref float __result)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            // Apply fishing speed bonus
            if (manager.HasBonus(BonusType.FishingSpeed))
            {
                __result = manager.ApplyBonus(__result, BonusType.FishingSpeed);
            }

            // Apply fishing luck bonus
            if (manager.HasBonus(BonusType.FishingLuck))
            {
                __result = manager.ApplyBonus(__result, BonusType.FishingLuck);
            }
        }

        /// <summary>
        /// Patch FarmingSkillLevel getter for farming bonuses (Elf)
        /// Game calculates: Professions[ProfessionType.Farming].level + GetStat(StatType.FarmingSkill)
        /// </summary>
        [HarmonyPatch(typeof(Player), "get_FarmingSkillLevel")]
        [HarmonyPostfix]
        public static void ModifyFarmingLevel(ref float __result)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            if (manager.HasBonus(BonusType.FarmingSpeed))
            {
                __result = manager.ApplyBonus(__result, BonusType.FarmingSpeed);
            }

            // Crop quality bonus adds to effective skill
            if (manager.HasBonus(BonusType.CropQuality))
            {
                __result = manager.ApplyBonus(__result, BonusType.CropQuality);
            }
        }

        /// <summary>
        /// Patch MiningSkillLevel getter for mining bonuses (Elemental)
        /// Game calculates: Professions[ProfessionType.Mining].level + GetStat(StatType.MiningSkill)
        /// </summary>
        [HarmonyPatch(typeof(Player), "get_MiningSkillLevel")]
        [HarmonyPostfix]
        public static void ModifyMiningLevel(ref float __result)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            if (manager.HasBonus(BonusType.MiningSpeed))
            {
                __result = manager.ApplyBonus(__result, BonusType.MiningSpeed);
            }
        }

        /// <summary>
        /// Patch ExplorationSkillLevel for foraging bonuses (Elf)
        /// Game calculates: Professions[ProfessionType.Exploration].level + GetStat(StatType.ExplorationSkill)
        /// </summary>
        [HarmonyPatch(typeof(Player), "get_ExplorationSkillLevel")]
        [HarmonyPostfix]
        public static void ModifyExplorationLevel(ref float __result)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            if (manager.HasBonus(BonusType.ForagingChance))
            {
                __result = manager.ApplyBonus(__result, BonusType.ForagingChance);
            }
        }
    }
}
