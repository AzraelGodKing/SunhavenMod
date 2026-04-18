using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SunhavenTodo.Data
{
    internal static class TodoJson
    {
        internal static string Serialize(TodoListData data)
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
                sb.AppendLine(",");

                sb.AppendLine($"      \"IsRecurring\": {(item.IsRecurring ? "true" : "false")},");
                sb.AppendLine($"      \"RecurInterval\": {(int)item.RecurInterval}");

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

        internal static TodoListData Deserialize(string json)
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
                    if (itemDict.TryGetValue("IsRecurring", out var recurVal))
                        item.IsRecurring = recurVal is bool rb && rb;
                    if (itemDict.TryGetValue("RecurInterval", out var recurIntVal))
                        item.RecurInterval = (RecurInterval)ToInt(recurIntVal);

                    data.Items.Add(item);
                }
            }

            return data;
        }

        private static int ToInt(object val)
        {
            if (val is long l) return (int)l;
            if (val is double d) return (int)d;
            if (val is int i) return i;
            return 0;
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

        private static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length && char.IsWhiteSpace(json[pos]))
                pos++;
        }

        private static object ParseValue(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length) return null;
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
            if (pos >= json.Length || json[pos] != '{') return null;
            pos++;
            var dict = new Dictionary<string, object>();
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == '}') { pos++; return dict; }
            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                var key = ParseString(json, ref pos);
                if (key == null) break;
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != ':') break;
                pos++;
                SkipWhitespace(json, ref pos);
                dict[key] = ParseValue(json, ref pos);
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') { pos++; continue; }
                break;
            }
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == '}') pos++;
            return dict;
        }

        private static List<object> ParseArray(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '[') return null;
            pos++;
            var list = new List<object>();
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == ']') { pos++; return list; }
            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                list.Add(ParseValue(json, ref pos));
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') { pos++; continue; }
                break;
            }
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == ']') pos++;
            return list;
        }

        private static string ParseString(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '"') return null;
            pos++;
            var sb = new StringBuilder();
            while (pos < json.Length)
            {
                char c = json[pos];
                if (c == '\\' && pos + 1 < json.Length)
                {
                    pos++;
                    switch (json[pos])
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        default: sb.Append(json[pos]); break;
                    }
                    pos++;
                }
                else if (c == '"') { pos++; return sb.ToString(); }
                else { sb.Append(c); pos++; }
            }
            return sb.ToString();
        }

        private static object ParseNumber(string json, ref int pos)
        {
            int start = pos;
            bool isFloat = false;
            if (pos < json.Length && json[pos] == '-') pos++;
            while (pos < json.Length && char.IsDigit(json[pos])) pos++;
            if (pos < json.Length && json[pos] == '.') { isFloat = true; pos++; while (pos < json.Length && char.IsDigit(json[pos])) pos++; }
            if (pos < json.Length && (json[pos] == 'e' || json[pos] == 'E'))
            {
                isFloat = true; pos++;
                if (pos < json.Length && (json[pos] == '+' || json[pos] == '-')) pos++;
                while (pos < json.Length && char.IsDigit(json[pos])) pos++;
            }
            string numStr = json.Substring(start, pos - start);
            if (isFloat && double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
            if (!isFloat && long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return l;
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
    }
}
