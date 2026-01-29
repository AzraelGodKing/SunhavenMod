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
                    catch { }
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
                        catch { }
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
                Plugin.Log?.LogInfo("[SYNC] NOTE: The game only stores BUNDLE completion, not individual items.");
                Plugin.Log?.LogInfo("[SYNC] This will mark ALL items in completed bundles as donated.");

                // Get GameSave instance
                object gameSaveInstance = GetGameSaveInstance();
                if (gameSaveInstance == null)
                {
                    Plugin.Log?.LogWarning("[SYNC] GameSave instance is null - cannot sync");
                    return;
                }

                var gameSaveType = gameSaveInstance.GetType();

                // Find GetProgressBoolCharacter method
                var getProgressMethod = gameSaveType.GetMethod("GetProgressBoolCharacter", new[] { typeof(string) });
                if (getProgressMethod == null)
                {
                    Plugin.Log?.LogWarning("[SYNC] Could not find GetProgressBoolCharacter method");
                    return;
                }

                // Check all bundles for completion
                int bundlesSynced = SyncCompletedBundles(gameSaveInstance, getProgressMethod, manager);

                Plugin.Log?.LogInfo($"[SYNC] Synced {bundlesSynced} completed bundles");
                Plugin.Log?.LogInfo("[SYNC] ======= Sync complete =======");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[SYNC] Error syncing with game progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks all known bundle progress keys and marks their items as donated if complete.
        /// </summary>
        private static int SyncCompletedBundles(object gameSaveInstance, MethodInfo getProgressMethod, DonationManager manager)
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

                    Plugin.Log?.LogDebug($"[SYNC] Checking bundle '{bundleId}' with progress key '{progressKey}'");

                    // Check if this bundle is complete in the game
                    bool isComplete = (bool)getProgressMethod.Invoke(gameSaveInstance, new object[] { progressKey });

                    if (isComplete)
                    {
                        Plugin.Log?.LogInfo($"[SYNC] Bundle '{bundleId}' is COMPLETE, marking all items...");

                        // Get all items in this bundle and mark them as donated
                        var items = MuseumContent.GetItemsInBundle(bundleId);
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
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogDebug($"[SYNC] Error checking bundle '{bundleId}': {ex.Message}");
                }
            }

            return bundlesSynced;
        }

        /// <summary>
        /// Get the GameSave singleton instance.
        /// </summary>
        private static object GetGameSaveInstance()
        {
            try
            {
                // Try SingletonBehaviour<GameSave>.Instance
                var gameSaveType = AccessTools.TypeByName("Wish.GameSave") ?? AccessTools.TypeByName("GameSave");
                if (gameSaveType == null) return null;

                // Try the SingletonBehaviour pattern
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var singletonType = assembly.GetType("SingletonBehaviour`1");
                        if (singletonType != null)
                        {
                            var genericType = singletonType.MakeGenericType(gameSaveType);
                            var instanceProp = genericType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                            if (instanceProp != null)
                            {
                                var instance = instanceProp.GetValue(null);
                                if (instance != null) return instance;
                            }
                        }
                    }
                    catch { }
                }

                // Fallback: try direct Instance property
                var directProp = gameSaveType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (directProp != null)
                {
                    return directProp.GetValue(null);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[MuseumPatches] Error getting GameSave instance: {ex.Message}");
            }

            return null;
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
}
