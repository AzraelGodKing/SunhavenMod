using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BirthdayReminder.Data
{
    /// <summary>
    /// Manages birthday detection and gift tracking.
    /// Uses game's NPCGiftTable data when available, falls back to BirthdayCache.
    /// </summary>
    public class BirthdayManager
    {
        private GiftTrackingData _giftTracking;
        private List<BirthdayDisplayInfo> _todaysBirthdays = new List<BirthdayDisplayInfo>();
        private string _currentCharacter;

        // Cached date for HUD display (avoids logging spam from per-frame calls)
        private string _cachedDateString = "";

        // Staleness check: track the date we last checked so we can detect day changes
        private string _lastCheckedDateKey = "";
        private float _stalenessCheckTimer = 0f;
        private const float STALENESS_CHECK_INTERVAL = 10f; // Check every 10 seconds

        private static readonly System.Random _random = new System.Random();

        // Cached reflection data for game API access
        private static bool _reflectionInitialized = false;
        private static Type _npcManagerType;
        private static Type _npcGiftTableType;
        private static PropertyInfo _npcManagerInstanceProp;
        private static FieldInfo _npcsDictField;
        private static FieldInfo _birthDayField;
        private static FieldInfo _birthMonthField;
        private static FieldInfo _love2Field;
        private static FieldInfo _like2Field;
        private static FieldInfo _gaveGiftForDayField;
        private static FieldInfo _giftTableField; // NPCAI.giftTable is a field, not a property
        private static bool _useGameData = false;

        public event Action OnBirthdaysUpdated;

        public List<BirthdayDisplayInfo> TodaysBirthdays => _todaysBirthdays;
        public bool HasBirthdays => _todaysBirthdays.Count > 0;
        public bool HasUngiftedBirthdays => _todaysBirthdays.Any(b => !b.HasBeenGifted);

        // Status message for notifications (e.g., "Refreshed!")
        private string _statusMessage = "";
        private float _statusMessageTimer = 0f;
        private const float STATUS_MESSAGE_DURATION = 3f;

        public string StatusMessage => _statusMessage;
        public bool HasStatusMessage => !string.IsNullOrEmpty(_statusMessage) && _statusMessageTimer > 0;

        /// <summary>
        /// Update status message timer (call from Update)
        /// </summary>
        public void UpdateStatusMessage(float deltaTime)
        {
            if (_statusMessageTimer > 0)
            {
                _statusMessageTimer -= deltaTime;
                if (_statusMessageTimer <= 0)
                {
                    _statusMessage = "";
                }
            }
        }

        /// <summary>
        /// Periodic check: if the in-game date has changed since we last checked,
        /// automatically refresh birthday data. Call from Update().
        /// </summary>
        public void CheckForDateChange(float deltaTime)
        {
            _stalenessCheckTimer += deltaTime;
            if (_stalenessCheckTimer < STALENESS_CHECK_INTERVAL)
                return;
            _stalenessCheckTimer = 0f;

            try
            {
                var (year, season, day) = GetCurrentDate();
                if (string.IsNullOrEmpty(season))
                    return;

                string dateKey = $"{year}_{season}_{day}";
                if (dateKey != _lastCheckedDateKey)
                {
                    Plugin.Log?.LogInfo($"[BirthdayManager] Date changed to {season} {day} — refreshing birthdays");
                    _lastCheckedDateKey = dateKey;
                    CheckTodaysBirthdays();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[BirthdayManager] Date check: {ex.Message}");
            }
        }

        /// <summary>
        /// Set a temporary status message
        /// </summary>
        public void SetStatusMessage(string message)
        {
            _statusMessage = message;
            _statusMessageTimer = STATUS_MESSAGE_DURATION;
        }

        /// <summary>
        /// Initialize reflection cache for accessing game's NPC data
        /// </summary>
        private void InitializeReflectionCache()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            try
            {
                // Get NPCManager type and instance
                _npcManagerType = AccessTools.TypeByName("Wish.NPCManager");
                if (_npcManagerType == null)
                {
                    Plugin.Log?.LogDebug("[BirthdayManager] NPCManager type not found - using cache");
                    return;
                }

                // Get NPCManager.Instance
                var singletonType = AccessTools.TypeByName("Wish.SingletonBehaviour`1");
                if (singletonType != null)
                {
                    var genericType = singletonType.MakeGenericType(_npcManagerType);
                    _npcManagerInstanceProp = genericType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                }

                // Get _npcs dictionary field
                _npcsDictField = AccessTools.Field(_npcManagerType, "_npcs")
                                 ?? AccessTools.Field(_npcManagerType, "npcs");

                // Get NPCGiftTable type for birthday and gift data
                _npcGiftTableType = AccessTools.TypeByName("Wish.NPCGiftTable");
                if (_npcGiftTableType != null)
                {
                    _birthDayField = AccessTools.Field(_npcGiftTableType, "birthDay");
                    _birthMonthField = AccessTools.Field(_npcGiftTableType, "birthMonth");
                    _love2Field = AccessTools.Field(_npcGiftTableType, "love2");
                    _like2Field = AccessTools.Field(_npcGiftTableType, "like2");
                }

                // Get NPCAI type for gaveGiftForDay and giftTable
                var npcaiType = AccessTools.TypeByName("Wish.NPCAI");
                if (npcaiType != null)
                {
                    _gaveGiftForDayField = AccessTools.Field(npcaiType, "gaveGiftForDay");
                    _giftTableField = AccessTools.Field(npcaiType, "giftTable"); // private field in NPCAI
                }

                // Check if we have enough to use game data
                _useGameData = _npcManagerInstanceProp != null &&
                               _npcsDictField != null &&
                               _birthDayField != null &&
                               _birthMonthField != null &&
                               _giftTableField != null;

                if (_useGameData)
                {
                    Plugin.Log?.LogInfo("[BirthdayManager] Game data API initialized - using NPCGiftTable for birthdays");
                }
                else
                {
                    Plugin.Log?.LogDebug("[BirthdayManager] Game data API not available - using hardcoded cache");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[BirthdayManager] Error initializing game data API: {ex.Message}");
                _useGameData = false;
            }
        }

        /// <summary>
        /// Manual refresh - used by debug hotkey
        /// </summary>
        public void ManualRefresh()
        {
            Plugin.Log?.LogInfo("[BirthdayManager] Manual refresh triggered");
            CheckTodaysBirthdays();
            SetStatusMessage($"Refreshed! Found {_todaysBirthdays.Count} birthday(s)");
        }

        /// <summary>
        /// Get the current date formatted as "Season XX" (uses cached value to avoid per-frame logging)
        /// </summary>
        public string CurrentDateFormatted => _cachedDateString;

        /// <summary>
        /// Check for birthdays on the current day
        /// </summary>
        public void CheckTodaysBirthdays()
        {
            _todaysBirthdays.Clear();

            try
            {
                var (year, season, day) = GetCurrentDate(logDate: true);
                if (string.IsNullOrEmpty(season)) return;

                // Update cached date string for HUD display
                _cachedDateString = $"{season} {day:D2}";
                _lastCheckedDateKey = $"{year}_{season}_{day}";

                // Reset gift tracking if it's a new day
                var characterName = GetCurrentCharacterName();
                if (_giftTracking == null || !_giftTracking.IsSameDay(year, season, day) || _currentCharacter != characterName)
                {
                    _giftTracking = new GiftTrackingData(characterName, year, season, day);
                    _currentCharacter = characterName;
                }

                // Get all NPCs and check birthdays
                var npcBirthdays = GetNPCsWithBirthdayToday(season, day);

                foreach (var npc in npcBirthdays)
                {
                    // Check gift status - prefer game's gaveGiftForDay, fall back to our tracking
                    bool hasGifted = false;
                    if (_gaveGiftForDayField != null && _useGameData)
                    {
                        hasGifted = CheckGaveGiftForDay(npc.NPCName);
                    }
                    if (!hasGifted)
                    {
                        hasGifted = _giftTracking.HasGifted(npc.NPCName);
                    }

                    // Get gift suggestions - prefer NPC's own gifts from game data, fall back to cache
                    string giftHint;
                    if (npc.LovedGifts.Count > 0)
                    {
                        // Use the NPC's loved gifts from game data (or cache); pick up to 3 at random without Guid/LINQ allocations
                        var randomGifts = TakeRandom(npc.LovedGifts, 3);
                        giftHint = $"Loves: {string.Join(", ", randomGifts)}";
                    }
                    else
                    {
                        // Fall back to cache suggestions
                        var randomGifts = BirthdayCache.GetRandomGiftSuggestions(npc.NPCName, 3);
                        giftHint = randomGifts.Count > 0 ? $"Loves: {string.Join(", ", randomGifts)}" : "";
                    }

                    // Pass full gift lists for expanded view
                    _todaysBirthdays.Add(new BirthdayDisplayInfo(
                        npc.NPCName,
                        hasGifted,
                        giftHint,
                        npc.LovedGifts.ToList(),
                        npc.LikedGifts.ToList()
                    ));
                }

                Plugin.Log?.LogInfo($"[BirthdayManager] Found {_todaysBirthdays.Count} birthdays on {season} {day}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[BirthdayManager] Error checking birthdays: {ex.Message}");
            }

            OnBirthdaysUpdated?.Invoke();
        }

        /// <summary>
        /// Returns up to <paramref name="count"/> items chosen at random using Fisher-Yates shuffle.
        /// Avoids Guid and LINQ allocations.
        /// </summary>
        private static List<string> TakeRandom(IList<string> source, int count)
        {
            if (source == null || source.Count == 0) return new List<string>();
            var list = new List<string>(source);
            if (list.Count <= count) return list;
            // Fisher-Yates shuffle, then take first count
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
            list.RemoveRange(count, list.Count - count);
            return list;
        }

        /// <summary>
        /// Mark an NPC as gifted
        /// </summary>
        public void MarkGifted(string npcName)
        {
            if (_giftTracking == null) return;

            _giftTracking.MarkGifted(npcName);

            var birthday = _todaysBirthdays.FirstOrDefault(b => b.NPCName == npcName);
            if (birthday != null)
            {
                birthday.HasBeenGifted = true;
            }

            OnBirthdaysUpdated?.Invoke();
        }

        /// <summary>
        /// Reset data when switching to a new character/save
        /// </summary>
        public void ResetForNewCharacter(string newCharacterName)
        {
            Plugin.Log?.LogInfo($"[BirthdayManager] Resetting for new character: {newCharacterName}");

            _currentCharacter = newCharacterName;
            _giftTracking = null;
            _todaysBirthdays.Clear();

            OnBirthdaysUpdated?.Invoke();
        }

        #region Debug Methods

        /// <summary>
        /// Debug: Add a test birthday for UI testing
        /// </summary>
        public void DebugAddTestBirthday(string npcName, string giftHint = "")
        {
            Plugin.Log?.LogInfo($"[DEBUG] Adding test birthday for: {npcName}");

            // Don't add duplicates
            if (_todaysBirthdays.Any(b => b.NPCName == npcName))
            {
                Plugin.Log?.LogInfo($"[DEBUG] {npcName} already has a birthday entry");
                return;
            }

            _todaysBirthdays.Add(new BirthdayDisplayInfo(npcName, false, giftHint));

            // Initialize gift tracking if needed
            if (_giftTracking == null)
            {
                var (year, season, day) = GetCurrentDate();
                _giftTracking = new GiftTrackingData("Debug", year, season, day);
            }

            OnBirthdaysUpdated?.Invoke();
        }

        /// <summary>
        /// Debug: Clear all birthday entries
        /// </summary>
        public void DebugClearBirthdays()
        {
            Plugin.Log?.LogInfo("[DEBUG] Clearing all birthdays");
            _todaysBirthdays.Clear();
            _giftTracking = null;
            OnBirthdaysUpdated?.Invoke();
        }

        /// <summary>
        /// Debug: Log current state
        /// </summary>
        public void DebugLogState()
        {
            Plugin.Log?.LogInfo($"[DEBUG] === Birthday Manager State ===");
            Plugin.Log?.LogInfo($"[DEBUG] Current Character: {_currentCharacter ?? "None"}");
            Plugin.Log?.LogInfo($"[DEBUG] Birthdays Today: {_todaysBirthdays.Count}");

            foreach (var birthday in _todaysBirthdays)
            {
                Plugin.Log?.LogInfo($"[DEBUG]   - {birthday.NPCName} (Gifted: {birthday.HasBeenGifted})");
            }

            if (_giftTracking != null)
            {
                Plugin.Log?.LogInfo($"[DEBUG] Gift Tracking: {_giftTracking.Season} {_giftTracking.Day}, Year {_giftTracking.Year}");
                Plugin.Log?.LogInfo($"[DEBUG] Gifted NPCs: {string.Join(", ", _giftTracking.GiftedNPCs)}");
            }

            Plugin.Log?.LogInfo($"[DEBUG] === End State ===");
        }

        /// <summary>
        /// Debug: Load ALL NPC birthdays from cache (not just today's)
        /// </summary>
        public void DebugLoadAllBirthdays()
        {
            Plugin.Log?.LogInfo("[DEBUG] Loading ALL NPC birthdays from cache...");
            _todaysBirthdays.Clear();

            foreach (var birthday in BirthdayCache.AllBirthdays)
            {
                string displayName = $"{birthday.NPCName} ({birthday.Season} {birthday.Day})";
                _todaysBirthdays.Add(new BirthdayDisplayInfo(displayName, false, ""));
            }

            Plugin.Log?.LogInfo($"[DEBUG] Loaded {_todaysBirthdays.Count} NPC birthdays from cache");
            OnBirthdaysUpdated?.Invoke();
        }

        /// <summary>
        /// Debug: Dump ALL info about a specific NPC (e.g., "Lynn")
        /// </summary>
        public void DebugDumpNPCInfo(string targetNpcName = "Lynn")
        {
            Plugin.Log?.LogInfo($"[DEBUG] ========== DUMPING NPC INFO: {targetNpcName} ==========");

            try
            {
                var npcManagerType = AccessTools.TypeByName("Wish.NPCManager");
                if (npcManagerType == null)
                {
                    Plugin.Log?.LogWarning("[DEBUG] NPCManager type not found");
                    return;
                }

                var npcManager = GetSingletonInstance(npcManagerType);
                if (npcManager == null)
                {
                    Plugin.Log?.LogWarning("[DEBUG] NPCManager instance not found");
                    return;
                }

                var npcsField = AccessTools.Field(npcManagerType, "_npcs")
                                ?? AccessTools.Field(npcManagerType, "npcs");
                var npcsDict = npcsField?.GetValue(npcManager);
                if (npcsDict == null)
                {
                    Plugin.Log?.LogWarning("[DEBUG] NPCs dictionary not found");
                    return;
                }

                var valuesProperty = npcsDict.GetType().GetProperty("Values");
                var values = valuesProperty?.GetValue(npcsDict) as System.Collections.IEnumerable;
                if (values == null) return;

                foreach (var npc in values)
                {
                    if (npc == null) continue;

                    var npcType = npc.GetType();
                    var nameProp = AccessTools.Property(npcType, "NPCName")
                                   ?? AccessTools.Property(npcType, "npcName")
                                   ?? AccessTools.Property(npcType, "Name");
                    string npcName = nameProp?.GetValue(npc)?.ToString();

                    if (string.IsNullOrEmpty(npcName) || !npcName.Equals(targetNpcName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    Plugin.Log?.LogInfo($"[DEBUG] Found {npcName}! Type: {npcType.FullName}");
                    Plugin.Log?.LogInfo($"[DEBUG] --- PROPERTIES ---");

                    // Dump all properties
                    foreach (var prop in npcType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                    {
                        try
                        {
                            var value = prop.GetValue(npc);
                            string valueStr = value?.ToString() ?? "null";
                            if (valueStr.Length > 100) valueStr = valueStr.Substring(0, 100) + "...";
                            Plugin.Log?.LogInfo($"[DEBUG]   {prop.Name} ({prop.PropertyType.Name}) = {valueStr}");
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log?.LogInfo($"[DEBUG]   {prop.Name} ({prop.PropertyType.Name}) = ERROR: {ex.Message}");
                        }
                    }

                    Plugin.Log?.LogInfo($"[DEBUG] --- FIELDS ---");

                    // Dump all fields
                    foreach (var field in npcType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                    {
                        try
                        {
                            var value = field.GetValue(npc);
                            string valueStr = value?.ToString() ?? "null";
                            if (valueStr.Length > 100) valueStr = valueStr.Substring(0, 100) + "...";
                            Plugin.Log?.LogInfo($"[DEBUG]   {field.Name} ({field.FieldType.Name}) = {valueStr}");
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log?.LogInfo($"[DEBUG]   {field.Name} ({field.FieldType.Name}) = ERROR: {ex.Message}");
                        }
                    }

                    // Note: NPCAI does not have characterData/CharacterData - those are on GameSave.CurrentSave.
                    // NPCAI has giftTable (field) for birthday/gift data.
                    var giftTableField = AccessTools.Field(npcType, "giftTable");
                    if (giftTableField != null)
                    {
                        var gt = giftTableField.GetValue(npc);
                        Plugin.Log?.LogInfo($"[DEBUG] giftTable (field) = {gt?.GetType().Name ?? "null"}");
                    }

                    Plugin.Log?.LogInfo($"[DEBUG] ========== END {npcName} ==========");
                    return;
                }

                Plugin.Log?.LogWarning($"[DEBUG] NPC '{targetNpcName}' not found in NPCManager");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[DEBUG] Error dumping NPC info: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Get the current in-game date using DayCycle
        /// </summary>
        /// <param name="logDate">If true, logs the date (only use for actual birthday checks, not per-frame HUD updates)</param>
        private (int year, string season, int day) GetCurrentDate(bool logDate = false)
        {
            try
            {
                // Use DayCycle class directly - much more reliable
                var dayCycleType = AccessTools.TypeByName("Wish.DayCycle");
                if (dayCycleType != null)
                {
                    // Get static properties
                    var yearProp = AccessTools.Property(dayCycleType, "Year");
                    var monthDayProp = AccessTools.Property(dayCycleType, "MonthDay");

                    int year = yearProp != null ? (int)yearProp.GetValue(null) : 1;
                    int day = monthDayProp != null ? (int)monthDayProp.GetValue(null) : 1;

                    // Get Season from instance (it's not static)
                    var dayCycleInstance = GetSingletonInstance(dayCycleType);
                    string season = "Spring";

                    if (dayCycleInstance != null)
                    {
                        var seasonProp = AccessTools.Property(dayCycleType, "Season");
                        if (seasonProp != null)
                        {
                            var seasonValue = seasonProp.GetValue(dayCycleInstance);
                            season = seasonValue?.ToString() ?? "Spring";
                        }
                    }

                    // Only log when explicitly requested (birthday checks, not per-frame HUD updates)
                    if (logDate)
                    {
                        Plugin.Log?.LogInfo($"[BirthdayManager] Got date from DayCycle: Year {year}, {season} {day}");
                    }
                    return (year, season, day);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[BirthdayManager] Error getting date: {ex.Message}");
            }

            return (1, "Spring", 1);
        }

        /// <summary>
        /// Get singleton instance using reflection
        /// </summary>
        private object GetSingletonInstance(Type targetType)
        {
            try
            {
                // Try to find SingletonBehaviour<T> base class
                var singletonBaseType = AccessTools.TypeByName("Wish.SingletonBehaviour`1");
                if (singletonBaseType != null)
                {
                    var genericType = singletonBaseType.MakeGenericType(targetType);
                    var instanceProp = AccessTools.Property(genericType, "Instance");
                    return instanceProp?.GetValue(null);
                }

                // Alternative: look for static Instance property directly
                var directInstanceProp = AccessTools.Property(targetType, "Instance");
                return directInstanceProp?.GetValue(null);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[BirthdayManager] GetSingletonInstance: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get the current character name
        /// </summary>
        private string GetCurrentCharacterName()
        {
            try
            {
                var gameSaveType = AccessTools.TypeByName("Wish.GameSave");
                if (gameSaveType != null)
                {
                    var gameSave = GetSingletonInstance(gameSaveType);

                    if (gameSave != null)
                    {
                        var currentCharProp = AccessTools.Property(gameSaveType, "CurrentCharacter");
                        var currentChar = currentCharProp?.GetValue(gameSave);
                        return currentChar?.ToString() ?? "Unknown";
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[BirthdayManager] Error getting character name: {ex.Message}");
            }

            return "Unknown";
        }

        /// <summary>
        /// Find NPCs whose birthday is today.
        /// Uses game's NPCGiftTable data when available, falls back to hardcoded cache.
        /// </summary>
        private List<NPCBirthday> GetNPCsWithBirthdayToday(string season, int day)
        {
            // Initialize reflection cache on first call
            InitializeReflectionCache();

            // Try game data first
            if (_useGameData)
            {
                var gameBirthdays = GetBirthdaysFromGameData(season, day);
                if (gameBirthdays.Count > 0)
                {
                    foreach (var birthday in gameBirthdays)
                    {
                        Plugin.Log?.LogInfo($"[BirthdayManager] Found birthday (game data): {birthday.NPCName} ({birthday.Season} {birthday.Day})");
                    }
                    return gameBirthdays;
                }
            }

            // Fall back to hardcoded cache
            var cacheBirthdays = BirthdayCache.GetBirthdaysForDate(season, day);
            foreach (var birthday in cacheBirthdays)
            {
                Plugin.Log?.LogInfo($"[BirthdayManager] Found birthday (cache): {birthday.NPCName} ({birthday.Season} {birthday.Day})");
            }
            return cacheBirthdays;
        }

        /// <summary>
        /// Get birthdays from game's NPCManager._npcs dictionary using NPCGiftTable data.
        /// </summary>
        private List<NPCBirthday> GetBirthdaysFromGameData(string season, int day)
        {
            var birthdays = new List<NPCBirthday>();

            try
            {
                // Get NPCManager instance
                var npcManager = _npcManagerInstanceProp?.GetValue(null);
                if (npcManager == null)
                {
                    Plugin.Log?.LogDebug("[BirthdayManager] NPCManager instance is null");
                    return birthdays;
                }

                // Get _npcs dictionary
                var npcsDict = _npcsDictField?.GetValue(npcManager);
                if (npcsDict == null)
                {
                    Plugin.Log?.LogDebug("[BirthdayManager] NPCs dictionary is null");
                    return birthdays;
                }

                // Iterate through NPCs
                var valuesProperty = npcsDict.GetType().GetProperty("Values");
                var values = valuesProperty?.GetValue(npcsDict) as System.Collections.IEnumerable;
                if (values == null) return birthdays;

                foreach (var npc in values)
                {
                    if (npc == null) continue;

                    try
                    {
                        // Get NPC name
                        var npcType = npc.GetType();
                        var nameProp = AccessTools.Property(npcType, "NPCName")
                                       ?? AccessTools.Property(npcType, "ActualNPCName")
                                       ?? AccessTools.Property(npcType, "OriginalName")
                                       ?? AccessTools.Property(npcType, "npcName")
                                       ?? AccessTools.Property(npcType, "Name");
                        string npcName = nameProp?.GetValue(npc)?.ToString();
                        if (string.IsNullOrEmpty(npcName)) continue;

                        // Get NPCGiftTable - NPCAI.giftTable is a private field (not characterData/CharacterData)
                        var giftTable = _giftTableField?.GetValue(npc);
                        if (giftTable == null) continue;

                        // Get birthDay and birthMonth from NPCGiftTable
                        int birthDay = 0;
                        object birthMonth = null;

                        if (_birthDayField != null)
                            birthDay = (int)_birthDayField.GetValue(giftTable);

                        if (_birthMonthField != null)
                            birthMonth = _birthMonthField.GetValue(giftTable);

                        string birthSeason = birthMonth?.ToString() ?? "";

                        // Check if birthday matches
                        if (birthDay == day && string.Equals(birthSeason, season, StringComparison.OrdinalIgnoreCase))
                        {
                            var birthday = new NPCBirthday(npcName, birthSeason, birthDay);

                            // Get gift preferences from NPCGiftTable
                            GetGiftsFromGameData(giftTable, birthday);

                            birthdays.Add(birthday);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogDebug($"[BirthdayManager] Error checking NPC: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[BirthdayManager] Error getting birthdays from game: {ex.Message}");
            }

            return birthdays;
        }

        /// <summary>
        /// Get loved and liked gifts from NPCGiftTable's love2 and like2 fields.
        /// </summary>
        private void GetGiftsFromGameData(object giftTable, NPCBirthday birthday)
        {
            try
            {
                // Get love2 list (loved items)
                if (_love2Field != null)
                {
                    var love2 = _love2Field.GetValue(giftTable) as System.Collections.IList;
                    if (love2 != null)
                    {
                        foreach (var itemId in love2)
                        {
                            string itemName = GetItemNameFromId(itemId);
                            if (!string.IsNullOrEmpty(itemName))
                            {
                                birthday.LovedGifts.Add(itemName);
                            }
                        }
                    }
                }

                // Get like2 list (liked items)
                if (_like2Field != null)
                {
                    var like2 = _like2Field.GetValue(giftTable) as System.Collections.IList;
                    if (like2 != null)
                    {
                        foreach (var itemId in like2)
                        {
                            string itemName = GetItemNameFromId(itemId);
                            if (!string.IsNullOrEmpty(itemName))
                            {
                                birthday.LikedGifts.Add(itemName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[BirthdayManager] Error getting gifts from game data: {ex.Message}");
            }
        }

        /// <summary>
        /// Get item name from item ID using the game's Database.
        /// </summary>
        private string GetItemNameFromId(object itemIdOrObj)
        {
            try
            {
                int itemId;
                if (itemIdOrObj is int id)
                {
                    itemId = id;
                }
                else
                {
                    // Could be an Item object or enum, try to get ID
                    var idProp = itemIdOrObj.GetType().GetProperty("id")
                                 ?? itemIdOrObj.GetType().GetProperty("ID");
                    if (idProp != null)
                    {
                        itemId = (int)idProp.GetValue(itemIdOrObj);
                    }
                    else
                    {
                        // Try casting to int (for enums)
                        itemId = Convert.ToInt32(itemIdOrObj);
                    }
                }

                // Get item from Database
                var databaseType = AccessTools.TypeByName("Wish.Database");
                if (databaseType != null)
                {
                    var getItemMethod = AccessTools.Method(databaseType, "GetItem", new[] { typeof(int) });
                    if (getItemMethod != null)
                    {
                        var item = getItemMethod.Invoke(null, new object[] { itemId });
                        if (item != null)
                        {
                            var nameProp = AccessTools.Property(item.GetType(), "name")
                                           ?? AccessTools.Property(item.GetType(), "Name");
                            return nameProp?.GetValue(item)?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[BirthdayManager] Error getting item name: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Check if an NPC has been gifted today using game's gaveGiftForDay field.
        /// </summary>
        private bool CheckGaveGiftForDay(string npcName)
        {
            try
            {
                if (_gaveGiftForDayField == null) return false;

                // Get NPC instance
                var npcManager = _npcManagerInstanceProp?.GetValue(null);
                if (npcManager == null) return false;

                var npcsDict = _npcsDictField?.GetValue(npcManager);
                if (npcsDict == null) return false;

                // Try to get the NPC by name from dictionary
                var tryGetValueMethod = npcsDict.GetType().GetMethod("TryGetValue");
                if (tryGetValueMethod != null)
                {
                    var args = new object[] { npcName, null };
                    bool found = (bool)tryGetValueMethod.Invoke(npcsDict, args);
                    if (found && args[1] != null)
                    {
                        var npc = args[1];
                        return (bool)_gaveGiftForDayField.GetValue(npc);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[BirthdayManager] Error checking gift status for {npcName}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Try to get NPC gift preferences
        /// </summary>
        private void GetNPCGiftPreferences(object npc, Type npcType, NPCBirthday birthday)
        {
            try
            {
                // Look for loved/liked gifts properties
                var lovedGiftsProp = AccessTools.Property(npcType, "LovedGifts")
                                     ?? AccessTools.Property(npcType, "lovedGifts");
                var likedGiftsProp = AccessTools.Property(npcType, "LikedGifts")
                                     ?? AccessTools.Property(npcType, "likedGifts");

                if (lovedGiftsProp != null)
                {
                    var lovedGifts = lovedGiftsProp.GetValue(npc) as System.Collections.IEnumerable;
                    if (lovedGifts != null)
                    {
                        foreach (var gift in lovedGifts)
                        {
                            string giftName = GetItemName(gift);
                            if (!string.IsNullOrEmpty(giftName))
                            {
                                birthday.LovedGifts.Add(giftName);
                            }
                        }
                    }
                }

                if (likedGiftsProp != null)
                {
                    var likedGifts = likedGiftsProp.GetValue(npc) as System.Collections.IEnumerable;
                    if (likedGifts != null)
                    {
                        foreach (var gift in likedGifts)
                        {
                            string giftName = GetItemName(gift);
                            if (!string.IsNullOrEmpty(giftName))
                            {
                                birthday.LikedGifts.Add(giftName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[BirthdayManager] Error getting gift preferences: {ex.Message}");
            }
        }

        /// <summary>
        /// Get item name from item object or ID
        /// </summary>
        private string GetItemName(object item)
        {
            if (item == null) return null;

            // If it's a string, return it directly
            if (item is string str) return str;

            // If it's an int (item ID), try to get the name from the database
            if (item is int itemId)
            {
                try
                {
                    var databaseType = AccessTools.TypeByName("Wish.Database");
                    if (databaseType != null)
                    {
                        var getItemMethod = AccessTools.Method(databaseType, "GetItem", new[] { typeof(int) });
                        if (getItemMethod != null)
                        {
                            var itemObj = getItemMethod.Invoke(null, new object[] { itemId });
                            if (itemObj != null)
                            {
                                var nameProp = AccessTools.Property(itemObj.GetType(), "name")
                                               ?? AccessTools.Property(itemObj.GetType(), "Name");
                                return nameProp?.GetValue(itemObj)?.ToString();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogDebug($"[BirthdayManager] Error getting gift name for item {itemId}: {ex.Message}");
                }

                return $"Item #{itemId}";
            }

            // Try to get name property
            var itemNameProp = AccessTools.Property(item.GetType(), "name")
                               ?? AccessTools.Property(item.GetType(), "Name");
            return itemNameProp?.GetValue(item)?.ToString();
        }

        /// <summary>
        /// Build a gift hint string for display
        /// </summary>
        private string BuildGiftHint(NPCBirthday npc)
        {
            var hints = new List<string>();

            if (npc.LovedGifts.Count > 0)
            {
                var loved = string.Join(", ", npc.LovedGifts.Take(3));
                hints.Add($"Loves: {loved}");
            }

            if (npc.LikedGifts.Count > 0 && hints.Count == 0)
            {
                var liked = string.Join(", ", npc.LikedGifts.Take(3));
                hints.Add($"Likes: {liked}");
            }

            return hints.Count > 0 ? hints[0] : "";
        }
    }
}
