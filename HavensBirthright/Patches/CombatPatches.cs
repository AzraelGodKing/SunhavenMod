using HarmonyLib;
using UnityEngine;
using Wish;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Patches for combat-related mechanics
    /// Handles melee damage, magic damage, critical hits, defense, attack speed
    /// </summary>
    public static class CombatPatches
    {
        /// <summary>
        /// Patch for GetStat to modify combat-related stats
        /// This hooks into the main stat retrieval system
        /// StatTypes: AttackDamage, Crit, Defense, DamageReduction, AttackSpeed, SpellAttackSpeed
        /// </summary>
        [HarmonyPatch(typeof(Player), "GetStat")]
        [HarmonyPostfix]
        public static void ModifyCombatStats(StatType stat, ref float __result)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            switch (stat)
            {
                // Melee/Attack damage bonus (Demon, Fire Elemental)
                case StatType.AttackDamage:
                    if (manager.HasBonus(BonusType.MeleeStrength))
                    {
                        __result = manager.ApplyBonus(__result, BonusType.MeleeStrength);
                    }
                    // Also apply magic power if it affects general damage
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
                        __result += bonus / 100f; // Add flat percentage to crit chance
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
            }
        }

        /// <summary>
        /// Patch for damage received - applies defense bonus
        /// The game calculates: DamageCalculator.CalculateFinalDamage(damage, GetStat(StatType.Defense), GetStat(StatType.DamageReduction))
        /// </summary>
        [HarmonyPatch(typeof(Player), "ReceiveDamage")]
        [HarmonyPrefix]
        public static void ModifyDamageReceived(ref DamageInfo damageInfo)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            // Apply defense bonus by reducing incoming damage
            if (manager.HasBonus(BonusType.Defense))
            {
                float defenseBonus = manager.GetBonusValue(BonusType.Defense);
                // Reduce damage by defense percentage
                damageInfo.damage *= (1f - defenseBonus / 100f);
            }
        }
    }
}
