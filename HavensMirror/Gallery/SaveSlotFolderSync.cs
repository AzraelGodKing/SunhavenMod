using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using HavensMirror.Config;
using SunhavenMods.Shared;

namespace HavensMirror.Gallery
{
    /// <summary>
    /// Creates <c>gallery/&lt;CharacterName&gt;/</c> folders from <c>Wish.GameSave</c> save slots
    /// so players do not invent folder names by hand. Names match dialogue portrait lookup
    /// (<c>characterData.characterName</c>).
    /// </summary>
    public static class SaveSlotFolderSync
    {
        private static int _lastKnownSlotCount = -1;
        private static string _lastSignature = string.Empty;

        /// <summary>
        /// Scan loaded save slots and ensure a gallery folder exists for each character name.
        /// Returns how many folders were newly created this call.
        /// </summary>
        public static int EnsureCharacterFolders()
        {
            if (MirrorOptions.CreateStarterFolders != null && !MirrorOptions.CreateStarterFolders.Value)
                return 0;

            var names = CollectCharacterNamesFromSaves();
            if (names.Count == 0)
                return 0;

            CharacterSaveStore.EnsureDirectory(GalleryPaths.GalleryRoot);

            int created = 0;
            foreach (string name in names)
            {
                string folder = GalleryPaths.CharacterFolder(name);
                if (Directory.Exists(folder))
                    continue;

                try
                {
                    Directory.CreateDirectory(folder);
                    created++;
                    Plugin.Log?.LogInfo($"[Gallery] Created character folder '{folder}'.");
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"[Gallery] Could not create folder for '{name}': {ex.Message}");
                }
            }

            string signature = string.Join("|", names);
            if (created > 0 || names.Count != _lastKnownSlotCount || signature != _lastSignature)
            {
                _lastKnownSlotCount = names.Count;
                _lastSignature = signature;
                if (created == 0 && names.Count > 0)
                    Plugin.Log?.LogDebug($"[Gallery] Save-slot sync: {names.Count} character folder(s) present.");
            }

            return created;
        }

        /// <summary>
        /// Enumerate display names from <c>GameSave.Instance.Saves</c> (same source The Vault uses for slot names).
        /// </summary>
        public static List<string> CollectCharacterNamesFromSaves()
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                Type gameSaveType = ReflectionHelper.FindWishType("GameSave");
                if (gameSaveType == null)
                    return names;

                object instance = ReflectionHelper.GetSingletonInstance(gameSaveType)
                                  ?? AccessTools.Property(gameSaveType, "Instance")?.GetValue(null);
                if (instance == null)
                    return names;

                object savesObj = AccessTools.Property(instance.GetType(), "Saves")?.GetValue(instance, null)
                                  ?? AccessTools.Field(instance.GetType(), "Saves")?.GetValue(instance)
                                  ?? AccessTools.Property(gameSaveType, "Saves")?.GetValue(null);

                if (!(savesObj is IEnumerable saves))
                    return names;

                foreach (object saveData in saves)
                {
                    if (saveData == null)
                        continue;

                    string name = ReadCharacterNameFromSaveData(saveData);
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    name = name.Trim();
                    if (!seen.Add(name))
                        continue;

                    names.Add(name);
                }

                // Also ensure the active character is covered (CurrentCharacter / CurrentSave).
                string current = GameSaveCharacterName.TryGetCurrent();
                if (!string.IsNullOrWhiteSpace(current) && seen.Add(current.Trim()))
                    names.Add(current.Trim());
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[Gallery] Save-slot scan failed: {ex.Message}");
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        private static string ReadCharacterNameFromSaveData(object saveData)
        {
            if (saveData == null)
                return null;

            // Preferred: characterData.characterName (matches dialogue / GameSaveCharacterName).
            object characterData = AccessTools.Property(saveData.GetType(), "characterData")?.GetValue(saveData, null)
                                   ?? AccessTools.Field(saveData.GetType(), "characterData")?.GetValue(saveData);
            if (characterData != null)
            {
                object nameObj = AccessTools.Property(characterData.GetType(), "characterName")?.GetValue(characterData, null)
                                 ?? AccessTools.Field(characterData.GetType(), "characterName")?.GetValue(characterData);
                string fromData = nameObj as string;
                if (!string.IsNullOrWhiteSpace(fromData))
                    return fromData;
            }

            // Direct characterName on the save entry (some game builds).
            object direct = AccessTools.Property(saveData.GetType(), "characterName")?.GetValue(saveData, null)
                            ?? AccessTools.Field(saveData.GetType(), "characterName")?.GetValue(saveData);
            if (direct is string directName && !string.IsNullOrWhiteSpace(directName))
                return directName;

            // Last resort: fileName without .save (may not match display name — only if nothing else).
            object fileNameObj = AccessTools.Property(saveData.GetType(), "fileName")?.GetValue(saveData, null)
                                 ?? AccessTools.Field(saveData.GetType(), "fileName")?.GetValue(saveData);
            if (fileNameObj is string fileName && !string.IsNullOrWhiteSpace(fileName))
            {
                string fromFile = fileName.Trim();
                if (fromFile.EndsWith(".save", StringComparison.OrdinalIgnoreCase))
                    fromFile = fromFile.Substring(0, fromFile.Length - 5);
                if (!string.IsNullOrWhiteSpace(fromFile))
                    return fromFile;
            }

            return null;
        }
    }
}
