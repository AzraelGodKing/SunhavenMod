using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SenpaisChest.Data
{
    internal static class SmartChestJson
    {
        internal static string Serialize(SmartChestSaveData data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.Append("  \"CharacterName\": ");
            WriteJsonString(sb, data.CharacterName);
            sb.AppendLine(",");
            sb.AppendLine("  \"Chests\": [");

            for (int i = 0; i < data.Chests.Count; i++)
            {
                var chest = data.Chests[i];
                sb.AppendLine("    {");

                sb.Append("      \"ChestId\": ");
                WriteJsonString(sb, chest.ChestId);
                sb.AppendLine(",");

                sb.Append("      \"ChestName\": ");
                WriteJsonString(sb, chest.ChestName);
                sb.AppendLine(",");

                sb.AppendLine($"      \"IsEnabled\": {(chest.IsEnabled ? "true" : "false")},");
                sb.AppendLine("      \"Rules\": [");

                for (int j = 0; j < chest.Rules.Count; j++)
                {
                    var rule = chest.Rules[j];
                    sb.AppendLine("        {");
                    sb.AppendLine($"          \"Type\": {(int)rule.Type},");
                    sb.AppendLine($"          \"ItemId\": {rule.ItemId},");

                    sb.Append("          \"CategoryName\": ");
                    WriteJsonString(sb, rule.CategoryName ?? "");
                    sb.AppendLine(",");

                    sb.Append("          \"ItemTypeName\": ");
                    WriteJsonString(sb, rule.ItemTypeName ?? "");
                    sb.AppendLine(",");

                    sb.Append("          \"PropertyName\": ");
                    WriteJsonString(sb, rule.PropertyName ?? "");
                    sb.AppendLine(",");

                    sb.Append("          \"GroupName\": ");
                    WriteJsonString(sb, rule.GroupName ?? "");
                    sb.AppendLine();

                    sb.Append("        }");
                    if (j < chest.Rules.Count - 1)
                        sb.AppendLine(",");
                    else
                        sb.AppendLine();
                }

                sb.AppendLine("      ]");
                sb.Append("    }");
                if (i < data.Chests.Count - 1)
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
            }

            sb.AppendLine("  ],");
            sb.AppendLine("  \"Groups\": [");
            var groups = data.Groups ?? new List<ItemGroup>();
            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                sb.AppendLine("    {");
                sb.Append("      \"Name\": ");
                WriteJsonString(sb, g.Name ?? "");
                sb.AppendLine(",");
                sb.AppendLine("      \"ItemIds\": [");
                var ids = g.ItemIds ?? new List<int>();
                for (int j = 0; j < ids.Count; j++)
                {
                    sb.Append("        ");
                    sb.Append(ids[j]);
                    if (j < ids.Count - 1) sb.AppendLine(",");
                    else sb.AppendLine();
                }
                sb.AppendLine("      ]");
                sb.Append("    }");
                if (i < groups.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.Append("}");
            return sb.ToString();
        }

        internal static SmartChestSaveData Deserialize(string json)
        {
            int pos = 0;
            var root = ParseObject(json, ref pos);
            if (root == null)
                return null;

            var data = new SmartChestSaveData();

            if (root.TryGetValue("CharacterName", out var charNameObj))
                data.CharacterName = charNameObj as string ?? "";

            if (root.TryGetValue("Chests", out var chestsObj) && chestsObj is List<object> chestsList)
            {
                foreach (var chestObj in chestsList)
                {
                    if (!(chestObj is Dictionary<string, object> chestDict))
                        continue;

                    var chest = new SmartChestData();

                    if (chestDict.TryGetValue("ChestId", out var cid))
                        chest.ChestId = cid as string ?? "";
                    if (chestDict.TryGetValue("ChestName", out var cname))
                        chest.ChestName = cname as string ?? "";
                    if (chestDict.TryGetValue("IsEnabled", out var cenabled))
                        chest.IsEnabled = cenabled is bool b ? b : false;

                    if (chestDict.TryGetValue("Rules", out var rulesObj) && rulesObj is List<object> rulesList)
                    {
                        foreach (var ruleObj in rulesList)
                        {
                            if (!(ruleObj is Dictionary<string, object> ruleDict))
                                continue;

                            var rule = new SmartChestRule();

                            if (ruleDict.TryGetValue("Type", out var typeVal))
                                rule.Type = (RuleType)ToInt(typeVal);
                            if (ruleDict.TryGetValue("ItemId", out var itemIdVal))
                                rule.ItemId = ToInt(itemIdVal);
                            if (ruleDict.TryGetValue("CategoryName", out var catVal))
                                rule.CategoryName = catVal as string ?? "";
                            if (ruleDict.TryGetValue("ItemTypeName", out var typeNameVal))
                                rule.ItemTypeName = typeNameVal as string ?? "";
                            if (ruleDict.TryGetValue("PropertyName", out var propVal))
                                rule.PropertyName = propVal as string ?? "";
                            if (ruleDict.TryGetValue("GroupName", out var grpVal))
                                rule.GroupName = grpVal as string ?? "";

                            chest.Rules.Add(rule);
                        }
                    }

                    data.Chests.Add(chest);
                }
            }

            if (root.TryGetValue("Groups", out var groupsObj) && groupsObj is List<object> groupsList)
            {
                foreach (var groupObj in groupsList)
                {
                    if (!(groupObj is Dictionary<string, object> groupDict))
                        continue;
                    var g = new ItemGroup();
                    if (groupDict.TryGetValue("Name", out var gname))
                        g.Name = gname as string ?? "";
                    if (groupDict.TryGetValue("ItemIds", out var idsObj) && idsObj is List<object> idsList)
                    {
                        foreach (var idObj in idsList)
                            g.ItemIds.Add(ToInt(idObj));
                    }
                    if (!string.IsNullOrWhiteSpace(g.Name))
                        data.Groups.Add(g);
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
