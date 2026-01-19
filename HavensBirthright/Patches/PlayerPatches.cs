using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using Wish;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Patches for player-related mechanics
    /// Hooks into Sun Haven's Wish.Player class to apply racial bonuses
    /// </summary>
    public static class PlayerPatches
    {
        private static bool _raceDetected = false;

        /// <summary>
        /// Detect player race when the player is initialized as owner (game load)
        /// </summary>
        [HarmonyPatch(typeof(Player), "InitializeAsOwner")]
        [HarmonyPostfix]
        public static void OnPlayerInitialized(Player __instance, bool host)
        {
            // Reset race detection flag - allows re-detection when switching saves
            _raceDetected = false;
            DetectAndSetRace();
        }

        /// <summary>
        /// Backup: Also try to detect race when Initialize is called
        /// Only detect if not already detected (InitializeAsOwner handles the reset)
        /// </summary>
        [HarmonyPatch(typeof(Player), "Initialize")]
        [HarmonyPostfix]
        public static void OnPlayerInitialize(Player __instance)
        {
            // Only try to detect if not already done
            if (!_raceDetected)
                DetectAndSetRace();
        }

        /// <summary>
        /// Common method to detect and set the player's race
        /// </summary>
        private static void DetectAndSetRace()
        {
            if (_raceDetected)
                return;

            try
            {
                // CRITICAL: Only detect race when Player.Instance actually exists
                if (Player.Instance == null)
                    return;

                var currentChar = GameSave.CurrentCharacter;
                if (currentChar == null)
                    return;

                // Verify StyleData exists
                if (currentChar.StyleData == null || currentChar.StyleData.Count == 0)
                    return;

                var manager = Plugin.GetRacialBonusManager();
                if (manager == null)
                {
                    Plugin.Log.LogError("RacialBonusManager is NULL - cannot proceed");
                    return;
                }

                // Get the race from CharacterData
                byte gameRace = currentChar.race;
                var wishRace = (Wish.Race)gameRace;
                string wishRaceName = wishRace.ToString();

                // Detect elemental variant from StyleData
                ElementalVariant elementalVariant = ElementalVariant.None;
                if (wishRace == Wish.Race.Elemental && currentChar.StyleData != null)
                {
                    if (currentChar.StyleData.TryGetValue(0, out string bodyStyle))
                    {
                        string bodyLower = bodyStyle.ToLowerInvariant();
                        if (bodyLower.Contains("fire"))
                            elementalVariant = ElementalVariant.Fire;
                        else if (bodyLower.Contains("water"))
                            elementalVariant = ElementalVariant.Water;
                    }
                }

                // Detect Amari variant from StyleData
                AmariVariant amariVariant = AmariVariant.None;
                if (wishRace == Wish.Race.Amari && currentChar.StyleData != null)
                {
                    if (currentChar.StyleData.TryGetValue(0, out string bodyStyle))
                    {
                        string bodyLower = bodyStyle.ToLowerInvariant();
                        if (bodyLower.Contains("cat"))
                            amariVariant = AmariVariant.Cat;
                        else if (bodyLower.Contains("dog") || bodyLower.Contains("wolf") || bodyLower.Contains("canine"))
                            amariVariant = AmariVariant.Dog;
                        else if (bodyLower.Contains("bird") || bodyLower.Contains("avian") || bodyLower.Contains("feather"))
                            amariVariant = AmariVariant.Bird;
                        else if (bodyLower.Contains("aquatic") || bodyLower.Contains("fish") || bodyLower.Contains("amphibian") || bodyLower.Contains("frog"))
                            amariVariant = AmariVariant.Aquatic;
                        else if (bodyLower.Contains("reptile") || bodyLower.Contains("lizard") || bodyLower.Contains("dragon") || bodyLower.Contains("snake"))
                            amariVariant = AmariVariant.Reptile;
                    }
                }

                // Convert to mod race, handling elemental and amari variants
                Race modRace;
                if (wishRace == Wish.Race.Elemental && elementalVariant != ElementalVariant.None)
                    modRace = elementalVariant == ElementalVariant.Fire ? Race.FireElemental : Race.WaterElemental;
                else if (wishRace == Wish.Race.Amari && amariVariant != AmariVariant.None)
                {
                    modRace = amariVariant switch
                    {
                        AmariVariant.Cat => Race.AmariCat,
                        AmariVariant.Dog => Race.AmariDog,
                        AmariVariant.Bird => Race.AmariBird,
                        AmariVariant.Aquatic => Race.AmariAquatic,
                        AmariVariant.Reptile => Race.AmariReptile,
                        _ => Race.Amari
                    };
                }
                else
                    modRace = ConvertGameRaceByName(wishRaceName);

                manager.SetPlayerRace(modRace);
                _raceDetected = true;
                Plugin.Log.LogInfo($"Player race set to: {modRace}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"!!! CRITICAL ERROR in race detection: {ex.Message}");
                Plugin.Log.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Reset race detection flag when returning to menu
        /// </summary>
        public static void ResetRaceDetection()
        {
            _raceDetected = false;
        }

        /// <summary>
        /// Convert the game's race name string to our mod's Race enum
        /// </summary>
        private static Race ConvertGameRaceByName(string raceName)
        {
            // Normalize the name for comparison
            string normalized = raceName?.ToLowerInvariant() ?? "";

            if (normalized.Contains("human"))
                return Race.Human;
            if (normalized.Contains("elf"))
                return Race.Elf;
            if (normalized.Contains("angel"))
                return Race.Angel;
            if (normalized.Contains("demon"))
                return Race.Demon;
            if (normalized.Contains("fire") && normalized.Contains("element"))
                return Race.FireElemental;
            if (normalized.Contains("water") && normalized.Contains("element"))
                return Race.WaterElemental;
            if (normalized.Contains("element"))
                return Race.Elemental;
            if (normalized.Contains("amari"))
                return Race.Amari;
            if (normalized.Contains("naga"))
                return Race.Naga;

            Plugin.Log.LogWarning($"Unknown race name: {raceName}, defaulting to Human");
            return Race.Human;
        }

        /// <summary>
        /// Convert the game's race byte value to our mod's Race enum
        /// Game race values (from dnSpy):
        /// 0 = Human, 1 = Elf, 2 = Amari, 3 = Naga, 4 = Elemental, 5 = Angel, 6 = Demon
        /// </summary>
        private static Race ConvertGameRaceToModRace(int gameRace)
        {
            return gameRace switch
            {
                0 => Race.Human,
                1 => Race.Elf,
                2 => Race.Amari,
                3 => Race.Naga,
                4 => Race.Elemental,  // TODO: Detect Fire/Water variant if possible
                5 => Race.Angel,
                6 => Race.Demon,
                _ => Race.Human
            };
        }

        /// <summary>
        /// Main stat modification patch - applies racial bonuses to all relevant stats
        /// This intercepts Player.GetStat(StatType) to modify the returned values
        /// </summary>
        [HarmonyPatch(typeof(Player), "GetStat")]
        [HarmonyPostfix]
        public static void ModifyGetStat(Player __instance, StatType statType, ref float __result)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            // Map StatType to our BonusType and apply the bonus
            BonusType? bonusType = MapStatTypeToBonusType(statType);
            if (bonusType.HasValue && manager.HasBonus(bonusType.Value))
            {
                float originalValue = __result;
                __result = manager.ApplyBonus(__result, bonusType.Value);

                // Debug logging (can be removed later)
                if (__result != originalValue)
                {
                    Plugin.Log.LogDebug($"Applied {bonusType.Value} bonus to {statType}: {originalValue} -> {__result}");
                }
            }
        }

        /// <summary>
        /// Maps the game's StatType enum to our mod's BonusType enum
        /// Based on decompiled game code - actual StatType values
        /// </summary>
        private static BonusType? MapStatTypeToBonusType(StatType statType)
        {
            return statType switch
            {
                // Combat stats
                StatType.AttackDamage => BonusType.MeleeStrength,
                StatType.SpellDamage => BonusType.MagicPower,
                StatType.Defense => BonusType.Defense,
                StatType.Crit => BonusType.CriticalChance,
                StatType.AttackSpeed => BonusType.AttackSpeed,
                StatType.SpellAttackSpeed => BonusType.AttackSpeed, // Magic attack speed too

                // Health/Mana stats
                StatType.Health => BonusType.MaxHealth,
                StatType.Mana => BonusType.MaxMana,
                StatType.HealthRegen => BonusType.HealthRegen,
                StatType.ManaRegen => BonusType.ManaRegen,

                // Movement
                StatType.Movespeed => BonusType.MovementSpeed,

                // Gathering/Skill stats - these affect the skill level calculations
                StatType.MiningSkill => BonusType.MiningSpeed,
                StatType.MiningCrit => BonusType.MiningYield,
                StatType.WoodcuttingCrit => BonusType.WoodcuttingYield,
                StatType.FishingSkill => BonusType.FishingSpeed,
                StatType.FarmingSkill => BonusType.FarmingSpeed,
                StatType.SmithingSkill => BonusType.CraftingSpeed,

                // Foraging/Exploration
                StatType.ExplorationSkill => BonusType.ForagingChance,
                StatType.ExtraForageableChance => BonusType.ForagingChance,

                // Economy stats
                StatType.GoldGain => BonusType.GoldFind,
                StatType.BonusExperience => BonusType.ExperienceGain,
                StatType.BonusFarmingEXP => BonusType.ExperienceGain,
                StatType.BonusWoodcuttingEXP => BonusType.ExperienceGain,

                _ => null
            };
        }

        /// <summary>
        /// Patch max health getter (backup for races with MaxHealth bonus)
        /// MaxHealth uses GetStat(StatType.Health)
        /// </summary>
        [HarmonyPatch(typeof(Player), "get_MaxHealth")]
        [HarmonyPostfix]
        public static void ModifyMaxHealth(ref float __result)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager != null && manager.HasBonus(BonusType.MaxHealth))
            {
                __result = manager.ApplyBonus(__result, BonusType.MaxHealth);
            }
        }

        /// <summary>
        /// Patch max mana getter (Angel bonus)
        /// MaxMana uses GetStat(StatType.Mana)
        /// </summary>
        [HarmonyPatch(typeof(Player), "get_MaxMana")]
        [HarmonyPostfix]
        public static void ModifyMaxMana(ref float __result)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager != null && manager.HasBonus(BonusType.MaxMana))
            {
                __result = manager.ApplyBonus(__result, BonusType.MaxMana);
            }
        }

        /// <summary>
        /// Patch experience gain (Human bonus)
        /// Game uses: amount * (1f + GetStat(StatType.BonusExperience)) * (1f + num)
        /// </summary>
        [HarmonyPatch(typeof(Player), "AddExperience")]
        [HarmonyPrefix]
        public static void ModifyExperienceGain(ref float amount)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager != null && manager.HasBonus(BonusType.ExperienceGain))
            {
                float bonus = manager.GetBonusValue(BonusType.ExperienceGain);
                amount *= (1f + bonus / 100f);
            }
        }

        /// <summary>
        /// Patch gold gain (Demon bonus)
        /// Game uses: (1f + GetStat(StatType.GoldGain)) * amount
        /// </summary>
        [HarmonyPatch(typeof(Player), "AddMoney")]
        [HarmonyPrefix]
        public static void ModifyGoldGain(ref int amount)
        {
            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            // Only apply to positive amounts (gains, not spending)
            if (amount <= 0)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager != null && manager.HasBonus(BonusType.GoldFind))
            {
                float bonus = manager.GetBonusValue(BonusType.GoldFind);
                amount = Mathf.RoundToInt(amount * (1f + bonus / 100f));
            }
        }

        // TODO: Add RelationshipGain patch once correct NPC class is identified
        // Human and Amari Dog have RelationshipGain bonus that needs patching

        // TODO: Add ShopDiscount patch once correct Shop class is identified
        // Human has ShopDiscount bonus that needs patching

        // TODO: Add FishingLuck patch - Water Elemental, Amari Aquatic, Naga
        // TODO: Add LuckBonus patch - Angel, Amari Cat, Amari Bird
        // TODO: Add CropQuality patch - Elf
    }
}
