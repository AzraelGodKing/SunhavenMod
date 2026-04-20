using System;
using System.IO;
using BepInEx;

namespace SunhavenTodo.Data
{
    public class TodoSaveSystem
    {
        private readonly TodoManager _manager;
        private string _savePath;

        public TodoSaveSystem(TodoManager manager)
        {
            _manager = manager;
            _savePath = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_GUID);

            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }
        }

        private string GetSaveFilePath(string characterName)
        {
            var safeName = SanitizeFileName(characterName);
            return Path.Combine(_savePath, $"{safeName}_todos.json");
        }

        private string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "unknown";

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        public void Save()
        {
            var data = _manager.GetData();
            if (data == null || string.IsNullOrEmpty(data.CharacterName))
            {
                Plugin.Log?.LogWarning("Cannot save: No data or character name");
                return;
            }

            Plugin.Log?.LogInfo($"[Save] Saving {data.Items.Count} todo(s) for '{data.CharacterName}'");

            try
            {
                var json = SerializeToJson(data);
                var filePath = GetSaveFilePath(data.CharacterName);

                Plugin.Log?.LogDebug($"[Save] Writing to: {filePath} ({json.Length} chars)");

                // Write to temp file first (atomic operation)
                var tempFilePath = filePath + ".tmp";
                try
                {
                    File.WriteAllText(tempFilePath, json);

                    // Backup existing file before overwriting
                    if (File.Exists(filePath))
                    {
                        var backupPath = filePath + ".bak";
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);
                        File.Move(filePath, backupPath);
                    }

                    // Move temp file to final location
                    File.Move(tempFilePath, filePath);
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }

                _manager.MarkClean();
                Plugin.Log?.LogInfo($"[Save] Saved successfully: {filePath}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Save] Failed to save: {ex}");
            }
        }

        public TodoListData Load(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                Plugin.Log?.LogWarning("Cannot load: No character name");
                return null;
            }

            var filePath = GetSaveFilePath(characterName);
            var backupPath = filePath + ".bak";

            if (File.Exists(filePath))
            {
                var result = TryLoadFromFile(filePath, characterName);
                if (result != null)
                    return result;

                Plugin.Log?.LogWarning($"Main save file corrupted for {characterName}, trying backup...");
            }

            if (File.Exists(backupPath))
            {
                var result = TryLoadFromFile(backupPath, characterName);
                if (result != null)
                {
                    Plugin.Log?.LogInfo($"Loaded from backup for {characterName}");
                    return result;
                }
                Plugin.Log?.LogWarning($"Backup file also corrupted for {characterName}");
            }

            Plugin.Log?.LogInfo($"No valid save file found for {characterName}, creating new todo list");
            return new TodoListData(characterName);
        }

        private TodoListData TryLoadFromFile(string filePath, string characterName)
        {
            try
            {
                var json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
                {
                    Plugin.Log?.LogWarning($"File {filePath} does not contain valid JSON");
                    return null;
                }

                var data = DeserializeFromJson(json);
                if (data == null)
                {
                    Plugin.Log?.LogWarning($"Failed to deserialize {filePath}");
                    return null;
                }

                Plugin.Log?.LogInfo($"Loaded todo list for {characterName}: {data.Items.Count} item(s)");
                return data;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error loading {filePath}: {ex.Message}");
                return null;
            }
        }

        public void Delete(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
                return;

            var filePath = GetSaveFilePath(characterName);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    Plugin.Log?.LogInfo($"Deleted todo list for {characterName}");
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogError($"Failed to delete todo list: {ex.Message}");
                }
            }
        }

        #region JSON Serialization (manual — replaces unreliable JsonUtility)
        // Serialization logic lives in TodoJson.cs (no BepInEx dependency) so it
        // can be linked into the unit-test project without stubs.

        private static string SerializeToJson(TodoListData data) => TodoJson.Serialize(data);
        private static TodoListData DeserializeFromJson(string json) => TodoJson.Deserialize(json);


        #endregion
    }
}
