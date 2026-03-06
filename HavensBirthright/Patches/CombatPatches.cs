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
    /// - Amari Cat's Nine Lives (passive cheat death)
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

            // Amari Cat - Nine Lives (passive): chance to survive lethal damage and heal to X% HP (not an active/innate power)
            if (race.Value == Race.AmariCat && AbilityConfig.EnableNineLives != null && AbilityConfig.EnableNineLives.Value)
            {
                TryNineLives(ref damageInfo);
            }

            // Active ability checks (innate powers)
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
            }
        }

        // Nine Lives: when we proc, postfix will set HP to this value
        private static bool _nineLivesProcced = false;
        private static float _nineLivesHealToHP = 0f;

        /// <summary>Call on menu/game transition so state is not carried between saves.</summary>
        public static void ResetNineLives()
        {
            _nineLivesProcced = false;
            _nineLivesHealToHP = 0f;
        }

        private static void TryNineLives(ref DamageInfo damageInfo)
        {
            try
            {
                var player = Player.Instance;
                if (player == null)
                    return;

                float maxHP = player.MaxHealth;
                float currentHP = ReflectionHelper.TryGetValue<float>(player, "health", maxHP);
                if (currentHP <= 0 || maxHP <= 0)
                    return;

                // Would this damage be lethal?
                float hpAfter = currentHP - damageInfo.damage;
                if (hpAfter > 0f)
                    return;

                float chanceRaw = AbilityConfig.NineLivesChance != null ? AbilityConfig.NineLivesChance.Value / 100f : 0.1f;
                float chance = Mathf.Min(chanceRaw, 0.6f); // Max 60%
                if (UnityEngine.Random.value >= chance)
                    return;

                // Proc: cancel lethal damage; postfix will set HP to heal percent
                damageInfo.damage = 0f;
                float healPercent = AbilityConfig.NineLivesHealPercent != null ? AbilityConfig.NineLivesHealPercent.Value / 100f : 0.5f;
                _nineLivesHealToHP = maxHP * healPercent;
                _nineLivesProcced = true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[NineLives] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Postfix patch for Player.ReceiveDamage - Nine Lives heal.
        /// </summary>
        public static void OnDamageReceivedPostfix(Player __instance)
        {
            // Nine Lives: set HP to saved value when we procced in prefix (survived lethal damage)
            if (_nineLivesProcced)
            {
                try
                {
                    ReflectionHelper.SetInstanceValue(__instance, "health", _nineLivesHealToHP);
                    ReflectionHelper.SetInstanceValue(__instance, "Health", _nineLivesHealToHP);
                    int pct = AbilityConfig.NineLivesHealPercent != null ? (int)AbilityConfig.NineLivesHealPercent.Value : 50;
                    var notifType = ReflectionHelper.FindWishType("NotificationStack");
                    if (notifType != null)
                    {
                        var instance = ReflectionHelper.GetSingletonInstance(notifType);
                        if (instance != null)
                            ReflectionHelper.InvokeMethod(instance, "SendNotification", $"Nine Lives! Survived with {pct}% HP!");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"[NineLives] Postfix error: {ex.Message}");
                }
                _nineLivesProcced = false;
                _nineLivesHealToHP = 0f;
            }

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
