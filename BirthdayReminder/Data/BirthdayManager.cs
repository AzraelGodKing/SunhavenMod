using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BirthdayReminder.Data
{
    /// <summary>
    /// Manages birthday detection and gift tracking
    /// </summary>
    public class BirthdayManager
    {
        private GiftTrackingData _giftTracking;
        private List<BirthdayDisplayInfo> _todaysBirthdays = new List<BirthdayDisplayInfo>();
        private string _currentCharacter;

        // Cached date for HUD display (avoids logging spam from per-frame calls)
        private string _cachedDateString = "";

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
        /// Set a temporary status message
        /// </summary>
        public void SetStatusMessage(string message)
        {
            _statusMessage = message;
            _statusMessageTimer = STATUS_MESSAGE_DURATION;
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
                    bool hasGifted = _giftTracking.HasGifted(npc.NPCName);

                    // Get 3 random gift suggestions for the short hint
                    var randomGifts = BirthdayCache.GetRandomGiftSuggestions(npc.NPCName, 3);
                    string giftHint = randomGifts.Count > 0 ? $"Loves: {string.Join(", ", randomGifts)}" : "";

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

                    // Check for characterData specifically
                    var charDataProp = AccessTools.Property(npcType, "characterData")
                                       ?? AccessTools.Property(npcType, "CharacterData");
                    if (charDataProp != null)
                    {
                        var charData = charDataProp.GetValue(npc);
                        if (charData != null)
                        {
                            Plugin.Log?.LogInfo($"[DEBUG] --- characterData PROPERTIES ({charData.GetType().Name}) ---");
                            foreach (var prop in charData.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                            {
                                try
                                {
                                    var value = prop.GetValue(charData);
                                    string valueStr = value?.ToString() ?? "null";
                                    if (valueStr.Length > 100) valueStr = valueStr.Substring(0, 100) + "...";
                                    Plugin.Log?.LogInfo($"[DEBUG]     {prop.Name} ({prop.PropertyType.Name}) = {valueStr}");
                                }
                                catch { }
                            }
                        }
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
            catch
            {
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
            catch { }

            return "Unknown";
        }

        /// <summary>
        /// Find NPCs whose birthday is today using the static cache
        /// </summary>
        private List<NPCBirthday> GetNPCsWithBirthdayToday(string season, int day)
        {
            // Use the hardcoded birthday cache - much more reliable than reflection
            var birthdays = BirthdayCache.GetBirthdaysForDate(season, day);

            foreach (var birthday in birthdays)
            {
                Plugin.Log?.LogInfo($"[BirthdayManager] Found birthday: {birthday.NPCName} ({birthday.Season} {birthday.Day})");
            }

            return birthdays;
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
                catch { }

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
