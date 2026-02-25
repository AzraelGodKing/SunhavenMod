using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SunHavenMuseumUtilityTracker.Data;

namespace SunHavenMuseumUtilityTracker.Patches
{
    /// <summary>
    /// Debug utilities for museum progress inspection.
    /// Note: Automatic tracking is not possible because Sun Haven only tracks
    /// bundle completion, not individual item donations. Use manual tracking instead.
    /// </summary>
    public static class MuseumPatches
    {
        // Maps progress key names to our item IDs (populated from game data)
        private static Dictionary<string, string> _progressKeyToItemId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Maps game item IDs to progress keys (reverse lookup)
        private static Dictionary<int, string> _gameIdToProgressKey = new Dictionary<int, string>();

        private static bool _gameDataDiscovered = false;

        // Log all progress key calls for debugging
        private static bool _debugLogging = true;

        // Cached reflection data for performance
        private static object _cachedGameSaveInstance;
        private static MethodInfo _cachedGetProgressBoolMethod;
        private static MethodInfo _cachedGetProgressIntMethod;
        private static bool _reflectionInitialized = false;
        private static bool _gameProgressAvailable = false;

        /// <summary>
        /// Discover the actual progress key patterns used by the game.
        /// Accesses MuseumCurator's static lists at runtime.
        /// Called from debug mode for inspection purposes.
        /// </summary>
        public static void DiscoverProgressKeyPatterns()
        {
            if (_gameDataDiscovered) return;
            _gameDataDiscovered = true;

            Plugin.Log?.LogInfo("[DISCOVER] ======= Starting discovery of game progress key patterns =======");

            try
            {
                // Find MuseumCurator type
                Type curatorType = null;

                Plugin.Log?.LogInfo("[DISCOVER] Searching for MuseumCurator type in all assemblies...");
                int assemblyCount = 0;

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    assemblyCount++;
                    try
                    {
                        curatorType = assembly.GetType("MuseumCurator");
                        if (curatorType == null)
                            curatorType = assembly.GetType("Wish.MuseumCurator");

                        if (curatorType != null)
                        {
                            Plugin.Log?.LogInfo($"[DISCOVER] FOUND MuseumCurator in assembly: {assembly.GetName().Name}");
                            Plugin.Log?.LogInfo($"[DISCOVER] Full type name: {curatorType.FullName}");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogDebug($"[MuseumPatches] Skipping assembly for MuseumCurator search: {ex.Message}");
                    }
                }

                Plugin.Log?.LogInfo($"[DISCOVER] Searched {assemblyCount} assemblies");

                if (curatorType == null)
                {
                    Plugin.Log?.LogWarning("[DISCOVER] Could not find MuseumCurator - trying AccessTools...");
                    curatorType = AccessTools.TypeByName("MuseumCurator") ?? AccessTools.TypeByName("Wish.MuseumCurator");
                }

                if (curatorType == null)
                {
                    Plugin.Log?.LogError("[DISCOVER] MuseumCurator type NOT FOUND in any assembly!");

                    // List all types containing "Museum" to help debug
                    Plugin.Log?.LogInfo("[DISCOVER] Searching for any types containing 'Museum'...");
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            foreach (var type in assembly.GetTypes())
                            {
                                if (type.Name.Contains("Museum"))
                                {
                                    Plugin.Log?.LogInfo($"[DISCOVER] Found type: {type.FullName}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log?.LogDebug($"[MuseumPatches] Skipping assembly for Museum types: {ex.Message}");
                        }
                    }
                    return;
                }

                // Log all static fields on MuseumCurator
                Plugin.Log?.LogInfo("[DISCOVER] Listing all static fields on MuseumCurator:");
                var allFields = curatorType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                foreach (var f in allFields)
                {
                    Plugin.Log?.LogInfo($"[DISCOVER]   Field: {f.Name} ({f.FieldType.Name})");
                }

                // Get all museum progress lists
                string[] listNames = { "culturalMuseumProgress", "aquaticMuseumProgress", "miningMuseumProgress" };
                int totalMapped = 0;

                foreach (var listName in listNames)
                {
                    Plugin.Log?.LogInfo($"[DISCOVER] Looking for field: {listName}");
                    var field = curatorType.GetField(listName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (field == null)
                    {
                        Plugin.Log?.LogWarning($"[DISCOVER] Field '{listName}' NOT FOUND on MuseumCurator");
                        continue;
                    }

                    Plugin.Log?.LogInfo($"[DISCOVER] Found field: {listName} (Type: {field.FieldType.FullName})");

                    var list = field.GetValue(null);
                    if (list == null)
                    {
                        Plugin.Log?.LogWarning($"[DISCOVER] Field '{listName}' is NULL");
                        continue;
                    }

                    Plugin.Log?.LogInfo($"[DISCOVER] Processing {listName} (contains data)...");

                    // The list is List<ValueTuple<string, int>>
                    if (list is IEnumerable enumerable)
                    {
                        int itemCount = 0;
                        foreach (var item in enumerable)
                        {
                            itemCount++;
                            try
                            {
                                // Get Item1 (progress key) and Item2 (game item ID)
                                var itemType = item.GetType();

                                var item1Field = itemType.GetField("Item1");
                                var item2Field = itemType.GetField("Item2");

                                if (item1Field == null || item2Field == null)
                                {
                                    continue;
                                }

                                string progressKey = item1Field.GetValue(item) as string;
                                int gameItemId = (int)item2Field.GetValue(item);

                                Plugin.Log?.LogInfo($"[DISCOVER]   Entry: progressKey='{progressKey}', gameItemId={gameItemId}");

                                if (!string.IsNullOrEmpty(progressKey))
                                {
                                    // Find matching item in our content
                                    var museumItem = MuseumContent.FindByGameItemId(gameItemId);
                                    if (museumItem != null)
                                    {
                                        _progressKeyToItemId[progressKey] = museumItem.Id;
                                        _gameIdToProgressKey[gameItemId] = progressKey;
                                        totalMapped++;
                                        Plugin.Log?.LogInfo($"[DISCOVER]   MAPPED: '{progressKey}' -> {museumItem.Id} ({museumItem.Name})");
                                    }
                                    else
                                    {
                                        Plugin.Log?.LogInfo($"[DISCOVER]   NOT IN OUR LIST: gameId {gameItemId}, key='{progressKey}'");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Plugin.Log?.LogError($"[DISCOVER]   Error processing item {itemCount}: {ex.Message}");
                            }
                        }
                        Plugin.Log?.LogInfo($"[DISCOVER] {listName} had {itemCount} entries");
                    }
                }

                Plugin.Log?.LogInfo($"[MuseumPatches] Discovered {totalMapped} progress key mappings");
                Plugin.Log?.LogInfo("[DISCOVER] NOTE: The game only tracks BUNDLE completion, not individual items.");
                Plugin.Log?.LogInfo("[DISCOVER] Use manual tracking in the UI to mark individual donations.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[MuseumPatches] Error discovering patterns: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Sync our tracker with the game's existing museum progress (bundle-level only).
        /// Called from debug mode for inspection purposes.
        /// NOTE: This only syncs COMPLETED bundles, not individual items.
        /// </summary>
        public static void SyncWithGameProgress()
        {
            try
            {
                var manager = Plugin.GetDonationManager();
                if (manager == null)
                {
                    Plugin.Log?.LogWarning("[SYNC] DonationManager is null");
                    return;
                }

                Plugin.Log?.LogInfo("[SYNC] ======= Starting sync with game progress =======");

                // Initialize and check if game progress is available
                InitializeReflectionCache();
                if (!_gameProgressAvailable || _cachedGameSaveInstance == null)
                {
                    Plugin.Log?.LogWarning("[SYNC] Game progress not available - cannot sync (character may not be loaded yet)");
                    return;
                }

                Plugin.Log?.LogInfo("[SYNC] NOTE: The game only stores BUNDLE completion, not individual items.");
                Plugin.Log?.LogInfo("[SYNC] This will mark ALL items in completed bundles as donated.");

                // Check all bundles for completion using cached methods
                int bundlesSynced = SyncCompletedBundles(_cachedGameSaveInstance, _cachedGetProgressBoolMethod, _cachedGetProgressIntMethod, manager);

                Plugin.Log?.LogInfo($"[SYNC] Synced {bundlesSynced} completed bundles");
                Plugin.Log?.LogInfo("[SYNC] ======= Sync complete =======");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[SYNC] Error syncing with game progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the donation count for a bundle from the game's progress system.
        /// Returns -1 if unable to read.
        /// </summary>
        public static int GetBundleDonationCount(string progressKey)
        {
            try
            {
                InitializeReflectionCache();
                if (!_gameProgressAvailable || _cachedGameSaveInstance == null || _cachedGetProgressIntMethod == null)
                    return -1;

                // Game stores count as "{BundleName}complete"
                string countKey = progressKey + "complete";
                return (int)_cachedGetProgressIntMethod.Invoke(_cachedGameSaveInstance, new object[] { countKey });
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Check if a bundle is marked as complete in the game's progress system.
        /// </summary>
        public static bool IsBundleCompleteInGame(string progressKey)
        {
            try
            {
                InitializeReflectionCache();
                if (!_gameProgressAvailable || _cachedGameSaveInstance == null || _cachedGetProgressBoolMethod == null)
                    return false;

                return (bool)_cachedGetProgressBoolMethod.Invoke(_cachedGameSaveInstance, new object[] { progressKey });
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks all known bundle progress keys and marks their items as donated if complete.
        /// </summary>
        private static int SyncCompletedBundles(object gameSaveInstance, MethodInfo getProgressBoolMethod, MethodInfo getProgressIntMethod, DonationManager manager)
        {
            int bundlesSynced = 0;

            // Get all bundle IDs and check their progress keys
            var bundleIds = MuseumContent.GetAllBundleIds();
            Plugin.Log?.LogInfo($"[SYNC] Checking {bundleIds.Count} bundles for completion...");

            foreach (var bundleId in bundleIds)
            {
                try
                {
                    // Get the progress key for this bundle
                    string progressKey = MuseumContent.GetProgressKeyForBundle(bundleId);
                    if (string.IsNullOrEmpty(progressKey)) continue;

                    // Get donation count from game (stored as "{BundleName}complete")
                    int gameDonationCount = 0;
                    if (getProgressIntMethod != null)
                    {
                        string countKey = progressKey + "complete";
                        gameDonationCount = (int)getProgressIntMethod.Invoke(gameSaveInstance, new object[] { countKey });
                    }

                    // Check if this bundle is complete in the game
                    bool isComplete = (bool)getProgressBoolMethod.Invoke(gameSaveInstance, new object[] { progressKey });

                    var items = MuseumContent.GetItemsInBundle(bundleId);
                    Plugin.Log?.LogDebug($"[SYNC] Bundle '{bundleId}' ({progressKey}): {gameDonationCount}/{items.Count} donated, complete={isComplete}");

                    if (isComplete)
                    {
                        Plugin.Log?.LogInfo($"[SYNC] Bundle '{bundleId}' is COMPLETE, marking all items...");

                        // Get all items in this bundle and mark them as donated
                        int markedCount = 0;

                        foreach (var item in items)
                        {
                            if (!manager.HasDonated(item.Id))
                            {
                                manager.MarkDonated(item.Id);
                                markedCount++;
                            }
                        }

                        if (markedCount > 0)
                        {
                            Plugin.Log?.LogInfo($"[SYNC] Marked {markedCount} items from bundle '{bundleId}'");
                            bundlesSynced++;
                        }
                    }
                    else if (gameDonationCount > 0)
                    {
                        // Bundle not complete, but has some donations
                        Plugin.Log?.LogInfo($"[SYNC] Bundle '{bundleId}' has {gameDonationCount}/{items.Count} donations (not complete)");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogDebug($"[SYNC] Error checking bundle '{bundleId}': {ex.Message}");
                }
            }

            return bundlesSynced;
        }

        /// <summary>
        /// Initialize reflection cache for game progress methods.
        /// Call this once, and it will cache all needed reflection data.
        /// </summary>
        private static void InitializeReflectionCache()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            try
            {
                // Try to get GameSave type
                var gameSaveType = AccessTools.TypeByName("Wish.GameSave") ?? AccessTools.TypeByName("GameSave");
                if (gameSaveType == null)
                {
                    Plugin.Log?.LogDebug("[MuseumPatches] GameSave type not found");
                    return;
                }

                // Try direct Instance property first (most common pattern)
                var instanceProp = gameSaveType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (instanceProp != null)
                {
                    _cachedGameSaveInstance = instanceProp.GetValue(null);
                }

                // If that didn't work, try SingletonBehaviour pattern
                if (_cachedGameSaveInstance == null)
                {
                    var singletonType = AccessTools.TypeByName("SingletonBehaviour`1");
                    if (singletonType != null)
                    {
                        var genericType = singletonType.MakeGenericType(gameSaveType);
                        instanceProp = genericType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                        if (instanceProp != null)
                        {
                            _cachedGameSaveInstance = instanceProp.GetValue(null);
                        }
                    }
                }

                if (_cachedGameSaveInstance == null)
                {
                    Plugin.Log?.LogDebug("[MuseumPatches] GameSave instance not available yet");
                    _reflectionInitialized = false; // Allow retry later
                    return;
                }

                // Cache the methods
                _cachedGetProgressBoolMethod = gameSaveType.GetMethod("GetProgressBoolWorld", new[] { typeof(string) });
                _cachedGetProgressIntMethod = gameSaveType.GetMethod("GetProgressIntWorld", new[] { typeof(string) });

                if (_cachedGetProgressBoolMethod != null && _cachedGetProgressIntMethod != null)
                {
                    _gameProgressAvailable = true;
                    Plugin.Log?.LogInfo("[MuseumPatches] Game progress reflection initialized successfully");
                }
                else
                {
                    Plugin.Log?.LogDebug("[MuseumPatches] Progress methods not found");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[MuseumPatches] Error initializing reflection: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if game progress features are available.
        /// </summary>
        public static bool IsGameProgressAvailable()
        {
            InitializeReflectionCache();
            return _gameProgressAvailable;
        }

        /// <summary>
        /// Reset the reflection cache (call when character changes).
        /// </summary>
        public static void ResetReflectionCache()
        {
            _reflectionInitialized = false;
            _gameProgressAvailable = false;
            _cachedGameSaveInstance = null;
            _cachedGetProgressBoolMethod = null;
            _cachedGetProgressIntMethod = null;
        }

        /// <summary>
        /// Get the GameSave singleton instance (cached).
        /// </summary>
        private static object GetGameSaveInstance()
        {
            InitializeReflectionCache();
            return _cachedGameSaveInstance;
        }

        /// <summary>
        /// Log all currently discovered mappings for debugging.
        /// </summary>
        public static void LogAllMappings()
        {
            Plugin.Log?.LogInfo($"[MuseumPatches] Current mappings ({_progressKeyToItemId.Count} total):");
            foreach (var kvp in _progressKeyToItemId)
            {
                var item = MuseumContent.FindById(kvp.Value);
                Plugin.Log?.LogInfo($"  '{kvp.Key}' -> {kvp.Value} ({item?.Name ?? "unknown"})");
            }
        }

        /// <summary>
        /// Enable or disable debug logging.
        /// </summary>
        public static void SetDebugLogging(bool enabled)
        {
            _debugLogging = enabled;
            Plugin.Log?.LogInfo($"[MuseumPatches] Debug logging: {enabled}");
        }
    }

    /// <summary>
    /// Harmony patches for HungryMonster (museum bundle donations).
    /// Tracks items when they're donated to museum bundles in real-time.
    /// </summary>
    public static class HungryMonsterPatches
    {
        private static bool _isHooked = false;

        /// <summary>
        /// Apply patches to HungryMonster for real-time donation tracking.
        /// Call from Plugin.ApplyPatches().
        /// </summary>
        public static void ApplyPatches(Harmony harmony)
        {
            if (_isHooked) return;

            try
            {
                var hungryMonsterType = AccessTools.TypeByName("Wish.HungryMonster") ?? AccessTools.TypeByName("HungryMonster");
                if (hungryMonsterType == null)
                {
                    Plugin.Log?.LogWarning("[HungryMonsterPatches] Could not find HungryMonster type");
                    return;
                }

                // Patch Chest.SaveInventory (declared method) - HarmonyX expects patching base, not override
                var chestType = AccessTools.TypeByName("Wish.Chest") ?? AccessTools.TypeByName("Chest");
                if (chestType != null)
                {
                    var saveInventoryMethod = AccessTools.DeclaredMethod(chestType, "SaveInventory");
                    if (saveInventoryMethod == null)
                        saveInventoryMethod = AccessTools.Method(chestType, "SaveInventory");
                    if (saveInventoryMethod != null)
                    {
                        var postfix = AccessTools.Method(typeof(HungryMonsterPatches), nameof(OnSaveInventory));
                        harmony.Patch(saveInventoryMethod, postfix: new HarmonyMethod(postfix));
                        Plugin.Log?.LogInfo("[HungryMonsterPatches] Patched Chest.SaveInventory (HungryMonster filter in postfix)");
                    }
                    else
                        Plugin.Log?.LogWarning("[HungryMonsterPatches] Could not find Chest.SaveInventory");
                }
                else
                    Plugin.Log?.LogWarning("[HungryMonsterPatches] Could not find Chest type");

                _isHooked = true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[HungryMonsterPatches] Error applying patches: {ex.Message}");
            }
        }

        /// <summary>
        /// Called after HungryMonster.SaveInventory() - tracks donated items.
        /// </summary>
        public static void OnSaveInventory(object __instance)
        {
            try
            {
                var instanceType = __instance.GetType();

                // Check if this is a museum bundle (bundleType == MuseumBundle)
                var bundleTypeField = instanceType.GetField("bundleType", BindingFlags.Public | BindingFlags.Instance);
                if (bundleTypeField == null) return;

                var bundleType = bundleTypeField.GetValue(__instance);
                if (bundleType == null) return;

                // BundleType.MuseumBundle = 3
                int bundleTypeValue = (int)bundleType;
                if (bundleTypeValue != 3) return; // Not a museum bundle

                // Get the progress key for this bundle
                var progressTokenField = instanceType.GetField("progressTokenWhenFull", BindingFlags.Public | BindingFlags.Instance);
                if (progressTokenField == null) return;

                var progressToken = progressTokenField.GetValue(__instance);
                if (progressToken == null) return;

                // Get progressID from the Progress object
                var progressIdField = progressToken.GetType().GetField("progressID", BindingFlags.Public | BindingFlags.Instance);
                if (progressIdField == null) return;

                string progressKey = progressIdField.GetValue(progressToken) as string;
                if (string.IsNullOrEmpty(progressKey)) return;

                // Get the selling inventory
                var inventoryField = instanceType.GetField("sellingInventory", BindingFlags.Public | BindingFlags.Instance);
                if (inventoryField == null) return;

                var inventory = inventoryField.GetValue(__instance);
                if (inventory == null) return;

                // Get Items property from inventory
                var itemsProperty = inventory.GetType().GetProperty("Items", BindingFlags.Public | BindingFlags.Instance);
                if (itemsProperty == null) return;

                var items = itemsProperty.GetValue(inventory) as IEnumerable;
                if (items == null) return;

                // Find the corresponding bundle in our content
                var bundle = MuseumContent.FindBundleByProgressKey(progressKey);
                if (bundle == null)
                {
                    Plugin.Log?.LogDebug($"[HungryMonsterPatches] No bundle found for progress key: {progressKey}");
                    return;
                }

                var manager = Plugin.GetDonationManager();
                if (manager == null) return;

                int trackedCount = 0;

                // Iterate through inventory items and mark them as donated
                foreach (var slotData in items)
                {
                    var slotDataType = slotData.GetType();
                    var itemField = slotDataType.GetField("item", BindingFlags.Public | BindingFlags.Instance);
                    if (itemField == null) continue;

                    var item = itemField.GetValue(slotData);
                    if (item == null) continue;

                    // Get item ID
                    var idMethod = item.GetType().GetMethod("ID", BindingFlags.Public | BindingFlags.Instance);
                    if (idMethod == null) continue;

                    int gameItemId = (int)idMethod.Invoke(item, null);

                    // Find this item in our content
                    var museumItem = MuseumContent.FindByGameItemId(gameItemId);
                    if (museumItem != null && !manager.HasDonated(museumItem.Id))
                    {
                        manager.MarkDonated(museumItem.Id);
                        trackedCount++;
                        Plugin.Log?.LogInfo($"[HungryMonsterPatches] Auto-tracked donation: {museumItem.Name} (ID: {gameItemId})");
                    }
                }

                if (trackedCount > 0)
                {
                    Plugin.Log?.LogInfo($"[HungryMonsterPatches] Tracked {trackedCount} new donations for bundle '{bundle.Name}'");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[HungryMonsterPatches] Error tracking donation: {ex.Message}");
            }
        }
    }
}
