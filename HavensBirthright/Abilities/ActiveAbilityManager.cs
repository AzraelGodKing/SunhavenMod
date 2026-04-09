using System.Collections.Generic;
using UnityEngine;

namespace HavensBirthright.Abilities
{
    /// <summary>
    /// Tracks ability state: timed buffs, stacking effects, and cooldowns.
    /// Used by BirthrightRunner (auto-triggers) and patches (condition checks).
    /// </summary>
    public static class ActiveAbilityManager
    {
        /// <summary>
        /// Represents an active timed buff/ability state
        /// </summary>
        public class AbilityState
        {
            public bool IsActive { get; set; }
            public float ExpiryTime { get; set; }
            public float CooldownUntil { get; set; }
            public int Stacks { get; set; }
            public float LastStackTime { get; set; }
            public float BonusValue { get; set; }
        }

        private static readonly Dictionary<string, AbilityState> _abilities = new Dictionary<string, AbilityState>();

        // Runtime toggles: F9-able abilities start OFF until the player enables them; each key press
        // flips ON<->OFF (unlimited times per session). Dictionary cleared on menu/character reset.
        private static readonly Dictionary<string, bool> _runtimeToggles = new Dictionary<string, bool>();

        // Ability keys
        public const string DivineWard = "DivineWard";
        public const string BloodRage = "BloodRage";
        public const string HardenedScales = "HardenedScales";
        public const string TailwindOutdoor = "TailwindOutdoor";
        public const string TidalBlessing = "TidalBlessing";
        public const string InfernalForge = "InfernalForge";
        public const string QuickLearner = "QuickLearner";
        public const string SerpentGrace = "SerpentGrace";
        public const string ElementalResonance = "ElementalResonance";
        public const string LoyaltyAura = "LoyaltyAura";
        // Celestial Bloodlines
        public const string FontOfLight = "FontOfLight";
        public const string SoulHarvest = "SoulHarvest";

        /// <summary>
        /// Gets or creates an ability state entry
        /// </summary>
        public static AbilityState GetState(string abilityKey)
        {
            if (!_abilities.TryGetValue(abilityKey, out var state))
            {
                state = new AbilityState();
                _abilities[abilityKey] = state;
            }
            return state;
        }

        /// <summary>
        /// Activates a timed ability buff
        /// </summary>
        public static bool ActivateAbility(string abilityKey, float duration, float cooldown)
        {
            var state = GetState(abilityKey);

            // Check cooldown
            if (Time.time < state.CooldownUntil)
                return false;

            state.IsActive = true;
            state.ExpiryTime = Time.time + duration;
            state.CooldownUntil = Time.time + cooldown;
            return true;
        }

        /// <summary>
        /// Checks if an ability is currently active (handles expiry)
        /// </summary>
        public static bool IsAbilityActive(string abilityKey)
        {
            var state = GetState(abilityKey);
            if (state.IsActive && Time.time >= state.ExpiryTime)
            {
                state.IsActive = false;
            }
            return state.IsActive;
        }

        /// <summary>
        /// Checks if an ability is on cooldown
        /// </summary>
        public static bool IsOnCooldown(string abilityKey)
        {
            var state = GetState(abilityKey);
            return Time.time < state.CooldownUntil;
        }

        /// <summary>
        /// Adds a stack to a stacking ability, returns current stack count.
        /// Stacks decay after stackDuration seconds without a new stack.
        /// </summary>
        public static int AddStack(string abilityKey, int maxStacks, float stackDuration)
        {
            var state = GetState(abilityKey);

            // Check if stacks have expired
            if (state.Stacks > 0 && Time.time - state.LastStackTime > stackDuration)
            {
                state.Stacks = 0;
            }

            if (state.Stacks < maxStacks)
            {
                state.Stacks++;
            }
            state.LastStackTime = Time.time;
            return state.Stacks;
        }

        /// <summary>
        /// Gets current stack count (handles decay)
        /// </summary>
        public static int GetStacks(string abilityKey, float stackDuration)
        {
            var state = GetState(abilityKey);
            if (state.Stacks > 0 && Time.time - state.LastStackTime > stackDuration)
            {
                state.Stacks = 0;
            }
            return state.Stacks;
        }

        /// <summary>
        /// Sets a bonus value for an ability (used for tracked calculations like outdoor time)
        /// </summary>
        public static void SetBonusValue(string abilityKey, float value)
        {
            var state = GetState(abilityKey);
            state.BonusValue = value;
        }

        /// <summary>
        /// Gets a stored bonus value for an ability
        /// </summary>
        public static float GetBonusValue(string abilityKey)
        {
            return GetState(abilityKey).BonusValue;
        }

        // ===================== RUNTIME TOGGLE METHODS =====================

        /// <summary>
        /// Checks if an ability is runtime-enabled for this session. Defaults to false (off) until
        /// the player presses the toggle key (default F9). Config must still allow active abilities.
        /// </summary>
        public static bool IsRuntimeEnabled(string abilityKey)
        {
            return _runtimeToggles.TryGetValue(abilityKey, out bool enabled) && enabled;
        }

        /// <summary>
        /// Toggles the runtime-enabled state of an ability. Returns the new state.
        /// </summary>
        public static bool ToggleRuntime(string abilityKey)
        {
            bool current = IsRuntimeEnabled(abilityKey);
            _runtimeToggles[abilityKey] = !current;
            return !current;
        }

        /// <summary>
        /// Resets all ability states and runtime toggles (call on menu transition)
        /// </summary>
        public static void ResetAll()
        {
            _abilities.Clear();
            _runtimeToggles.Clear();
        }
    }
}
