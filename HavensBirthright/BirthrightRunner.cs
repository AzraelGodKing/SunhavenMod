using HarmonyLib;
using HavensBirthright.Abilities;
using HavensBirthright.Patches;
using SunhavenMods.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HavensBirthright
{
    /// <summary>
    /// Persistent MonoBehaviour that powers auto-triggered abilities.
    /// Runs every frame to check conditions, manage cooldowns, and track environmental state.
    /// Also caches expensive reflection lookups once per frame for StatPatches to read.
    /// </summary>
    public class BirthrightRunner : PersistentRunnerBase
    {
        protected override string RunnerName => "BirthrightRunner";

        private float _outdoorTime = 0f;
        private float _lastTidalBlessingCheck = 0f;
        private float _lastInfernalForgeCheck = 0f;
        private float _lastFontOfLightCheck = 0f;

        // Cached reflection references for game APIs (initialized once)
        private bool _apiCacheInitialized = false;
        private Type _cropType;
        private Type _dayCycleType;

        // Cached TileManager references for Tidal Blessing (tile-based watering)
        private Type _tileManagerType;
        private object _tileManagerInstance;
        private MethodInfo _isWateredMethod;     // TileManager.IsWatered(Vector2Int) → bool
        private MethodInfo _waterTileMethod;     // TileManager.Water(Vector2Int, Int16) → bool
        private MethodInfo _isHoedOrWateredMethod; // TileManager.IsHoedOrWatered(Vector2Int) → bool
        private FieldInfo _farmingDataField;     // TileManager.farmingData (Dictionary<Vector2Int, FarmingTileInfo>)
        private FieldInfo _farmingTileMapField;  // TileManager.farmingTileMap (Tilemap)
        private MethodInfo _worldToCellMethod;   // Tilemap.WorldToCell(Vector3) → Vector3Int
        private object _gridInstance;            // Parent Grid component (from tilemap.layoutGrid)
        private MethodInfo _gridCellToWorldMethod; // Grid.CellToWorld(Vector3Int) → Vector3

        // One-time diagnostic flag (logs all gate checks on first call after toggle ON)
        private bool _tidalBlessingDiagLogged = false;

        // Cached inventory methods for Infernal Forge periodic scan
        private MethodInfo _cachedGetAmountMethod;
        private MethodInfo _cachedRemoveItemMethod;
        private bool _inventoryMethodsCached = false;

        // === Per-frame cached values (updated once in OnUpdate, read by StatPatches) ===
        private static float _cachedHPRatio = 1f;
        private static string _cachedSeason = null;
        private static bool _cachedIsDaytime = true;
        private static bool _cachedIsInMine = false;
        private static float _cachedQuickLearnerBonus = 0f;
        private static bool _cacheValid = false;

        // Public read-only access for StatPatches (zero-cost static field reads)
        public static float CachedHPRatio => _cachedHPRatio;
        public static string CachedSeason => _cachedSeason;
        public static bool CachedIsDaytime => _cachedIsDaytime;
        public static bool CachedIsInMine => _cachedIsInMine;
        public static float CachedQuickLearnerBonus => _cachedQuickLearnerBonus;
        public static bool IsCacheValid => _cacheValid;

        protected override void OnUpdate()
        {
            if (!SceneHelpers.IsInGame())
                return;

            // Check ability toggle hotkey (runs before config/race checks so user
            // can pre-toggle even when abilities haven't activated yet)
            CheckAbilityToggleHotkey();

            // Reload config from file (e.g. after editing the .cfg)
            if (Plugin.StaticReloadConfigKey != KeyCode.None && UnityEngine.Input.GetKeyDown(Plugin.StaticReloadConfigKey))
            {
                Plugin.Instance?.ReloadConfig();
            }

            if (!RacialConfig.EnableRacialBonuses.Value)
                return;

            var manager = Plugin.GetRacialBonusManager();
            if (manager == null)
                return;

            var race = manager.GetPlayerRace();
            if (!race.HasValue)
            {
                // Race detection may have failed during Harmony patches (Player.Instance not ready yet).
                // Retry here — once it succeeds, _raceDetected locks in and this stops retrying.
                PlayerPatches.RetryRaceDetection();
                race = manager.GetPlayerRace();
                if (!race.HasValue)
                    return;
            }

            // Initialize API caches on first in-game update
            // NOTE: This runs BEFORE the active abilities check because
            // stat caching needs _dayCycleType even when abilities are disabled
            if (!_apiCacheInitialized)
            {
                InitializeApiCache();
                _apiCacheInitialized = true;
            }

            // Update per-frame cache for StatPatches. Run every 4th frame to reduce reflection cost
            // and stutter (e.g. Amari Cat + scythe); values are at most 3 frames stale.
            if (!_cacheValid || (Time.frameCount & 3) == 0)
                UpdateStatCache(race.Value);

            // Active abilities require their own config toggle
            if (!AbilityConfig.EnableActiveAbilities.Value)
                return;

            // Track outdoor time for Amari Bird Tailwind
            if (race.Value == Race.AmariBird && AbilityConfig.EnableTailwind.Value)
            {
                UpdateTailwind();
            }

            // Water Elemental Tidal Blessing - auto-water nearby plots
            if (race.Value == Race.WaterElemental && AbilityConfig.EnableTidalBlessing.Value)
            {
                UpdateTidalBlessing();
            }

            // Fire Elemental Infernal Forge - periodic inventory scan for ore → bars
            if ((race.Value == Race.FireElemental || race.Value == Race.Elemental) && AbilityConfig.EnableInfernalForge.Value)
            {
                UpdateInfernalForge();
            }

            // Angel only - Font of Light: periodic mana restore at gold cost (Celestial)
            if (race.Value == Race.Angel && AbilityConfig.EnableFontOfLight.Value)
            {
                UpdateFontOfLight();
            }
        }

        protected override void OnMenuTransition()
        {
            ResetAllStateForNewSave();
        }

        protected override void OnGameTransition()
        {
            ResetInstanceState();
        }

        /// <summary>
        /// Resets all mod state when a new save is loaded. Call from PlayerPatches.OnPlayerInitialized
        /// so the mod works correctly when switching saves without going through the main menu
        /// (e.g. load-screen flows that skip OnMenuTransition).
        /// </summary>
        public static void ResetAllStateForNewSave()
        {
            ActiveAbilityManager.ResetAll();
            var manager = Plugin.GetRacialBonusManager();
            manager?.ClearPlayerRace();
            PlayerPatches.ResetRaceDetection();
            Patches.CombatPatches.ResetNineLives();
            AbilityPatches.ResetNotificationCache();
            AbilityPatches.ResetReflectionCache();

            var runner = Plugin.GetRunner();
            runner?.ResetInstanceState();
        }

        /// <summary>
        /// Resets instance-level caches and state. Called on menu transition and when loading a new save.
        /// </summary>
        public void ResetInstanceState()
        {
            _outdoorTime = 0f;
            _lastTidalBlessingCheck = 0f;
            _lastInfernalForgeCheck = 0f;
            _lastFontOfLightCheck = 0f;
            _apiCacheInitialized = false;
            _inventoryMethodsCached = false;
            _cachedGetAmountMethod = null;
            _cachedRemoveItemMethod = null;
            _tileManagerInstance = null;
            _worldToCellMethod = null;
            _gridInstance = null;
            _gridCellToWorldMethod = null;
            _tidalBlessingDiagLogged = false;
            ResetStatCache();
        }

        private static void ResetStatCache()
        {
            _cacheValid = false;
            _cachedHPRatio = 1f;
            _cachedSeason = null;
            _cachedIsDaytime = true;
            _cachedIsInMine = false;
            _cachedQuickLearnerBonus = 0f;
        }

        protected override void Log(string message)
        {
            Plugin.Log?.LogInfo(message);
        }

        protected override void LogWarning(string message)
        {
            Plugin.Log?.LogWarning(message);
        }

        private void InitializeApiCache()
        {
            try
            {
                _cropType = ReflectionHelper.FindWishType("Crop");
                _dayCycleType = ReflectionHelper.FindWishType("DayCycle");

                if (_cropType != null)
                    Plugin.Log.LogInfo("[BirthrightRunner] Found Crop type for Tidal Blessing");
                if (_dayCycleType != null)
                    Plugin.Log.LogInfo("[BirthrightRunner] Found DayCycle type for synergies");

                // Cache TileManager type and methods for tile-based watering
                CacheTileManager();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[BirthrightRunner] API cache init failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Caches TileManager type, instance, and watering methods.
        /// TileManager is a SingletonBehaviour — its instance may not be ready at init time.
        /// </summary>
        private void CacheTileManager()
        {
            try
            {
                _tileManagerType = ReflectionHelper.FindWishType("TileManager");
                if (_tileManagerType == null)
                {
                    Plugin.Log.LogWarning("[BirthrightRunner] TileManager type not found — Tidal Blessing will use fallback");
                    return;
                }

                // Cache method references (these are stable across scene loads)
                _isWateredMethod = _tileManagerType.GetMethod("IsWatered",
                    new[] { typeof(Vector2Int) });
                _waterTileMethod = _tileManagerType.GetMethod("Water",
                    new[] { typeof(Vector2Int), typeof(short) });
                _isHoedOrWateredMethod = _tileManagerType.GetMethod("IsHoedOrWatered",
                    new[] { typeof(Vector2Int) });

                // Cache farmingData field for direct dictionary access
                _farmingDataField = _tileManagerType.GetField("farmingData",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                // Cache farmingTileMap field for WorldToCell coordinate conversion
                _farmingTileMapField = _tileManagerType.GetField("farmingTileMap",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                Plugin.Log.LogInfo($"[BirthrightRunner] TileManager cached — " +
                    $"IsWatered:{_isWateredMethod != null}, Water:{_waterTileMethod != null}, " +
                    $"IsHoedOrWatered:{_isHoedOrWateredMethod != null}, " +
                    $"farmingData:{_farmingDataField != null}, farmingTileMap:{_farmingTileMapField != null}");

                // Try to get instance now (may need lazy retry later)
                TryGetTileManagerInstance();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[BirthrightRunner] TileManager cache failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to get the TileManager singleton instance.
        /// Called lazily — instance may not be available during init.
        /// </summary>
        private void TryGetTileManagerInstance()
        {
            // Check for stale Unity reference (destroyed object stored as C# object
            // bypasses Unity's == null override → MissingReferenceException on invoke)
            if (_tileManagerInstance != null)
            {
                if (_tileManagerInstance is UnityEngine.Object unityObj && unityObj == null)
                {
                    Plugin.Log?.LogInfo("[BirthrightRunner] TileManager instance was stale (destroyed Unity object) — clearing");
                    _tileManagerInstance = null;
                }
                else
                {
                    return; // Instance is valid
                }
            }

            if (_tileManagerType == null) return;

            try
            {
                _tileManagerInstance = ReflectionHelper.GetSingletonInstance(_tileManagerType);
                if (_tileManagerInstance != null)
                {
                    Plugin.Log?.LogInfo("[BirthrightRunner] TileManager instance acquired");

                    // Cache WorldToCell method from the farmingTileMap Tilemap
                    if (_worldToCellMethod == null && _farmingTileMapField != null)
                    {
                        try
                        {
                            var tilemap = _farmingTileMapField.GetValue(_tileManagerInstance);
                            if (tilemap != null)
                            {
                                _worldToCellMethod = tilemap.GetType().GetMethod("WorldToCell",
                                    new[] { typeof(Vector3) });
                                Plugin.Log?.LogInfo($"[BirthrightRunner] WorldToCell method: {(_worldToCellMethod != null ? "found" : "NOT found")}");
                            }
                            else
                            {
                                Plugin.Log?.LogInfo("[BirthrightRunner] farmingTileMap is null on TileManager instance");
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log?.LogWarning($"[BirthrightRunner] Failed to cache WorldToCell: {ex.Message}");
                        }
                    }

                    // Cache parent Grid component + CellToWorld for distance filtering
                    // Grid.CellToWorld converts farmingData keys → world positions correctly
                    // (unlike Tilemap.CellToWorld which has a transform offset)
                    if (_gridInstance == null && _farmingTileMapField != null)
                    {
                        try
                        {
                            var tilemap = _farmingTileMapField.GetValue(_tileManagerInstance);
                            if (tilemap != null)
                            {
                                var layoutGridProp = tilemap.GetType().GetProperty("layoutGrid");
                                _gridInstance = layoutGridProp?.GetValue(tilemap);
                                if (_gridInstance != null)
                                {
                                    _gridCellToWorldMethod = _gridInstance.GetType().GetMethod("CellToWorld",
                                        new[] { typeof(Vector3Int) });
                                    Plugin.Log?.LogInfo($"[BirthrightRunner] Grid cached — CellToWorld: {(_gridCellToWorldMethod != null ? "found" : "NOT found")}");
                                }
                                else
                                {
                                    Plugin.Log?.LogInfo("[BirthrightRunner] layoutGrid is null — distance filtering will be disabled");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log?.LogWarning($"[BirthrightRunner] Failed to cache Grid: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Plugin.Log?.LogInfo("[BirthrightRunner] TileManager instance NOT available (singleton returned null)");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[BirthrightRunner] TileManager instance lookup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates all cached values once per frame. StatPatches reads these cached values
        /// instead of doing expensive reflection on every GetStat call.
        /// </summary>
        private void UpdateStatCache(Race race)
        {
            try
            {
                // --- IsInMine (cheap - string ops only) ---
                _cachedIsInMine = IsInMine();

                // --- DayCycle lookups (hour, season) using cached _dayCycleType ---
                if (_dayCycleType != null)
                {
                    var dayCycleInstance = ReflectionHelper.GetSingletonInstance(_dayCycleType);
                    if (dayCycleInstance != null)
                    {
                        // Cache current hour → isDaytime
                        float hour = ReflectionHelper.TryGetValue<float>(dayCycleInstance, "Hour", -1f);
                        if (hour < 0)
                            hour = ReflectionHelper.TryGetValue<float>(dayCycleInstance, "CurrentHour", -1f);
                        if (hour < 0)
                            hour = ReflectionHelper.TryGetValue<float>(dayCycleInstance, "currentHour", -1f);
                        if (hour < 0)
                        {
                            int hourInt = ReflectionHelper.TryGetValue<int>(dayCycleInstance, "Hour", -1);
                            if (hourInt >= 0) hour = hourInt;
                        }

                        _cachedIsDaytime = hour < 0 ? true : (hour >= 6f && hour < 18f);

                        // Cache current season
                        var season = ReflectionHelper.GetInstanceValue(dayCycleInstance, "Season");
                        if (season == null)
                            season = ReflectionHelper.GetInstanceValue(dayCycleInstance, "season");
                        if (season == null)
                            season = ReflectionHelper.GetInstanceValue(dayCycleInstance, "CurrentSeason");

                        _cachedSeason = season?.ToString();
                    }
                }

                // --- HP ratio ---
                var player = Wish.Player.Instance;
                if (player != null)
                {
                    float maxHP = player.MaxHealth;
                    if (maxHP > 0)
                    {
                        float currentHP = ReflectionHelper.TryGetValue<float>(player, "health", maxHP);
                        _cachedHPRatio = currentHP / maxHP;
                    }
                    else
                    {
                        _cachedHPRatio = 1f;
                    }

                    // --- Quick Learner bonus (only for Humans) ---
                    if (race == Race.Human && AbilityConfig.EnableQuickLearner != null && AbilityConfig.EnableQuickLearner.Value)
                    {
                        _cachedQuickLearnerBonus = CalculateQuickLearnerBonus(player);
                    }
                    else
                    {
                        _cachedQuickLearnerBonus = 0f;
                    }
                }

                _cacheValid = true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[BirthrightRunner] Cache update error: {ex.Message}");
                // Keep cache valid with safe defaults rather than disabling bonuses
                _cacheValid = true;
            }
        }

        /// <summary>
        /// Calculates the Quick Learner XP bonus based on leveled skills.
        /// Called once per frame from UpdateStatCache, NOT from the GetStat hot path.
        /// </summary>
        private static float CalculateQuickLearnerBonus(Wish.Player player)
        {
            try
            {
                int threshold = AbilityConfig.QuickLearnerSkillThreshold.Value;
                int qualifyingSkills = 0;

                // Access skill levels via reflection (they are properties returning float)
                float farmingLevel = ReflectionHelper.TryGetValue<float>(player, "FarmingSkillLevel", 0f);
                float miningLevel = ReflectionHelper.TryGetValue<float>(player, "MiningSkillLevel", 0f);
                float fishingLevel = ReflectionHelper.TryGetValue<float>(player, "FishingSkillLevel", 0f);
                float explorationLevel = ReflectionHelper.TryGetValue<float>(player, "ExplorationSkillLevel", 0f);

                if (farmingLevel >= threshold) qualifyingSkills++;
                if (miningLevel >= threshold) qualifyingSkills++;
                if (fishingLevel >= threshold) qualifyingSkills++;
                if (explorationLevel >= threshold) qualifyingSkills++;

                // Try to access crafting/smithing profession level
                try
                {
                    var professions = ReflectionHelper.GetInstanceValue(player, "Professions");
                    if (professions != null)
                    {
                        var craftingType = ReflectionHelper.FindWishType("ProfessionType");
                        if (craftingType != null)
                        {
                            var craftingEnum = Enum.Parse(craftingType, "Crafting");
                            var dict = professions as System.Collections.IDictionary;
                            if (dict != null && dict.Contains(craftingEnum))
                            {
                                var profession = dict[craftingEnum];
                                int level = ReflectionHelper.TryGetValue<int>(profession, "level", 0);
                                if (level >= threshold) qualifyingSkills++;
                            }
                        }
                    }
                }
                catch { /* Crafting level check failed - skip */ }

                float bonus = qualifyingSkills * AbilityConfig.QuickLearnerBonusPerSkill.Value;
                return Mathf.Min(bonus, AbilityConfig.QuickLearnerMaxBonus.Value);
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// Tracks time spent outdoors for Amari Bird's Tailwind ability.
        /// Outdoor time resets when entering indoor/mine scenes.
        /// </summary>
        private void UpdateTailwind()
        {
            string sceneName = SceneHelpers.GetCurrentSceneName().ToLowerInvariant();

            // Consider indoor: scenes containing "house", "inn", "shop", "cave", "mine", "dungeon"
            bool isIndoor = sceneName.Contains("house") || sceneName.Contains("inn") ||
                            sceneName.Contains("shop") || sceneName.Contains("cave") ||
                            sceneName.Contains("mine") || sceneName.Contains("dungeon") ||
                            sceneName.Contains("interior");

            if (isIndoor)
            {
                _outdoorTime = 0f;
                ActiveAbilityManager.SetBonusValue(ActiveAbilityManager.TailwindOutdoor, 0f);
            }
            else
            {
                _outdoorTime += Time.deltaTime;
                float minutesOutdoor = _outdoorTime / 60f;
                float bonus = Mathf.Min(
                    minutesOutdoor * AbilityConfig.TailwindBonusPerMinute.Value,
                    AbilityConfig.TailwindMaxBonus.Value
                );
                ActiveAbilityManager.SetBonusValue(ActiveAbilityManager.TailwindOutdoor, bonus);
            }
        }

        /// <summary>
        /// Water Elemental's Tidal Blessing: auto-water nearby hoed/planted tiles at HP cost.
        ///
        /// Primary approach: Uses TileManager.Water(Vector2Int, Int16) to water tiles directly.
        /// This is the game's native watering API and handles both the tile state AND any crop on it.
        ///
        /// Fallback: If TileManager is unavailable, falls back to FindObjectsOfType(Crop) + Crop.Water().
        /// </summary>
        private void UpdateTidalBlessing()
        {
            // Runtime toggle check (independent from config — allows in-game on/off)
            if (!ActiveAbilityManager.IsRuntimeEnabled(ActiveAbilityManager.TidalBlessing))
            {
                _tidalBlessingDiagLogged = false; // Reset so diagnostic fires again when re-enabled
                return;
            }

            // Cooldown check
            if (Time.time - _lastTidalBlessingCheck < AbilityConfig.TidalBlessingCooldown.Value)
                return;

            _lastTidalBlessingCheck = Time.time;

            if (ActiveAbilityManager.IsOnCooldown(ActiveAbilityManager.TidalBlessing))
                return;

            try
            {
                var player = Wish.Player.Instance;
                if (player == null)
                    return;

                // Safety threshold check
                float maxHP = player.MaxHealth;
                float currentHP = ReflectionHelper.TryGetValue<float>(player, "health", -1f);
                if (currentHP < 0)
                    currentHP = ReflectionHelper.TryGetValue<float>(player, "Health", maxHP);

                float hpPercent = (currentHP / maxHP) * 100f;

                // One-time diagnostic dump (fires once per toggle ON)
                if (!_tidalBlessingDiagLogged)
                {
                    _tidalBlessingDiagLogged = true;
                    Plugin.Log?.LogInfo($"[TidalBlessing] === DIAGNOSTIC ===");
                    Plugin.Log?.LogInfo($"[TidalBlessing]   HP: {currentHP:F0}/{maxHP:F0} ({hpPercent:F0}%), threshold: {AbilityConfig.TidalBlessingHPThreshold.Value}%");
                    Plugin.Log?.LogInfo($"[TidalBlessing]   TileManager: type={(_tileManagerType != null ? "ok" : "NULL")}, instance={(_tileManagerInstance != null ? "ok" : "NULL")}");
                    Plugin.Log?.LogInfo($"[TidalBlessing]   Methods: Water={(_waterTileMethod != null ? "ok" : "NULL")}, farmingData field={(_farmingDataField != null ? "ok" : "NULL")}");
                    Plugin.Log?.LogInfo($"[TidalBlessing]   Cooldown: {AbilityConfig.TidalBlessingCooldown.Value}s, HP cost: {AbilityConfig.TidalBlessingHPCostPercent.Value}%");

                    // Log farmingData counts
                    if (_farmingDataField != null && _tileManagerInstance != null)
                    {
                        try
                        {
                            var idict = _farmingDataField.GetValue(_tileManagerInstance) as IDictionary;
                            if (idict != null)
                            {
                                int hoedCount = 0;
                                int wateredCount = 0;
                                foreach (DictionaryEntry entry in idict)
                                {
                                    string s = entry.Value.ToString();
                                    if (s == "Hoed" || s == "2") hoedCount++;
                                    else if (s == "Watered" || s == "3") wateredCount++;
                                }
                                Plugin.Log?.LogInfo($"[TidalBlessing]   farmingData: {idict.Count} total, {hoedCount} hoed, {wateredCount} watered");
                            }
                        }
                        catch (Exception ex) { Plugin.Log?.LogDebug($"[BirthrightRunner] TidalBlessing diagnostic: {ex.Message}"); }
                    }

                    Plugin.Log?.LogInfo($"[TidalBlessing]   Scene: {SceneHelpers.GetCurrentSceneName()}");
                    Plugin.Log?.LogInfo($"[TidalBlessing]   Player world pos: ({player.transform.position.x:F1}, {player.transform.position.y:F1})");
                    Plugin.Log?.LogInfo($"[TidalBlessing]   Grid distance filter: {(_gridCellToWorldMethod != null && _gridInstance != null ? "enabled (radius=1)" : "disabled (no Grid)")}");
                    Plugin.Log?.LogInfo($"[TidalBlessing] === END DIAGNOSTIC ===");
                }

                if (hpPercent <= AbilityConfig.TidalBlessingHPThreshold.Value)
                    return;

                // Try TileManager approach first (primary), fall back to Crop approach
                if (_tileManagerType != null && _isWateredMethod != null && _waterTileMethod != null)
                {
                    UpdateTidalBlessingViaTileManager(player, maxHP, currentHP);
                }
                else
                {
                    Plugin.Log?.LogInfo("[TidalBlessing] Using fallback Crop approach (TileManager methods not available)");
                    UpdateTidalBlessingViaCropObjects(player, maxHP, currentHP);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[TidalBlessing] Error: {ex.Message}");
                Plugin.Log?.LogWarning($"[TidalBlessing] Stack: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Primary Tidal Blessing approach: iterate farmingData dictionary directly.
        /// Bypasses coordinate conversion entirely — farmingData keys are already in the
        /// exact format that TileManager.Water(Vector2Int, Int16) expects.
        /// Only waters hoed tiles within TidalBlessingRadius of the player (using Grid.CellToWorld
        /// to convert farmingData keys to world positions for distance comparison).
        /// </summary>
        private void UpdateTidalBlessingViaTileManager(Wish.Player player, float maxHP, float currentHP)
        {
            // Check for stale Unity reference before use
            if (_tileManagerInstance is UnityEngine.Object unityObj && unityObj == null)
            {
                Plugin.Log?.LogInfo("[TidalBlessing] TileManager instance was stale — re-acquiring");
                _tileManagerInstance = null;
            }

            // Lazy-acquire TileManager instance (may not be ready during init)
            if (_tileManagerInstance == null)
                TryGetTileManagerInstance();
            if (_tileManagerInstance == null)
            {
                Plugin.Log?.LogInfo("[TidalBlessing] TileManager instance not available — skipping this cycle");
                return;
            }

            // Read farmingData dictionary directly (bypasses coordinate conversion)
            if (_farmingDataField == null) return;
            var farmingDict = _farmingDataField.GetValue(_tileManagerInstance) as IDictionary;
            if (farmingDict == null || farmingDict.Count == 0) return;

            float hpCostPerPlot = maxHP * (AbilityConfig.TidalBlessingHPCostPercent.Value / 100f);
            short sceneIndex = (short)SceneManager.GetActiveScene().buildIndex;

            int plotsWatered = 0;
            int hoedFound = 0;

            // Collect hoed tile positions near the player (can't modify dict during iteration
            // because Water() may change the entry from Hoed → Watered)
            var hoedTiles = new List<Vector2Int>();
            var playerPos2D = new Vector2(player.transform.position.x, player.transform.position.y);
            int radius = 1;
            bool hasGridConversion = _gridCellToWorldMethod != null && _gridInstance != null;

            foreach (DictionaryEntry entry in farmingDict)
            {
                string state = entry.Value.ToString();
                if (state == "Hoed" || state == "2")
                {
                    var tilePos = (Vector2Int)entry.Key;

                    // Distance filter: convert farmingData key → world pos via Grid.CellToWorld
                    if (hasGridConversion)
                    {
                        try
                        {
                            var worldPos = (Vector3)_gridCellToWorldMethod.Invoke(_gridInstance,
                                new object[] { new Vector3Int(tilePos.x, tilePos.y, 0) });
                            float dist = Vector2.Distance(playerPos2D, new Vector2(worldPos.x, worldPos.y));
                            if (dist <= radius)
                                hoedTiles.Add(tilePos);
                        }
                        catch
                        {
                            // Grid conversion failed for this tile — include it as fallback
                            hoedTiles.Add(tilePos);
                        }
                    }
                    else
                    {
                        // No Grid available — water all hoed tiles (fallback behavior)
                        hoedTiles.Add(tilePos);
                    }
                }
            }
            hoedFound = hoedTiles.Count;

            // Water each hoed tile using the dictionary key directly
            // (keys are already in the coordinate system Water() expects)
            foreach (var tilePos in hoedTiles)
            {
                // HP safety check before each watering
                float newHPPercent = ((currentHP - hpCostPerPlot * (plotsWatered + 1)) / maxHP) * 100f;
                if (newHPPercent <= AbilityConfig.TidalBlessingHPThreshold.Value)
                    break;

                try
                {
                    var waterResult = _waterTileMethod.Invoke(_tileManagerInstance, new object[] { tilePos, sceneIndex });
                    if (waterResult is bool success && success)
                        plotsWatered++;
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"[TidalBlessing] Water({tilePos}) error: {ex.Message}");
                }
            }

            if (plotsWatered > 0)
            {
                // Deduct HP directly (avoid ReceiveDamage which triggers CombatPatches)
                float totalHPCost = hpCostPerPlot * plotsWatered;
                float newHP = currentHP - totalHPCost;
                if (newHP < 1f) newHP = 1f;
                ReflectionHelper.SetInstanceValue(player, "Health", newHP);

                Plugin.Log?.LogInfo($"[TidalBlessing] Watered {plotsWatered}/{hoedFound} hoed tiles via farmingData scan");
            }
            else if (hoedFound > 0)
            {
                Plugin.Log?.LogInfo($"[TidalBlessing] Found {hoedFound} hoed tiles but Water() returned false for all");
            }
            else
            {
                Plugin.Log?.LogInfo("[TidalBlessing] No hoed tiles in farmingData");
            }
        }

        /// <summary>
        /// Fallback Tidal Blessing approach: find Crop MonoBehaviours via FindObjectsOfType.
        /// Used only if TileManager is unavailable.
        /// </summary>
        private void UpdateTidalBlessingViaCropObjects(Wish.Player player, float maxHP, float currentHP)
        {
            if (_cropType == null)
            {
                Plugin.Log?.LogWarning("[TidalBlessing] No Crop type and no TileManager — cannot water");
                return;
            }

            var playerPos = player.transform.position;
            int radius = 1;
            int plotsWatered = 0;

            var crops = UnityEngine.Object.FindObjectsOfType(_cropType);
            Plugin.Log?.LogInfo($"[TidalBlessing] Fallback mode: FindObjectsOfType found {crops?.Length ?? 0} Crop objects");

            if (crops == null || crops.Length == 0)
                return;

            float hpCostPerPlot = maxHP * (AbilityConfig.TidalBlessingHPCostPercent.Value / 100f);

            foreach (var cropObj in crops)
            {
                var cropComponent = cropObj as Component;
                if (cropComponent == null)
                    continue;

                // Check distance
                float distance = Vector2.Distance(
                    new Vector2(playerPos.x, playerPos.y),
                    new Vector2(cropComponent.transform.position.x, cropComponent.transform.position.y)
                );

                if (distance > radius)
                    continue;

                // Check if already watered via crop.data.watered (CropSaveData property)
                object cropData = ReflectionHelper.GetInstanceValue(cropObj, "data");
                if (cropData != null)
                {
                    bool isWatered = ReflectionHelper.TryGetValue<bool>(cropData, "watered", false);
                    if (isWatered)
                        continue;
                }

                // Check HP safety before each watering
                float newHPPercent = ((currentHP - hpCostPerPlot * (plotsWatered + 1)) / maxHP) * 100f;
                if (newHPPercent <= AbilityConfig.TidalBlessingHPThreshold.Value)
                    break;

                // Call Crop.Water() — public void method
                var cropObjType = cropObj.GetType();
                var waterMethod = cropObjType.GetMethod("Water", BindingFlags.Public | BindingFlags.Instance);
                if (waterMethod != null)
                {
                    waterMethod.Invoke(cropObj, null);
                    plotsWatered++;
                }
                else
                {
                    // Last resort: set watered on CropSaveData directly
                    if (cropData != null)
                    {
                        ReflectionHelper.SetInstanceValue(cropData, "watered", true);
                        plotsWatered++;
                    }
                }
            }

            if (plotsWatered > 0)
            {
                float totalHPCost = hpCostPerPlot * plotsWatered;
                float newHP = currentHP - totalHPCost;
                if (newHP < 1f) newHP = 1f;
                ReflectionHelper.SetInstanceValue(player, "Health", newHP);

                Plugin.Log?.LogInfo($"[TidalBlessing] Watered {plotsWatered} crops via fallback Crop.Water()");
            }
        }

        // ===================== INFERNAL FORGE (PERIODIC INVENTORY SCAN) =====================

        /// <summary>
        /// Fire Elemental's Infernal Forge: periodically scans inventory for ore stacks,
        /// smelts them into bars, deducts mana. Replaces the old per-pickup Harmony prefix
        /// which failed because ore is picked up 1 unit at a time (1/3 = 0 bars).
        /// </summary>
        private void UpdateInfernalForge()
        {
            // Runtime toggle check
            if (!ActiveAbilityManager.IsRuntimeEnabled(ActiveAbilityManager.InfernalForge))
                return;

            // Cooldown: only scan every N seconds (default 2s)
            float scanInterval = AbilityConfig.InfernalForgeScanInterval?.Value ?? 2f;
            if (Time.time - _lastInfernalForgeCheck < scanInterval)
                return;
            _lastInfernalForgeCheck = Time.time;

            try
            {
                var player = Wish.Player.Instance;
                if (player == null)
                    return;

                // Get player inventory via reflection
                object inventory = ReflectionHelper.GetInstanceValue(player, "Inventory");
                if (inventory == null)
                    inventory = ReflectionHelper.GetInstanceValue(player, "inventory");
                if (inventory == null)
                {
                    Plugin.Log?.LogInfo("[InfernalForge] Could not find player inventory");
                    return;
                }

                // Cache inventory methods on first use
                if (!_inventoryMethodsCached)
                {
                    CacheInventoryMethods(inventory);
                    _inventoryMethodsCached = true;
                }

                // Check mana threshold before scanning
                float maxMana = player.MaxMana;
                float currentMana = ReflectionHelper.TryGetValue<float>(player, "Mana", 0f);
                float manaPercent = (currentMana / maxMana) * 100f;
                if (manaPercent <= AbilityConfig.InfernalForgeManaThreshold.Value)
                    return;

                int orePerBar = AbilityConfig.InfernalForgeOrePerBar.Value;
                if (orePerBar < 1) orePerBar = 3;

                // Get the AddItem method via AbilityPatches (already has caching logic)
                var addMethod = AbilityPatches.GetAddItemIntMethod(inventory);

                int totalBarsSmelted = 0;
                float totalManaCost = 0f;

                // Scan all ore types in OreToBarMap
                foreach (var kvp in AbilityPatches.OreToBarMap)
                {
                    int oreId = kvp.Key;
                    int barId = kvp.Value;

                    // Get current ore count in inventory
                    int oreCount = GetInventoryAmount(inventory, oreId);
                    if (oreCount < orePerBar)
                        continue;

                    // Tiered mana cost: lookup per-ore cost (defaults to 3% if unknown ore)
                    float costPercent = AbilityPatches.OreManaCostMap.TryGetValue(oreId, out float pct) ? pct : 3f;
                    float costPerBar = maxMana * (costPercent / 100f);

                    // Calculate how many bars we can produce
                    int barsProduced = oreCount / orePerBar;
                    int oreUsed = barsProduced * orePerBar;

                    // Check if we can afford the mana cost
                    float manaCostForBatch = costPerBar * barsProduced;
                    float newManaPercent = ((currentMana - totalManaCost - manaCostForBatch) / maxMana) * 100f;

                    if (newManaPercent < AbilityConfig.InfernalForgeManaThreshold.Value)
                    {
                        // Try fewer bars
                        barsProduced = 0;
                        for (int i = oreCount / orePerBar; i >= 1; i--)
                        {
                            float cost = costPerBar * i;
                            float testPercent = ((currentMana - totalManaCost - cost) / maxMana) * 100f;
                            if (testPercent >= AbilityConfig.InfernalForgeManaThreshold.Value)
                            {
                                barsProduced = i;
                                break;
                            }
                        }
                        if (barsProduced <= 0)
                            continue;

                        oreUsed = barsProduced * orePerBar;
                        manaCostForBatch = costPerBar * barsProduced;
                    }

                    // Remove ore from inventory
                    bool removeSuccess = RemoveInventoryItem(inventory, oreId, oreUsed);
                    if (!removeSuccess)
                    {
                        Plugin.Log?.LogWarning($"[InfernalForge] Failed to remove {oreUsed}x ore {oreId}");
                        continue;
                    }

                    // Add bars to inventory
                    bool addSuccess = false;
                    if (addMethod != null)
                    {
                        addSuccess = AbilityPatches.InvokeAddItem(addMethod, inventory, barId, barsProduced, true);
                    }

                    if (!addSuccess)
                    {
                        // Bar addition failed — try to return ore
                        Plugin.Log?.LogError($"[InfernalForge] Bar addition failed for {barsProduced}x bar {barId} — returning ore");
                        AddInventoryItem(inventory, oreId, oreUsed);
                        continue;
                    }

                    totalBarsSmelted += barsProduced;
                    totalManaCost += manaCostForBatch;

                    Plugin.Log?.LogInfo($"[InfernalForge] Smelted {oreUsed}x ore {oreId} → {barsProduced}x bar {barId} (cost: {costPercent}% per bar)");
                }

                // Apply mana cost and send notification
                if (totalBarsSmelted > 0)
                {
                    float newMana = currentMana - totalManaCost;
                    if (newMana < 0f) newMana = 0f;
                    ReflectionHelper.SetInstanceValue(player, "Mana", newMana);

                    AbilityPatches.SendGameNotification(
                        $"Infernal Forge: Smelted {totalBarsSmelted} bar(s) (-{totalManaCost:F1} Mana)");

                    Plugin.Log?.LogInfo($"[InfernalForge] Total: {totalBarsSmelted} bars, {totalManaCost:F1} mana used");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[InfernalForge] Error: {ex.Message}");
                Plugin.Log?.LogWarning($"[InfernalForge] Stack: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Angel-only: Font of Light — periodic mana restore at the cost of gold.
        /// Only runs when player race is Angel; has no effect for any other race.
        /// </summary>
        private void UpdateFontOfLight()
        {
            if (!ActiveAbilityManager.IsRuntimeEnabled(ActiveAbilityManager.FontOfLight))
                return;

            float interval = AbilityConfig.FontOfLightInterval?.Value ?? 45f;
            if (Time.time - _lastFontOfLightCheck < interval)
                return;
            _lastFontOfLightCheck = Time.time;

            try
            {
                var player = Wish.Player.Instance;
                if (player == null) return;

                float maxMana = player.MaxMana;
                float currentMana = ReflectionHelper.TryGetValue<float>(player, "Mana", 0f);
                float manaPercent = maxMana > 0 ? (currentMana / maxMana) * 100f : 0f;
                if (manaPercent >= (AbilityConfig.FontOfLightManaThreshold?.Value ?? 80f))
                    return;

                int goldCost = AbilityConfig.FontOfLightGoldCost?.Value ?? 10;
                if (goldCost > 0)
                {
                    var gameSaveType = AccessTools.TypeByName("Wish.GameSave");
                    if (gameSaveType != null)
                    {
                        var coinsObj = ReflectionHelper.GetStaticValue(gameSaveType, "Coins");
                        int coins = coinsObj is int c ? c : 0;
                        if (coins < goldCost)
                            return;
                    }
                }

                float restorePercent = AbilityConfig.FontOfLightManaPercent?.Value ?? 5f;
                float restoreAmount = maxMana * (restorePercent / 100f);
                if (restoreAmount <= 0f) return;

                ReflectionHelper.InvokeMethod(player, "AddMana", restoreAmount, 1f);

                if (goldCost > 0)
                {
                    var addMoneyMethod = AccessTools.Method(player.GetType(), "AddMoney",
                        new[] { typeof(int), typeof(bool), typeof(bool), typeof(bool) });
                    if (addMoneyMethod != null)
                        addMoneyMethod.Invoke(player, new object[] { -goldCost, false, false, false });
                }

                AbilityPatches.SendGameNotification(
                    $"Font of Light: +{restorePercent:F0}% mana" + (goldCost > 0 ? $" (-{goldCost} gold)" : ""));
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[FontOfLight] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Caches inventory GetAmount and RemoveItem methods for repeated use.
        /// </summary>
        private void CacheInventoryMethods(object inventory)
        {
            try
            {
                var invType = inventory.GetType();

                // GetAmount(int itemId) → int
                _cachedGetAmountMethod = invType.GetMethod("GetAmount",
                    new[] { typeof(int) });
                if (_cachedGetAmountMethod == null)
                {
                    // Try alternate names
                    _cachedGetAmountMethod = invType.GetMethod("GetItemAmount",
                        new[] { typeof(int) });
                }

                // RemoveAll(Int32 id) — removes all of an item by ID
                // (RemoveItem requires a slot index we don't have, so we use
                //  RemoveAll then AddItem back the leftovers)
                _cachedRemoveItemMethod = invType.GetMethod("RemoveAll",
                    new[] { typeof(int) });

                Plugin.Log?.LogInfo($"[InfernalForge] Inventory methods cached - " +
                    $"GetAmount:{_cachedGetAmountMethod?.Name ?? "null"}, " +
                    $"RemoveItem:{_cachedRemoveItemMethod?.Name ?? "null"}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[InfernalForge] Failed to cache inventory methods: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the amount of a specific item in the inventory.
        /// </summary>
        private int GetInventoryAmount(object inventory, int itemId)
        {
            try
            {
                if (_cachedGetAmountMethod != null)
                {
                    var result = _cachedGetAmountMethod.Invoke(inventory, new object[] { itemId });
                    if (result is int count)
                        return count;
                }

                // Fallback: try reflection helper
                var amount = ReflectionHelper.InvokeMethod(inventory, "GetAmount", itemId);
                if (amount is int amt)
                    return amt;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[InfernalForge] GetAmount failed for item {itemId}: {ex.Message}");
            }
            return 0;
        }

        /// <summary>
        /// Removes items from the inventory.
        /// </summary>
        /// <summary>
        /// Removes a specific amount of an item from inventory.
        /// Uses RemoveAll(id) then adds back leftovers, since RemoveItem requires a slot index.
        /// </summary>
        private bool RemoveInventoryItem(object inventory, int itemId, int amount)
        {
            try
            {
                if (_cachedRemoveItemMethod == null)
                {
                    Plugin.Log?.LogWarning($"[InfernalForge] No cached RemoveAll method — cannot remove {amount}x item {itemId}");
                    return false;
                }

                // Get current count before removing
                int currentCount = GetInventoryAmount(inventory, itemId);
                if (currentCount < amount)
                {
                    Plugin.Log?.LogWarning($"[InfernalForge] Not enough items: have {currentCount}, need {amount} of item {itemId}");
                    return false;
                }

                int leftover = currentCount - amount;

                // RemoveAll(int id) — removes everything of this item
                _cachedRemoveItemMethod.Invoke(inventory, new object[] { itemId });

                // Add back leftovers if any
                if (leftover > 0)
                {
                    AddInventoryItem(inventory, itemId, leftover);
                }

                return true;
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                Plugin.Log?.LogWarning($"[InfernalForge] RemoveItem failed for {amount}x item {itemId}: {inner.GetType().Name}: {inner.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adds items to the inventory (used for returning ore on failure).
        /// </summary>
        private void AddInventoryItem(object inventory, int itemId, int amount)
        {
            try
            {
                var addMethod = AbilityPatches.GetAddItemIntMethod(inventory);
                if (addMethod != null)
                {
                    AbilityPatches.InvokeAddItem(addMethod, inventory, itemId, amount, false);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[InfernalForge] Failed to return {amount}x item {itemId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current in-game hour (0-24) from DayCycle for time-of-day synergies.
        /// Returns -1 if the hour cannot be determined.
        /// </summary>
        public static float GetCurrentHour()
        {
            try
            {
                var dayCycleType = ReflectionHelper.FindWishType("DayCycle");
                if (dayCycleType == null)
                    return -1f;

                var instance = ReflectionHelper.GetSingletonInstance(dayCycleType);
                if (instance != null)
                {
                    // Try common property names
                    float hour = ReflectionHelper.TryGetValue<float>(instance, "Hour", -1f);
                    if (hour >= 0) return hour;

                    hour = ReflectionHelper.TryGetValue<float>(instance, "CurrentHour", -1f);
                    if (hour >= 0) return hour;

                    hour = ReflectionHelper.TryGetValue<float>(instance, "currentHour", -1f);
                    if (hour >= 0) return hour;

                    // Try int version
                    int hourInt = ReflectionHelper.TryGetValue<int>(instance, "Hour", -1);
                    if (hourInt >= 0) return hourInt;
                }

                // Try static access
                float staticHour = ReflectionHelper.TryGetValue<float>(null, "Hour", -1f);
                return staticHour;
            }
            catch
            {
                return -1f;
            }
        }

        /// <summary>
        /// Gets the current season from DayCycle.
        /// Returns the season as a string (Spring, Summer, Fall, Winter) or null.
        /// </summary>
        public static string GetCurrentSeason()
        {
            try
            {
                var dayCycleType = ReflectionHelper.FindWishType("DayCycle");
                if (dayCycleType == null)
                    return null;

                var instance = ReflectionHelper.GetSingletonInstance(dayCycleType);
                if (instance != null)
                {
                    var season = ReflectionHelper.GetInstanceValue(instance, "Season");
                    if (season != null)
                        return season.ToString();

                    season = ReflectionHelper.GetInstanceValue(instance, "season");
                    if (season != null)
                        return season.ToString();

                    season = ReflectionHelper.GetInstanceValue(instance, "CurrentSeason");
                    if (season != null)
                        return season.ToString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if the current scene is a mine/underground area.
        /// </summary>
        public static bool IsInMine()
        {
            string scene = SceneHelpers.GetCurrentSceneName().ToLowerInvariant();
            return scene.Contains("mine") || scene.Contains("dungeon") ||
                   scene.Contains("cave") || scene.Contains("underground") ||
                   scene.Contains("nelvari") || scene.Contains("withergate");
        }

        /// <summary>
        /// Checks if it is currently daytime (6am-6pm).
        /// </summary>
        public static bool IsDaytime()
        {
            float hour = GetCurrentHour();
            if (hour < 0) return true; // Default to daytime if unknown
            return hour >= 6f && hour < 18f;
        }

        /// <summary>
        /// Gets the player's current HP ratio (0-1).
        /// </summary>
        public static float GetPlayerHPRatio()
        {
            try
            {
                var player = Wish.Player.Instance;
                if (player == null)
                    return 1f;

                float maxHP = player.MaxHealth;
                if (maxHP <= 0) return 1f;

                float currentHP = ReflectionHelper.TryGetValue<float>(player, "health", maxHP);
                return currentHP / maxHP;
            }
            catch
            {
                return 1f;
            }
        }

        // ===================== ABILITY TOGGLE HOTKEY =====================

        /// <summary>
        /// Checks for the ability toggle hotkey press and toggles the active ability
        /// for the player's current race. Sends an in-game notification on toggle.
        /// </summary>
        private void CheckAbilityToggleHotkey()
        {
            try
            {
                if (!Input.GetKeyDown(Plugin.StaticAbilityToggleKey))
                    return;

                var manager = Plugin.GetRacialBonusManager();
                if (manager == null) return;

                var race = manager.GetPlayerRace();
                if (!race.HasValue) return;

                string abilityKey = GetActiveAbilityForRace(race.Value);
                if (abilityKey == null)
                {
                    Plugin.Log?.LogDebug($"[BirthrightRunner] No toggleable active ability for race {race.Value}");
                    return;
                }

                bool newState = ActiveAbilityManager.ToggleRuntime(abilityKey);
                string displayName = GetAbilityDisplayName(abilityKey);

                // Send in-game notification via cached 5-arg method
                AbilityPatches.SendGameNotification($"{displayName}: {(newState ? "ON" : "OFF")}");

                Plugin.Log?.LogInfo($"[BirthrightRunner] Toggled {displayName}: {(newState ? "ON" : "OFF")}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[BirthrightRunner] Toggle hotkey error: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps a mod Race to its corresponding toggleable active ability key.
        /// Returns null if the race has no toggleable active ability.
        /// </summary>
        private static string GetActiveAbilityForRace(Race race)
        {
            switch (race)
            {
                case Race.FireElemental:
                    return ActiveAbilityManager.InfernalForge;
                case Race.WaterElemental:
                    return ActiveAbilityManager.TidalBlessing;
                case Race.Angel:
                    return ActiveAbilityManager.FontOfLight;
                case Race.Demon:
                    return ActiveAbilityManager.SoulHarvest;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns a human-readable display name for an ability key.
        /// </summary>
        private static string GetAbilityDisplayName(string abilityKey)
        {
            switch (abilityKey)
            {
                case ActiveAbilityManager.InfernalForge:
                    return "Infernal Forge";
                case ActiveAbilityManager.TidalBlessing:
                    return "Tidal Blessing";
                case ActiveAbilityManager.FontOfLight:
                    return "Font of Light";
                case ActiveAbilityManager.SoulHarvest:
                    return "Soul Harvest";
                default:
                    return abilityKey;
            }
        }
    }
}
