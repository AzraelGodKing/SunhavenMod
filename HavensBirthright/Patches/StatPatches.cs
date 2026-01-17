using HarmonyLib;
using Wish;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Unified patch for Player.GetStat to handle all stat modifications
    /// This consolidates combat, farming, regen, and economy stat bonuses
    /// </summary>
    public static class StatPatches
    {
        /// <summary>
        /// Postfix patch for Player.GetStat - modifies stat values based on racial bonuses
        /// </summary>
        public static void ModifyGetStat(StatType stat, ref float __result)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            switch (stat)
            {
                // === COMBAT STATS ===

                // Melee/Attack damage bonus (Demon, Fire Elemental, Amari Reptile)
                case StatType.AttackDamage:
                    if (manager.HasBonus(BonusType.MeleeStrength))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.MeleeStrength);
                    }
                    break;

                // Spell/Magic damage bonus (Angel, Fire Elemental, Elemental)
                case StatType.SpellDamage:
                    if (manager.HasBonus(BonusType.MagicPower))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.MagicPower);
                    }
                    break;

                // Critical hit chance (Demon, Fire Elemental)
                case StatType.Crit:
                    if (manager.HasBonus(BonusType.CriticalChance))
                    {
                        float bonus = manager.GetBonusValue(BonusType.CriticalChance);
                        __result += bonus / 100f;
                    }
                    break;

                // Defense bonus (Water Elemental, Naga)
                case StatType.Defense:
                    if (manager.HasBonus(BonusType.Defense))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.Defense);
                    }
                    break;

                // Attack speed bonus (Fire Elemental, Amari)
                case StatType.AttackSpeed:
                case StatType.SpellAttackSpeed:
                    if (manager.HasBonus(BonusType.AttackSpeed))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.AttackSpeed);
                    }
                    break;

                // Movement speed bonus (Amari)
                case StatType.Movespeed:
                    if (manager.HasBonus(BonusType.MovementSpeed))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.MovementSpeed);
                    }
                    break;

                // Dodge chance bonus (Amari Cat)
                case StatType.Dodge:
                    if (manager.HasBonus(BonusType.DodgeChance))
                    {
                        float dodgeBonus = manager.GetBonusValue(BonusType.DodgeChance);
                        __result += dodgeBonus / 100f;
                    }
                    break;

                // === SKILL STATS ===

                // Farming skill bonus (Elf)
                case StatType.FarmingSkill:
                    if (manager.HasBonus(BonusType.FarmingSpeed))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.FarmingSpeed);
                    }
                    break;

                // Extra crop chance bonus (Elf) - chance to get extra crops
                case StatType.ExtraCropChance:
                    if (manager.HasBonus(BonusType.CropQuality))
                    {
                        float cropBonus = manager.GetBonusValue(BonusType.CropQuality);
                        __result += cropBonus / 100f;
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
                        float miningBonus = manager.GetBonusValue(BonusType.MiningYield);
                        __result += miningBonus / 100f;
                    }
                    break;

                // Woodcutting crit bonus (extra wood chance)
                // Also applies WoodcuttingSpeed here since no separate WoodcuttingSkill StatType exists
                case StatType.WoodcuttingCrit:
                    if (manager.HasBonus(BonusType.WoodcuttingSpeed))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.WoodcuttingSpeed);
                    }
                    if (manager.HasBonus(BonusType.WoodcuttingYield))
                    {
                        float woodBonus = manager.GetBonusValue(BonusType.WoodcuttingYield);
                        __result += woodBonus / 100f;
                    }
                    break;

                // Fishing skill bonus (Naga, Amari Aquatic)
                // Also applies FishingLuck here since no separate StatType exists
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
                        float forageBonus = manager.GetBonusValue(BonusType.ForagingChance);
                        __result += forageBonus / 100f;
                    }
                    break;

                // Smithing/Crafting skill (Amari)
                case StatType.SmithingSkill:
                    if (manager.HasBonus(BonusType.CraftingSpeed))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.CraftingSpeed);
                    }
                    break;

                // === HEALTH/MANA STATS ===

                // Max Health (Demon, Amari Dog, Amari Reptile)
                case StatType.Health:
                    if (manager.HasBonus(BonusType.MaxHealth))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.MaxHealth);
                    }
                    break;

                // Max Mana (Angel)
                case StatType.Mana:
                    if (manager.HasBonus(BonusType.MaxMana))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.MaxMana);
                    }
                    break;

                // === REGEN STATS ===

                // Health regeneration (Water Elemental, Naga)
                case StatType.HealthRegen:
                    if (manager.HasBonus(BonusType.HealthRegen))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.HealthRegen);
                    }
                    break;

                // Mana regeneration (Angel, Elf)
                case StatType.ManaRegen:
                    if (manager.HasBonus(BonusType.ManaRegen))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.ManaRegen);
                    }
                    break;

                // === ECONOMY STATS ===

                // Gold gain bonus (Demon)
                case StatType.GoldGain:
                    if (manager.HasBonus(BonusType.GoldFind))
                    {
                        float bonus = manager.GetBonusValue(BonusType.GoldFind);
                        __result += bonus / 100f;
                    }
                    break;

                // Experience bonuses (Human)
                case StatType.BonusExperience:
                case StatType.BonusFarmingEXP:
                case StatType.BonusWoodcuttingEXP:
                    if (manager.HasBonus(BonusType.ExperienceGain))
                    {
                        float bonus = manager.GetBonusValue(BonusType.ExperienceGain);
                        __result += bonus / 100f;
                    }
                    break;
            }
        }
    }
}
