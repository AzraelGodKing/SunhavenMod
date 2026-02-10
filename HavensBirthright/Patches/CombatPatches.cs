using HavensBirthright.Abilities;
using HarmonyLib;
using SunhavenMods.Shared;
using System;
using UnityEngine;
using Wish;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Patches for combat-related mechanics.
    /// Handles defense, damage reduction, and combat-triggered abilities:
    /// - Angel's Divine Ward (auto-shield on low HP)
    /// - Amari Cat's Predator's Reflex (dodge triggers attack speed buff)
    /// - Amari Reptile's Hardened Scales (stacking defense on hit)
    /// </summary>
    public static class CombatPatches
    {
        /// <summary>
        /// Prefix patch for Player.ReceiveDamage - applies defense bonus,
        /// Divine Ward damage reduction, and triggers combat abilities.
        /// </summary>
        public static void ModifyDamageReceived(ref DamageInfo damageInfo)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            var race = manager.GetPlayerRace();
            if (!race.HasValue)
                return;

            // Apply base defense bonus (existing)
            if (manager.HasBonus(BonusType.Defense))
            {
                float defenseBonus = manager.GetBonusValue(BonusType.Defense);
                damageInfo.damage *= (1f - defenseBonus / 100f);
            }

            // Active ability checks
            if (AbilityConfig.EnableActiveAbilities != null && AbilityConfig.EnableActiveAbilities.Value)
            {
                // Angel - Divine Ward: damage reduction shield when HP drops low
                if (race.Value == Race.Angel && AbilityConfig.EnableDivineWard.Value)
                {
                    ApplyDivineWard(ref damageInfo);
                }

                // Amari Reptile - Hardened Scales: stack defense on hit
                if (race.Value == Race.AmariReptile && AbilityConfig.EnableHardenedScales.Value)
                {
                    ActiveAbilityManager.AddStack(
                        ActiveAbilityManager.HardenedScales,
                        AbilityConfig.HardenedScalesMaxStacks.Value,
                        AbilityConfig.HardenedScalesDecayTime.Value
                    );
                }

                // Amari Cat - Predator's Reflex: check for dodge (damage == 0 after processing)
                // Note: dodge detection happens AFTER this prefix, so we use a postfix approach
                // We track the pre-damage state here and check in postfix
                if (race.Value == Race.AmariCat && AbilityConfig.EnablePredatorReflex.Value)
                {
                    // Store the incoming damage for dodge detection in postfix
                    _lastIncomingDamage = damageInfo.damage;
                }
            }
        }

        // Track incoming damage for dodge detection
        private static float _lastIncomingDamage = 0f;

        /// <summary>
        /// Postfix patch for Player.ReceiveDamage - detects dodges for Predator's Reflex.
        /// </summary>
        public static void OnDamageReceivedPostfix(Player __instance)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            if (AbilityConfig.EnableActiveAbilities == null || !AbilityConfig.EnableActiveAbilities.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            var race = manager.GetPlayerRace();
            if (!race.HasValue)
                return;

            // Amari Cat - Predator's Reflex: trigger on dodge
            if (race.Value == Race.AmariCat && AbilityConfig.EnablePredatorReflex.Value)
            {
                // If we had incoming damage but health didn't decrease, it was likely a dodge
                if (_lastIncomingDamage > 0)
                {
                    // Activate the attack speed buff
                    ActiveAbilityManager.ActivateAbility(
                        ActiveAbilityManager.PredatorReflex,
                        AbilityConfig.PredatorReflexDuration.Value,
                        0f // No cooldown - triggers every dodge
                    );

                    try
                    {
                        var notifType = ReflectionHelper.FindWishType("NotificationStack");
                        if (notifType != null)
                        {
                            var instance = ReflectionHelper.GetSingletonInstance(notifType);
                            if (instance != null)
                            {
                                ReflectionHelper.InvokeMethod(instance, "SendNotification",
                                    "Predator's Reflex! Attack speed increased!");
                            }
                        }
                    }
                    catch { /* notification failure is non-critical */ }
                }
                _lastIncomingDamage = 0f;
            }

            // Angel - Divine Ward: check if HP dropped below threshold
            if (race.Value == Race.Angel && AbilityConfig.EnableDivineWard.Value)
            {
                CheckDivineWardTrigger(__instance);
            }
        }

        /// <summary>
        /// Angel's Divine Ward: auto-activates when HP drops below threshold.
        /// While active, reduces all incoming damage.
        /// </summary>
        private static void ApplyDivineWard(ref DamageInfo damageInfo)
        {
            // If ward is already active, reduce damage
            if (ActiveAbilityManager.IsAbilityActive(ActiveAbilityManager.DivineWard))
            {
                float reduction = AbilityConfig.DivineWardDamageReduction.Value / 100f;
                damageInfo.damage *= (1f - reduction);
            }
        }

        /// <summary>
        /// Checks if Angel's HP dropped below threshold and activates Divine Ward.
        /// </summary>
        private static void CheckDivineWardTrigger(Player player)
        {
            try
            {
                if (ActiveAbilityManager.IsAbilityActive(ActiveAbilityManager.DivineWard))
                    return;

                if (ActiveAbilityManager.IsOnCooldown(ActiveAbilityManager.DivineWard))
                    return;

                float maxHP = player.MaxHealth;
                float currentHP = ReflectionHelper.TryGetValue<float>(player, "health", maxHP);
                float hpPercent = (currentHP / maxHP) * 100f;

                if (hpPercent <= AbilityConfig.DivineWardHPTrigger.Value)
                {
                    // Check mana cost
                    float maxMana = player.MaxMana;
                    float currentMana = ReflectionHelper.TryGetValue<float>(player, "mana", 0f);
                    float manaCost = maxMana * (AbilityConfig.DivineWardManaCostPercent.Value / 100f);

                    if (currentMana >= manaCost)
                    {
                        // Deduct mana
                        ReflectionHelper.SetInstanceValue(player, "mana", currentMana - manaCost);

                        // Activate ward
                        ActiveAbilityManager.ActivateAbility(
                            ActiveAbilityManager.DivineWard,
                            AbilityConfig.DivineWardDuration.Value,
                            AbilityConfig.DivineWardCooldown.Value
                        );

                        // Notify
                        try
                        {
                            var notifType = ReflectionHelper.FindWishType("NotificationStack");
                            if (notifType != null)
                            {
                                var instance = ReflectionHelper.GetSingletonInstance(notifType);
                                if (instance != null)
                                {
                                    ReflectionHelper.InvokeMethod(instance, "SendNotification",
                                        $"Divine Ward activated! (-{manaCost:F0} Mana)");
                                }
                            }
                        }
                        catch { /* notification failure is non-critical */ }

                        Plugin.Log.LogDebug($"[DivineWard] Activated! Duration: {AbilityConfig.DivineWardDuration.Value}s, Mana cost: {manaCost:F0}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[DivineWard] Error: {ex.Message}");
            }
        }
    }
}
