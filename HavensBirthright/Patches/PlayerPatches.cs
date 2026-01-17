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
            Plugin.Log.LogInfo("InitializeAsOwner called!");

            // Reset race detection flag - allows re-detection when switching saves
            // without restarting the game
            _raceDetected = false;
            Plugin.Log.LogInfo("Race detection reset for new save load");

            DetectAndSetRace();

            // Log current race status after save is loaded
            var manager = Plugin.GetRacialBonusManager();
            if (manager != null)
            {
                var currentRace = manager.GetPlayerRace();
                var bonuses = manager.GetCurrentPlayerBonuses();
                Plugin.Log.LogInfo($"=== SAVE LOADED ===");
                Plugin.Log.LogInfo($"Current player race: {currentRace}");
                Plugin.Log.LogInfo($"Active bonuses: {bonuses.Count}");
                foreach (var bonus in bonuses)
                {
                    Plugin.Log.LogInfo($"  - {bonus.Type}: {bonus.GetFormattedValue()} ({bonus.Description})");
                }
                Plugin.Log.LogInfo($"===================");
            }
        }

        /// <summary>
        /// Backup: Also try to detect race when Initialize is called
        /// This also handles save switching - reset detection each time
        /// </summary>
        [HarmonyPatch(typeof(Player), "Initialize")]
        [HarmonyPostfix]
        public static void OnPlayerInitialize(Player __instance)
        {
            Plugin.Log.LogInfo("Player.Initialize called!");

            // Reset and re-detect on each Initialize call
            // This ensures switching saves works correctly
            if (_raceDetected)
            {
                Plugin.Log.LogInfo("Resetting race detection for potential save switch...");
                _raceDetected = false;
            }

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
                // This ensures we're in-game with a loaded save, not at the main menu
                // GameSave.CurrentCharacter is cached and contains stale data at the menu!
                if (Player.Instance == null)
                {
                    Plugin.Log.LogInfo("Race detection skipped - Player.Instance is NULL (not in-game yet)");
                    return;
                }

                var currentChar = GameSave.CurrentCharacter;
                if (currentChar == null)
                {
                    Plugin.Log.LogInfo("Race detection skipped - no CurrentCharacter");
                    return;
                }

                // Verify StyleData exists
                if (currentChar.StyleData == null || currentChar.StyleData.Count == 0)
                {
                    Plugin.Log.LogInfo("Race detection skipped - StyleData is empty");
                    return;
                }

                Plugin.Log.LogInfo("╔══════════════════════════════════════════════════════════════════╗");
                Plugin.Log.LogInfo("║          HAVEN'S BIRTHRIGHT - RACE DETECTION DIAGNOSTICS         ║");
                Plugin.Log.LogInfo("╚══════════════════════════════════════════════════════════════════╝");

                var manager = Plugin.GetRacialBonusManager();
                if (manager == null)
                {
                    Plugin.Log.LogError("!!! RacialBonusManager is NULL - cannot proceed !!!");
                    return;
                }

                // ═══════════════════════════════════════════════════════════════════
                // SECTION 1: DUMP THE Wish.Race ENUM - What values does the game have?
                // ═══════════════════════════════════════════════════════════════════
                Plugin.Log.LogInfo("");
                Plugin.Log.LogInfo("┌──────────────────────────────────────────────────────────────────┐");
                Plugin.Log.LogInfo("│ SECTION 1: Wish.Race ENUM VALUES (What the game defines)        │");
                Plugin.Log.LogInfo("└──────────────────────────────────────────────────────────────────┘");
                try
                {
                    var raceEnumType = typeof(Wish.Race);
                    var raceValues = Enum.GetValues(raceEnumType);
                    Plugin.Log.LogInfo($"  Wish.Race has {raceValues.Length} defined values:");
                    foreach (var val in raceValues)
                    {
                        int intVal = (int)(Wish.Race)val;
                        Plugin.Log.LogInfo($"    [{intVal}] = {val}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"  !!! Failed to enumerate Wish.Race: {ex.Message}");
                }

                // ═══════════════════════════════════════════════════════════════════
                // SECTION 2: CHARACTERDATA - All fields and properties
                // ═══════════════════════════════════════════════════════════════════
                Plugin.Log.LogInfo("");
                Plugin.Log.LogInfo("┌──────────────────────────────────────────────────────────────────┐");
                Plugin.Log.LogInfo("│ SECTION 2: CharacterData COMPLETE DUMP                          │");
                Plugin.Log.LogInfo("└──────────────────────────────────────────────────────────────────┘");

                byte? raceFromCurrentChar = null;
                try
                {
                    // currentChar already retrieved at the top of this method
                    var charType = currentChar.GetType();
                        Plugin.Log.LogInfo($"  CharacterData Type: {charType.FullName}");
                        Plugin.Log.LogInfo("");

                        // Get ALL fields
                        Plugin.Log.LogInfo("  ── ALL FIELDS ──");
                        var fields = charType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var field in fields)
                        {
                            try
                            {
                                var value = field.GetValue(currentChar);
                                string valueStr = value?.ToString() ?? "NULL";
                                if (valueStr.Length > 100) valueStr = valueStr.Substring(0, 100) + "...";
                                Plugin.Log.LogInfo($"    FIELD [{field.FieldType.Name}] {field.Name} = {valueStr}");
                            }
                            catch (Exception ex)
                            {
                                Plugin.Log.LogInfo($"    FIELD {field.Name} = <error: {ex.Message}>");
                            }
                        }

                        Plugin.Log.LogInfo("");
                        Plugin.Log.LogInfo("  ── ALL PROPERTIES ──");
                        var properties = charType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var prop in properties)
                        {
                            try
                            {
                                if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                                {
                                    var value = prop.GetValue(currentChar);
                                    string valueStr = value?.ToString() ?? "NULL";
                                    if (valueStr.Length > 100) valueStr = valueStr.Substring(0, 100) + "...";
                                    Plugin.Log.LogInfo($"    PROP [{prop.PropertyType.Name}] {prop.Name} = {valueStr}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Plugin.Log.LogInfo($"    PROP {prop.Name} = <error: {ex.Message}>");
                            }
                        }

                        // Get the race specifically
                        raceFromCurrentChar = currentChar.race;
                        Plugin.Log.LogInfo("");
                        Plugin.Log.LogInfo($"  ★★★ currentChar.race (byte) = {raceFromCurrentChar} ★★★");
                        Plugin.Log.LogInfo($"  ★★★ Cast to Wish.Race = {(Wish.Race)raceFromCurrentChar} ★★★");

                    // Dump StyleData (already checked it's not null at the top)
                    Plugin.Log.LogInfo("");
                    Plugin.Log.LogInfo($"  ── STYLEDATA ({currentChar.StyleData.Count} entries) ──");
                    foreach (var kvp in currentChar.StyleData)
                    {
                        Plugin.Log.LogInfo($"    StyleData[\"{kvp.Key}\"] = {kvp.Value}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"  !!! Failed to read CharacterData: {ex.Message}");
                    Plugin.Log.LogError($"      Stack: {ex.StackTrace}");
                }

                // ═══════════════════════════════════════════════════════════════════
                // SECTION 3: PLAYER INSTANCE - All components and race-related data
                // ═══════════════════════════════════════════════════════════════════
                Plugin.Log.LogInfo("");
                Plugin.Log.LogInfo("┌──────────────────────────────────────────────────────────────────┐");
                Plugin.Log.LogInfo("│ SECTION 3: PLAYER INSTANCE COMPLETE DUMP                        │");
                Plugin.Log.LogInfo("└──────────────────────────────────────────────────────────────────┘");

                try
                {
                    if (Player.Instance != null)
                    {
                        var player = Player.Instance;
                        var playerType = player.GetType();
                        Plugin.Log.LogInfo($"  Player Type: {playerType.FullName}");
                        Plugin.Log.LogInfo($"  Player GameObject: {player.gameObject?.name ?? "NULL"}");
                        Plugin.Log.LogInfo("");

                        // Look for race-related fields/properties
                        Plugin.Log.LogInfo("  ── RACE-RELATED MEMBERS (searching for 'race', 'Race', 'appearance', 'character') ──");
                        var allMembers = playerType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var member in allMembers)
                        {
                            string nameLower = member.Name.ToLowerInvariant();
                            if (nameLower.Contains("race") || nameLower.Contains("appearance") ||
                                nameLower.Contains("character") || nameLower.Contains("visual") ||
                                nameLower.Contains("skin") || nameLower.Contains("body"))
                            {
                                try
                                {
                                    if (member is FieldInfo field)
                                    {
                                        var value = field.GetValue(player);
                                        Plugin.Log.LogInfo($"    FIELD [{field.FieldType.Name}] {field.Name} = {value ?? "NULL"}");
                                    }
                                    else if (member is PropertyInfo prop && prop.CanRead && prop.GetIndexParameters().Length == 0)
                                    {
                                        var value = prop.GetValue(player);
                                        Plugin.Log.LogInfo($"    PROP [{prop.PropertyType.Name}] {prop.Name} = {value ?? "NULL"}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Plugin.Log.LogInfo($"    {member.MemberType} {member.Name} = <error: {ex.Message}>");
                                }
                            }
                        }

                        // List ALL MonoBehaviour components on the Player GameObject
                        Plugin.Log.LogInfo("");
                        Plugin.Log.LogInfo("  ── ALL COMPONENTS ON PLAYER GAMEOBJECT ──");
                        var components = player.gameObject.GetComponents<Component>();
                        foreach (var comp in components)
                        {
                            if (comp != null)
                            {
                                Plugin.Log.LogInfo($"    Component: {comp.GetType().FullName}");
                            }
                        }

                        // Also check children for appearance-related components
                        Plugin.Log.LogInfo("");
                        Plugin.Log.LogInfo("  ── CHILD OBJECTS (first 2 levels) ──");
                        for (int i = 0; i < player.transform.childCount && i < 20; i++)
                        {
                            var child = player.transform.GetChild(i);
                            Plugin.Log.LogInfo($"    Child: {child.name}");
                            var childComps = child.GetComponents<Component>();
                            foreach (var comp in childComps)
                            {
                                if (comp != null && comp.GetType().Name != "Transform")
                                {
                                    Plugin.Log.LogInfo($"      └─ {comp.GetType().Name}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Plugin.Log.LogWarning("  !!! Player.Instance is NULL !!!");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"  !!! Failed to read Player instance: {ex.Message}");
                    Plugin.Log.LogError($"      Stack: {ex.StackTrace}");
                }

                // ═══════════════════════════════════════════════════════════════════
                // SECTION 4: FINAL RACE DETERMINATION
                // ═══════════════════════════════════════════════════════════════════
                Plugin.Log.LogInfo("");
                Plugin.Log.LogInfo("┌──────────────────────────────────────────────────────────────────┐");
                Plugin.Log.LogInfo("│ SECTION 4: FINAL RACE DETERMINATION                             │");
                Plugin.Log.LogInfo("└──────────────────────────────────────────────────────────────────┘");

                byte gameRace = raceFromCurrentChar ?? 0;
                var wishRace = (Wish.Race)gameRace;
                string wishRaceName = wishRace.ToString();

                Plugin.Log.LogInfo($"  Raw byte value: {gameRace}");
                Plugin.Log.LogInfo($"  Wish.Race enum: {wishRace}");
                Plugin.Log.LogInfo($"  Wish.Race name: {wishRaceName}");

                // Detect elemental variant from StyleData
                ElementalVariant elementalVariant = ElementalVariant.None;
                if (wishRace == Wish.Race.Elemental && currentChar.StyleData != null)
                {
                    // Check StyleData for body type - key "0" contains the body sprite name
                    // e.g., "body_elemental_fire" or "body_elemental_water"
                    if (currentChar.StyleData.TryGetValue(0, out string bodyStyle))
                    {
                        string bodyLower = bodyStyle.ToLowerInvariant();
                        Plugin.Log.LogInfo($"  Elemental body style: {bodyStyle}");

                        if (bodyLower.Contains("fire"))
                        {
                            elementalVariant = ElementalVariant.Fire;
                            Plugin.Log.LogInfo($"  ★★★ DETECTED FIRE ELEMENTAL from StyleData! ★★★");
                        }
                        else if (bodyLower.Contains("water"))
                        {
                            elementalVariant = ElementalVariant.Water;
                            Plugin.Log.LogInfo($"  ★★★ DETECTED WATER ELEMENTAL from StyleData! ★★★");
                        }
                    }
                }

                // Detect Amari variant from StyleData
                AmariVariant amariVariant = AmariVariant.None;
                if (wishRace == Wish.Race.Amari && currentChar.StyleData != null)
                {
                    // Check StyleData for body type - key "0" contains the body sprite name
                    // e.g., "body_amari_cat", "body_amari_dog", etc.
                    if (currentChar.StyleData.TryGetValue(0, out string bodyStyle))
                    {
                        string bodyLower = bodyStyle.ToLowerInvariant();
                        Plugin.Log.LogInfo($"  Amari body style: {bodyStyle}");

                        if (bodyLower.Contains("cat"))
                        {
                            amariVariant = AmariVariant.Cat;
                            Plugin.Log.LogInfo($"  ★★★ DETECTED AMARI CAT from StyleData! ★★★");
                        }
                        else if (bodyLower.Contains("dog") || bodyLower.Contains("wolf") || bodyLower.Contains("canine"))
                        {
                            amariVariant = AmariVariant.Dog;
                            Plugin.Log.LogInfo($"  ★★★ DETECTED AMARI DOG from StyleData! ★★★");
                        }
                        else if (bodyLower.Contains("bird") || bodyLower.Contains("avian") || bodyLower.Contains("feather"))
                        {
                            amariVariant = AmariVariant.Bird;
                            Plugin.Log.LogInfo($"  ★★★ DETECTED AMARI BIRD from StyleData! ★★★");
                        }
                        else if (bodyLower.Contains("aquatic") || bodyLower.Contains("fish") || bodyLower.Contains("amphibian") || bodyLower.Contains("frog"))
                        {
                            amariVariant = AmariVariant.Aquatic;
                            Plugin.Log.LogInfo($"  ★★★ DETECTED AMARI AQUATIC from StyleData! ★★★");
                        }
                        else if (bodyLower.Contains("reptile") || bodyLower.Contains("lizard") || bodyLower.Contains("dragon") || bodyLower.Contains("snake"))
                        {
                            amariVariant = AmariVariant.Reptile;
                            Plugin.Log.LogInfo($"  ★★★ DETECTED AMARI REPTILE from StyleData! ★★★");
                        }
                    }
                }

                // Convert to mod race, handling elemental and amari variants
                Race modRace;
                if (wishRace == Wish.Race.Elemental && elementalVariant != ElementalVariant.None)
                {
                    modRace = elementalVariant == ElementalVariant.Fire ? Race.FireElemental : Race.WaterElemental;
                    Plugin.Log.LogInfo($"  Converted Elemental + {elementalVariant} variant to: {modRace}");
                }
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
                    Plugin.Log.LogInfo($"  Converted Amari + {amariVariant} variant to: {modRace}");
                }
                else
                {
                    modRace = ConvertGameRaceByName(wishRaceName);
                    Plugin.Log.LogInfo($"  Converted to mod Race: {modRace}");
                }

                manager.SetPlayerRace(modRace);
                _raceDetected = true;

                Plugin.Log.LogInfo("");
                Plugin.Log.LogInfo("╔══════════════════════════════════════════════════════════════════╗");
                Plugin.Log.LogInfo($"║  FINAL RESULT: Player is a {modRace,-20}                    ║");
                Plugin.Log.LogInfo("╚══════════════════════════════════════════════════════════════════╝");
                Plugin.Log.LogInfo("");
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
