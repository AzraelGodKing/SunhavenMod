using HavensBirthright.Abilities;
using HarmonyLib;
using SunhavenMods.Shared;
using System;
using UnityEngine;
using Wish;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Demon-only: Soul Harvest — bonus gold when defeating enemies, at the cost of HP per kill.
    /// Only affects the player when their chosen race is Demon; has no effect for any other race.
    /// </summary>
    public static class SoulHarvestPatches
    {
        /// <summary>
        /// Postfix for EnemyAI.Die(bool fromLocalPlayer).
        /// When the local player kills an enemy and the player is a Demon with Soul Harvest enabled and toggled ON,
        /// grants bonus gold and deducts HP.
        /// </summary>
        public static void OnEnemyDiePostfix(bool fromLocalPlayer)
        {
            if (!fromLocalPlayer)
                return;

            if (!RacialConfig.EnableRacialBonuses.Value)
                return;
            if (AbilityConfig.EnableActiveAbilities == null || !AbilityConfig.EnableActiveAbilities.Value)
                return;
            if (AbilityConfig.EnableSoulHarvest == null || !AbilityConfig.EnableSoulHarvest.Value)
                return;
            if (!ActiveAbilityManager.IsRuntimeEnabled(ActiveAbilityManager.SoulHarvest))
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null) return;

            var race = manager.GetPlayerRace();
            if (!race.HasValue || race.Value != Race.Demon)
                return;

            try
            {
                var player = Player.Instance;
                if (player == null) return;

                int goldAmount = AbilityConfig.SoulHarvestGoldPerKill?.Value ?? 15;
                float hpCostPercent = AbilityConfig.SoulHarvestHPCostPercent?.Value ?? 1f;

                if (goldAmount > 0)
                {
                    var addMoneyMethod = AccessTools.Method(player.GetType(), "AddMoney",
                        new[] { typeof(int), typeof(bool), typeof(bool), typeof(bool) });
                    if (addMoneyMethod != null)
                        addMoneyMethod.Invoke(player, new object[] { goldAmount, true, false, true });
                }

                if (hpCostPercent > 0f)
                {
                    float maxHP = player.MaxHealth;
                    float currentHP = ReflectionHelper.TryGetValue<float>(player, "health", -1f);
                    if (currentHP < 0f)
                        currentHP = ReflectionHelper.TryGetValue<float>(player, "Health", maxHP);
                    float cost = maxHP * (hpCostPercent / 100f);
                    float newHP = Mathf.Max(1f, currentHP - cost);
                    ReflectionHelper.SetInstanceValue(player, "Health", newHP);
                }

                AbilityPatches.SendGameNotification(
                    $"Soul Harvest: +{goldAmount} gold" + (hpCostPercent > 0f ? $" (-{hpCostPercent}% HP)" : ""));
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[SoulHarvest] OnEnemyDiePostfix: {ex.Message}");
            }
        }
    }
}
