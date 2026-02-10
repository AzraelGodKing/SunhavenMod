using HarmonyLib;
using SunhavenMods.Shared;
using System;
using System.Collections.Generic;
using Wish;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Patches for player race detection.
    /// Hooks into Sun Haven's Wish.Player class to detect the player's race on load.
    ///
    /// Two-tier detection:
    ///   Tier 1: characterData.race byte → Wish.Race (Human/Elf/Amari/Naga/Elemental/Angel/Demon)
    ///   Tier 2: ClothingLayerData.subRace for Amari variants, string parsing for Elemental variants
    ///
    /// NOTE: All stat modifications are handled by StatPatches.ModifyGetStat which provides
    /// a single unified pipeline (base bonuses + abilities + drawbacks + synergies).
    /// Do NOT add [HarmonyPatch] GetStat/MaxHealth/MaxMana/AddExperience/AddMoney patches here
    /// as they would double-apply bonuses and cause game freezes.
    /// </summary>
    public static class PlayerPatches
    {
        private static bool _raceDetected = false;

        // Game's SubRace enum values (Wish.SubRace)
        // We store these as ints so we don't need a direct reference to the enum type
        private const int SUBRACE_DEFAULT = 0;
        private const int SUBRACE_CAT = 1;
        private const int SUBRACE_DOG = 2;
        private const int SUBRACE_BIRD = 3;
        private const int SUBRACE_AQUATIC = 4;
        private const int SUBRACE_GREEN_REPTILE = 5;
        private const int SUBRACE_ORANGE_REPTILE = 6;

        /// <summary>
        /// Detect player race when the player is initialized as owner (game load)
        /// </summary>
        public static void OnPlayerInitialized(Player __instance, bool host)
        {
            // Ensure BirthrightRunner exists — it gets destroyed by UIHandler.UnloadGame()
            // when returning to main menu. Must recreate before race detection.
            Plugin.EnsureRunner();

            // Reset race detection flag - allows re-detection when switching saves
            _raceDetected = false;
            DetectAndSetRace();
        }

        /// <summary>
        /// Backup: Also try to detect race when Initialize is called
        /// Only detect if not already detected (InitializeAsOwner handles the reset)
        /// </summary>
        public static void OnPlayerInitialize(Player __instance)
        {
            // Only try to detect if not already done
            if (!_raceDetected)
                DetectAndSetRace();
        }

        /// <summary>
        /// Common method to detect and set the player's race using two-tier approach:
        ///   Tier 1: characterData.race → base Wish.Race
        ///   Tier 2: ClothingLayerData.subRace (Amari) or string parsing (Elemental)
        /// Result is cached in RacialBonusManager — only runs once per save load.
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

                // ===== TIER 1: Base race from characterData.race byte =====
                byte gameRace = currentChar.race;
                var wishRace = (Wish.Race)gameRace;
                Plugin.Log.LogInfo($"[RaceDetection] Tier 1 - Game race byte: {gameRace}, Wish.Race: {wishRace}");

                // Get body style name from StyleData[14] (ClothingLayer.Body = 14)
                string bodyStyleName = null;
                currentChar.StyleData.TryGetValue(14, out bodyStyleName);

                // ===== TIER 2: Variant detection based on base race =====
                Race modRace;
                int detectedSubRace = -1;

                switch (wishRace)
                {
                    case Wish.Race.Amari:
                        // Try ClothingLayerData.subRace first (reliable), fall back to string parsing
                        detectedSubRace = TryGetSubRaceFromClothingData(bodyStyleName);
                        if (detectedSubRace >= 0)
                        {
                            Plugin.Log.LogInfo($"[RaceDetection] Tier 2 - Amari SubRace from ClothingData: {detectedSubRace}");
                            modRace = ResolveAmariRace(detectedSubRace);
                        }
                        else
                        {
                            // Fallback: string parsing of body style name
                            Plugin.Log.LogInfo($"[RaceDetection] Tier 2 - ClothingData lookup failed, falling back to string parsing");
                            modRace = ResolveAmariFromString(bodyStyleName);
                        }
                        break;

                    case Wish.Race.Elemental:
                        // SubRace is Default for both Fire/Water — must use string parsing
                        modRace = ResolveElementalFromString(bodyStyleName);
                        break;

                    case Wish.Race.Naga:
                        // No variant detection needed — all Naga are treated the same
                        modRace = Race.Naga;
                        // Still try to read SubRace for logging/caching
                        detectedSubRace = TryGetSubRaceFromClothingData(bodyStyleName);
                        Plugin.Log.LogInfo($"[RaceDetection] Tier 2 - Naga SubRace: {detectedSubRace} (no variant needed)");
                        break;

                    default:
                        // Human, Elf, Angel, Demon — direct mapping, no variants
                        modRace = ConvertGameRaceByName(wishRace.ToString());
                        break;
                }

                // Store the result — cached for the entire session until save switch
                manager.SetPlayerRace(modRace, detectedSubRace);
                _raceDetected = true;
                Plugin.Log.LogInfo($"Player race set to: {modRace} (SubRace cached: {detectedSubRace})");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"!!! CRITICAL ERROR in race detection: {ex.Message}");
                Plugin.Log.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        // ===================== TIER 2: ClothingLayerData SubRace Lookup =====================

        /// <summary>
        /// Tries to get the SubRace value from the game's ClothingLayerData for the given body style.
        /// Access pattern: CharacterClothingStyles.ClothingStyles[ClothingLayer.Body][bodyStyleName].subRace
        /// Returns the SubRace int value, or -1 if the lookup fails.
        /// </summary>
        private static int TryGetSubRaceFromClothingData(string bodyStyleName)
        {
            if (string.IsNullOrEmpty(bodyStyleName))
                return -1;

            try
            {
                // Get CharacterClothingStyles type and its static ClothingStyles dictionary
                var clothingStylesType = ReflectionHelper.FindWishType("CharacterClothingStyles");
                if (clothingStylesType == null)
                {
                    Plugin.Log.LogDebug("[RaceDetection] Could not find CharacterClothingStyles type");
                    return -1;
                }

                // ClothingStyles is a static field: Dictionary<ClothingLayer, Dictionary<string, ClothingLayerData>>
                var clothingStyles = ReflectionHelper.GetStaticValue(clothingStylesType, "ClothingStyles");
                if (clothingStyles == null)
                {
                    Plugin.Log.LogDebug("[RaceDetection] ClothingStyles is null");
                    return -1;
                }

                // Cast to Dictionary<ClothingLayer, Dictionary<string, ClothingLayerData>>
                // We use reflection to be safe since we don't directly reference ClothingLayerData
                var dictType = clothingStyles.GetType();

                // Access the Body layer entry: ClothingStyles[ClothingLayer.Body]
                // ClothingLayer.Body = 14
                var bodyLayerDict = ReflectionHelper.InvokeMethod(clothingStyles, "get_Item", (ClothingLayer)14);
                if (bodyLayerDict == null)
                {
                    Plugin.Log.LogDebug("[RaceDetection] No Body layer in ClothingStyles");
                    return -1;
                }

                // Check if the body style name exists in the dictionary
                var containsMethod = bodyLayerDict.GetType().GetMethod("ContainsKey");
                if (containsMethod != null)
                {
                    bool contains = (bool)containsMethod.Invoke(bodyLayerDict, new object[] { bodyStyleName });
                    if (!contains)
                    {
                        Plugin.Log.LogDebug($"[RaceDetection] Body style '{bodyStyleName}' not found in ClothingStyles[Body]");
                        return -1;
                    }
                }

                // Get the ClothingLayerData for this body style
                var clothingLayerData = ReflectionHelper.InvokeMethod(bodyLayerDict, "get_Item", bodyStyleName);
                if (clothingLayerData == null)
                {
                    Plugin.Log.LogDebug($"[RaceDetection] ClothingLayerData is null for '{bodyStyleName}'");
                    return -1;
                }

                // Read the subRace field (public SubRace subRace)
                var subRaceValue = ReflectionHelper.GetInstanceValue(clothingLayerData, "subRace");
                if (subRaceValue == null)
                {
                    Plugin.Log.LogDebug("[RaceDetection] subRace field not found on ClothingLayerData");
                    return -1;
                }

                // SubRace is an enum — convert to int
                int subRaceInt = (int)subRaceValue;
                Plugin.Log.LogInfo($"[RaceDetection] ClothingLayerData.subRace = {subRaceValue} ({subRaceInt})");
                return subRaceInt;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[RaceDetection] ClothingLayerData lookup failed: {ex.Message}");
                return -1;
            }
        }

        // ===================== VARIANT RESOLVERS =====================

        /// <summary>
        /// Maps a Wish.SubRace int value to a mod Race for Amari characters.
        /// </summary>
        private static Race ResolveAmariRace(int subRace)
        {
            switch (subRace)
            {
                case SUBRACE_CAT:
                    return Race.AmariCat;
                case SUBRACE_DOG:
                    return Race.AmariDog;
                case SUBRACE_BIRD:
                    return Race.AmariBird;
                case SUBRACE_AQUATIC:
                    return Race.AmariAquatic;
                case SUBRACE_GREEN_REPTILE:
                case SUBRACE_ORANGE_REPTILE:
                    return Race.AmariReptile;
                default:
                    Plugin.Log.LogWarning($"[RaceDetection] Unknown Amari SubRace: {subRace}, using generic Amari");
                    return Race.Amari;
            }
        }

        /// <summary>
        /// Fallback: resolve Amari variant from body style name string parsing.
        /// Used when ClothingLayerData lookup fails.
        /// </summary>
        private static Race ResolveAmariFromString(string bodyStyleName)
        {
            if (string.IsNullOrEmpty(bodyStyleName))
            {
                Plugin.Log.LogWarning("[RaceDetection] No body style name for Amari, using generic");
                return Race.Amari;
            }

            Plugin.Log.LogInfo($"[RaceDetection] Amari string fallback, body style: {bodyStyleName}");
            string bodyLower = bodyStyleName.ToLowerInvariant();

            if (bodyLower.Contains("cat"))
                return Race.AmariCat;
            if (bodyLower.Contains("dog") || bodyLower.Contains("wolf") || bodyLower.Contains("canine"))
                return Race.AmariDog;
            if (bodyLower.Contains("bird") || bodyLower.Contains("avian") || bodyLower.Contains("feather"))
                return Race.AmariBird;
            if (bodyLower.Contains("aquatic") || bodyLower.Contains("fish") || bodyLower.Contains("amphibian") || bodyLower.Contains("frog"))
                return Race.AmariAquatic;
            if (bodyLower.Contains("reptile") || bodyLower.Contains("lizard") || bodyLower.Contains("dragon") || bodyLower.Contains("snake"))
                return Race.AmariReptile;

            Plugin.Log.LogWarning($"[RaceDetection] Could not determine Amari variant from '{bodyStyleName}', using generic");
            return Race.Amari;
        }

        /// <summary>
        /// Resolve Elemental variant from body style name string parsing.
        /// SubRace is Default for both Fire/Water Elementals, so string parsing is required.
        /// </summary>
        private static Race ResolveElementalFromString(string bodyStyleName)
        {
            if (string.IsNullOrEmpty(bodyStyleName))
            {
                Plugin.Log.LogWarning("[RaceDetection] No body style name for Elemental, using generic");
                return Race.Elemental;
            }

            Plugin.Log.LogInfo($"[RaceDetection] Elemental body style (key 14): {bodyStyleName}");
            string bodyLower = bodyStyleName.ToLowerInvariant();

            if (bodyLower.Contains("fire"))
                return Race.FireElemental;
            if (bodyLower.Contains("water"))
                return Race.WaterElemental;

            Plugin.Log.LogWarning($"[RaceDetection] Could not determine Elemental variant from '{bodyStyleName}', using generic");
            return Race.Elemental;
        }

        /// <summary>
        /// Reset race detection flag when returning to menu
        /// </summary>
        public static void ResetRaceDetection()
        {
            _raceDetected = false;
        }

        /// <summary>
        /// Retry race detection if it hasn't succeeded yet.
        /// Called from BirthrightRunner.OnUpdate() when race is null — handles the case where
        /// both Harmony patches fire before Player.Instance is ready.
        /// </summary>
        public static void RetryRaceDetection()
        {
            if (!_raceDetected)
                DetectAndSetRace();
        }

        /// <summary>
        /// Convert the game's race name string to our mod's Race enum.
        /// Used for simple races with no variants (Human, Elf, Angel, Demon).
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
            if (normalized.Contains("naga"))
                return Race.Naga;

            // These shouldn't be reached via this method (handled by resolvers above)
            // but keep them for safety
            if (normalized.Contains("fire") && normalized.Contains("element"))
                return Race.FireElemental;
            if (normalized.Contains("water") && normalized.Contains("element"))
                return Race.WaterElemental;
            if (normalized.Contains("element"))
                return Race.Elemental;
            if (normalized.Contains("amari"))
                return Race.Amari;

            Plugin.Log.LogWarning($"Unknown race name: {raceName}, defaulting to Human");
            return Race.Human;
        }
    }
}
