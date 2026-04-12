using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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

        private static string SerializeToJson(TodoListData data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");

            sb.Append("  \"CharacterName\": ");
            WriteJsonString(sb, data.CharacterName);
            sb.AppendLine(",");

            sb.Append("  \"LastUpdated\": ");
            WriteJsonString(sb, data.LastUpdated.ToString("o"));
            sb.AppendLine(",");

            sb.AppendLine("  \"Items\": [");

            for (int i = 0; i < data.Items.Count; i++)
            {
                var item = data.Items[i];
                sb.AppendLine("    {");

                sb.Append("      \"Id\": ");
                WriteJsonString(sb, item.Id ?? "");
                sb.AppendLine(",");

                sb.Append("      \"Title\": ");
                WriteJsonString(sb, item.Title ?? "");
                sb.AppendLine(",");

                sb.Append("      \"Description\": ");
                WriteJsonString(sb, item.Description ?? "");
                sb.AppendLine(",");

                sb.AppendLine($"      \"IconItemId\": {item.IconItemId},");

                sb.Append("      \"MuseumDestination\": ");
                WriteJsonString(sb, item.MuseumDestination ?? "");
                sb.AppendLine(",");

                sb.AppendLine($"      \"Priority\": {(int)item.Priority},");
                sb.AppendLine($"      \"Category\": {(int)item.Category},");
                sb.AppendLine($"      \"IsCompleted\": {(item.IsCompleted ? "true" : "false")},");

                sb.Append("      \"CreatedAt\": ");
                WriteJsonString(sb, item.CreatedAt.ToString("o"));
                sb.AppendLine(",");

                sb.Append("      \"CompletedAt\": ");
                WriteJsonString(sb, item.CompletedAt?.ToString("o") ?? "");
                sb.AppendLine();

                sb.Append("    }");
                if (i < data.Items.Count - 1)
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.Append("}");
            return sb.ToString();
        }

        private static void WriteJsonString(StringBuilder sb, string value)
        {
            sb.Append('"');
            if (value != null)
            {
                foreach (char c in value)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        default: sb.Append(c); break;
                    }
                }
            }
            sb.Append('"');
        }

        #endregion

        #region JSON Deserialization (minimal recursive descent parser)

        private static TodoListData DeserializeFromJson(string json)
        {
            try
            {
                int pos = 0;
                var root = ParseObject(json, ref pos);
                if (root == null)
                    return null;

                var data = new TodoListData();

                if (root.TryGetValue("CharacterName", out var charNameObj))
                    data.CharacterName = charNameObj as string ?? "";

                if (root.TryGetValue("LastUpdated", out var lastUpdObj) && lastUpdObj is string lastUpdStr)
                    data.LastUpdated = DateTime.TryParse(lastUpdStr, null, DateTimeStyles.RoundtripKind, out var dt) ? dt : DateTime.Now;

                if (root.TryGetValue("Items", out var itemsObj) && itemsObj is List<object> itemsList)
                {
                    foreach (var itemObj in itemsList)
                    {
                        if (!(itemObj is Dictionary<string, object> itemDict))
                            continue;

                        var item = new TodoItem();

                        if (itemDict.TryGetValue("Id", out var idVal))
                            item.Id = idVal as string ?? Guid.NewGuid().ToString();
                        if (itemDict.TryGetValue("Title", out var titleVal))
                            item.Title = titleVal as string ?? "";
                        if (itemDict.TryGetValue("Description", out var descVal))
                            item.Description = descVal as string ?? "";
                        if (itemDict.TryGetValue("IconItemId", out var iconVal))
                            item.IconItemId = ToInt(iconVal);
                        else
                            item.IconItemId = -1;
                        if (itemDict.TryGetValue("MuseumDestination", out var hallVal))
                            item.MuseumDestination = hallVal as string ?? "";
                        else
                            item.MuseumDestination = "";
                        if (itemDict.TryGetValue("Priority", out var prioVal))
                            item.Priority = (TodoPriority)ToInt(prioVal);
                        if (itemDict.TryGetValue("Category", out var catVal))
                            item.Category = (TodoCategory)ToInt(catVal);
                        if (itemDict.TryGetValue("IsCompleted", out var compVal))
                            item.IsCompleted = compVal is bool b && b;
                        if (itemDict.TryGetValue("CreatedAt", out var createdVal) && createdVal is string createdStr)
                            item.CreatedAt = DateTime.TryParse(createdStr, null, DateTimeStyles.RoundtripKind, out var cdt) ? cdt : DateTime.Now;
                        if (itemDict.TryGetValue("CompletedAt", out var compAtVal) && compAtVal is string compAtStr && !string.IsNullOrEmpty(compAtStr))
                            item.CompletedAt = DateTime.TryParse(compAtStr, null, DateTimeStyles.RoundtripKind, out var cat) ? cat : (DateTime?)null;

                        data.Items.Add(item);
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[JSON] Parse error: {ex.Message}");
                return null;
            }
        }

        private static int ToInt(object val)
        {
            if (val is long l) return (int)l;
            if (val is double d) return (int)d;
            if (val is int i) return i;
            return 0;
        }

        private static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length && char.IsWhiteSpace(json[pos]))
                pos++;
        }

        private static object ParseValue(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length)
                return null;

            char c = json[pos];

            if (c == '"') return ParseString(json, ref pos);
            if (c == '{') return ParseObject(json, ref pos);
            if (c == '[') return ParseArray(json, ref pos);
            if (c == 't') return ParseLiteral(json, ref pos, "true", true);
            if (c == 'f') return ParseLiteral(json, ref pos, "false", false);
            if (c == 'n') return ParseLiteral(json, ref pos, "null", null);
            if (c == '-' || char.IsDigit(c)) return ParseNumber(json, ref pos);

            return null;
        }

        private static Dictionary<string, object> ParseObject(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '{')
                return null;
            pos++;

            var dict = new Dictionary<string, object>();

            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == '}')
            {
                pos++;
                return dict;
            }

            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                var key = ParseString(json, ref pos);
                if (key == null)
                    break;

                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != ':')
                    break;
                pos++;

                SkipWhitespace(json, ref pos);
                var value = ParseValue(json, ref pos);
                dict[key] = value;

                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',')
                {
                    pos++;
                    continue;
                }
                break;
            }

            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == '}')
                pos++;

            return dict;
        }

        private static List<object> ParseArray(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '[')
                return null;
            pos++;

            var list = new List<object>();

            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == ']')
            {
                pos++;
                return list;
            }

            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                var value = ParseValue(json, ref pos);
                list.Add(value);

                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',')
                {
                    pos++;
                    continue;
                }
                break;
            }

            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == ']')
                pos++;

            return list;
        }

        private static string ParseString(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '"')
                return null;
            pos++;

            var sb = new StringBuilder();
            while (pos < json.Length)
            {
                char c = json[pos];
                if (c == '\\' && pos + 1 < json.Length)
                {
                    pos++;
                    char esc = json[pos];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        default: sb.Append(esc); break;
                    }
                    pos++;
                }
                else if (c == '"')
                {
                    pos++;
                    return sb.ToString();
                }
                else
                {
                    sb.Append(c);
                    pos++;
                }
            }

            return sb.ToString();
        }

        private static object ParseNumber(string json, ref int pos)
        {
            int start = pos;
            bool isFloat = false;

            if (pos < json.Length && json[pos] == '-')
                pos++;

            while (pos < json.Length && char.IsDigit(json[pos]))
                pos++;

            if (pos < json.Length && json[pos] == '.')
            {
                isFloat = true;
                pos++;
                while (pos < json.Length && char.IsDigit(json[pos]))
                    pos++;
            }

            if (pos < json.Length && (json[pos] == 'e' || json[pos] == 'E'))
            {
                isFloat = true;
                pos++;
                if (pos < json.Length && (json[pos] == '+' || json[pos] == '-'))
                    pos++;
                while (pos < json.Length && char.IsDigit(json[pos]))
                    pos++;
            }

            string numStr = json.Substring(start, pos - start);

            if (isFloat)
            {
                if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    return d;
            }
            else
            {
                if (long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                    return l;
            }

            return 0L;
        }

        private static object ParseLiteral(string json, ref int pos, string literal, object result)
        {
            if (pos + literal.Length <= json.Length && json.Substring(pos, literal.Length) == literal)
            {
                pos += literal.Length;
                return result;
            }
            pos++;
            return null;
        }

        #endregion
    }
}
