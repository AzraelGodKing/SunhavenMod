using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HavensAlmanac.Data;

namespace HavensAlmanac.Services
{
    internal static class GameRelationshipReader
    {
        private static bool _resolved;
        private static Type _npcManagerType;
        private static PropertyInfo _npcManagerInstanceProp;
        private static FieldInfo _npcsDictField;
        private static Type _gameSaveType;
        private static PropertyInfo _gameSaveInstanceProp;
        private static PropertyInfo _currentSaveProp;
        private static PropertyInfo _characterDataProp;
        private static PropertyInfo _relationshipsProp;
        private static MethodInfo _getProgressBoolMethod;
        private static MethodInfo _getProgressStringMethod;
        private static FieldInfo _gaveGiftForDayField;
        private static PropertyInfo _originalNameProp;
        private static PropertyInfo _localizedNameProp;
        private static PropertyInfo _romanceableProp;

        public static bool IsAvailable
        {
            get
            {
                EnsureResolved();
                return TryGetNpcDictionary(out _) && TryGetSave(out _, out _);
            }
        }

        public static List<RelationshipRow> ReadRows(bool romanceOnly, string nameFilter)
        {
            var rows = new List<RelationshipRow>();
            if (!TryGetNpcDictionary(out IDictionary npcs) || !TryGetSave(out object save, out object characterData))
                return rows;

            object relationships = _relationshipsProp?.GetValue(characterData);
            string primary = ReadProgressString(save, "MarriedWith") ?? string.Empty;
            string filter = nameFilter?.Trim() ?? string.Empty;

            foreach (DictionaryEntry entry in npcs)
            {
                if (entry.Value == null)
                    continue;

                object npc = entry.Value;
                bool romanceable = ReadBool(npc, _romanceableProp);
                if (romanceOnly && !romanceable)
                    continue;

                string key = ReadString(npc, _originalNameProp) ?? entry.Key?.ToString();
                if (string.IsNullOrEmpty(key))
                    continue;

                string display = ReadString(npc, _localizedNameProp) ?? key;
                if (!string.IsNullOrEmpty(filter)
                    && key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0
                    && display.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                float points = ReadRelationshipPoints(relationships, key);
                rows.Add(new RelationshipRow
                {
                    Key = key,
                    DisplayName = display,
                    Points = points,
                    Romanceable = romanceable,
                    IsDating = ReadProgressBool(save, "Dating" + key),
                    IsMarriedTo = ReadProgressBool(save, "MarriedTo" + key),
                    IsPrimarySpouse = !string.IsNullOrEmpty(primary)
                        && primary.Equals(key, StringComparison.Ordinal),
                    GiftedToday = ReadGiftedToday(npc),
                });
            }

            return rows.OrderBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static float ReadRelationshipPoints(object relationships, string key)
        {
            if (relationships == null || string.IsNullOrEmpty(key))
                return 0f;

            try
            {
                if (relationships is IDictionary dict && dict.Contains(key))
                {
                    object raw = dict[key];
                    return Convert.ToSingle(raw);
                }
            }
            catch
            {
            }

            return 0f;
        }

        private static bool ReadGiftedToday(object npc)
        {
            if (npc == null || _gaveGiftForDayField == null)
                return false;

            try
            {
                object raw = _gaveGiftForDayField.GetValue(npc);
                return raw is bool gifted && gifted;
            }
            catch
            {
                return false;
            }
        }

        private static bool ReadProgressBool(object save, string key)
        {
            if (save == null || _getProgressBoolMethod == null || string.IsNullOrEmpty(key))
                return false;

            try
            {
                object raw = _getProgressBoolMethod.Invoke(save, new object[] { key });
                return raw is bool value && value;
            }
            catch
            {
                return false;
            }
        }

        private static string ReadProgressString(object save, string key)
        {
            if (save == null || _getProgressStringMethod == null || string.IsNullOrEmpty(key))
                return null;

            try
            {
                return _getProgressStringMethod.Invoke(save, new object[] { key }) as string;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetNpcDictionary(out IDictionary npcs)
        {
            npcs = null;
            EnsureResolved();
            if (_npcManagerInstanceProp == null || _npcsDictField == null)
                return false;

            try
            {
                object manager = _npcManagerInstanceProp.GetValue(null);
                if (manager == null)
                    return false;
                npcs = _npcsDictField.GetValue(manager) as IDictionary;
                return npcs != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetSave(out object save, out object characterData)
        {
            save = null;
            characterData = null;
            EnsureResolved();
            if (_gameSaveInstanceProp == null || _currentSaveProp == null || _characterDataProp == null)
                return false;

            try
            {
                object gameSave = _gameSaveInstanceProp.GetValue(null);
                if (gameSave == null)
                    return false;
                save = _currentSaveProp.GetValue(gameSave);
                if (save == null)
                    return false;
                characterData = _characterDataProp.GetValue(save);
                return characterData != null;
            }
            catch
            {
                return false;
            }
        }

        private static string ReadString(object instance, PropertyInfo prop)
        {
            if (instance == null || prop == null)
                return null;
            try
            {
                return prop.GetValue(instance) as string;
            }
            catch
            {
                return null;
            }
        }

        private static bool ReadBool(object instance, PropertyInfo prop)
        {
            if (instance == null || prop == null)
                return false;
            try
            {
                object raw = prop.GetValue(instance);
                return raw is bool value && value;
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureResolved()
        {
            if (_resolved)
                return;
            _resolved = true;

            _npcManagerType = AccessTools.TypeByName("Wish.NPCManager");
            if (_npcManagerType != null)
            {
                _npcManagerInstanceProp = FindProperty(_npcManagerType, "Instance", "instance");
                _npcsDictField = AccessTools.Field(_npcManagerType, "_npcs");
            }

            _gameSaveType = AccessTools.TypeByName("Wish.GameSave");
            if (_gameSaveType != null)
            {
                _gameSaveInstanceProp = FindProperty(_gameSaveType, "Instance", "instance");
                Type saveType = AccessTools.TypeByName("Wish.SaveGame");
                if (saveType != null)
                {
                    _currentSaveProp = FindProperty(_gameSaveType, "CurrentSave", "currentSave");
                    _characterDataProp = FindProperty(saveType, "characterData", "CharacterData");
                    Type characterDataType = _characterDataProp?.PropertyType;
                    if (characterDataType != null)
                        _relationshipsProp = FindProperty(characterDataType, "Relationships", "relationships");
                }

                _getProgressBoolMethod = AccessTools.Method(_gameSaveType, "GetProgressBoolCharacter", new[] { typeof(string) });
                _getProgressStringMethod = AccessTools.Method(_gameSaveType, "GetProgressStringCharacter", new[] { typeof(string) });
            }

            Type npcAiType = AccessTools.TypeByName("Wish.NPCAI");
            if (npcAiType != null)
            {
                _gaveGiftForDayField = AccessTools.Field(npcAiType, "gaveGiftForDay");
                _originalNameProp = FindProperty(npcAiType, "OriginalName", "originalName");
                _localizedNameProp = FindProperty(npcAiType, "LocalizedActualNPCName", "localizedActualNPCName");
                _romanceableProp = FindProperty(npcAiType, "Romanceable", "romanceable");
            }
        }

        private static PropertyInfo FindProperty(Type type, params string[] names)
        {
            if (type == null || names == null)
                return null;

            for (int i = 0; i < names.Length; i++)
            {
                PropertyInfo prop = AccessTools.Property(type, names[i]);
                if (prop != null)
                    return prop;
            }

            return null;
        }
    }
}
