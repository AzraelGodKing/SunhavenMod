using System;
using System.Collections.Generic;
using System.Text;
using SunhavenMods.Shared;

namespace SenpaisChest.Data
{
    internal static class SmartChestJson
    {
        internal static string Serialize(SmartChestSaveData data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.Append("  \"CharacterName\": ");
            MinimalJsonParser.WriteJsonString(sb, data.CharacterName);
            sb.AppendLine(",");
            sb.AppendLine("  \"Chests\": [");

            for (int i = 0; i < data.Chests.Count; i++)
            {
                var chest = data.Chests[i];
                sb.AppendLine("    {");

                sb.Append("      \"ChestId\": ");
                MinimalJsonParser.WriteJsonString(sb, chest.ChestId);
                sb.AppendLine(",");

                sb.Append("      \"ChestName\": ");
                MinimalJsonParser.WriteJsonString(sb, chest.ChestName);
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
                    MinimalJsonParser.WriteJsonString(sb, rule.CategoryName ?? "");
                    sb.AppendLine(",");

                    sb.Append("          \"ItemTypeName\": ");
                    MinimalJsonParser.WriteJsonString(sb, rule.ItemTypeName ?? "");
                    sb.AppendLine(",");

                    sb.Append("          \"PropertyName\": ");
                    MinimalJsonParser.WriteJsonString(sb, rule.PropertyName ?? "");
                    sb.AppendLine(",");

                    sb.Append("          \"GroupName\": ");
                    MinimalJsonParser.WriteJsonString(sb, rule.GroupName ?? "");
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
                MinimalJsonParser.WriteJsonString(sb, g.Name ?? "");
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
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return DeserializeCore(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SmartChestJson] Deserialize failed: {ex}");
                return null;
            }
        }

        private static SmartChestSaveData DeserializeCore(string json)
        {
            int pos = 0;
            var root = MinimalJsonParser.ParseObject(json, ref pos);
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
                                rule.Type = (RuleType)MinimalJsonParser.ToInt(typeVal);
                            if (ruleDict.TryGetValue("ItemId", out var itemIdVal))
                                rule.ItemId = MinimalJsonParser.ToInt(itemIdVal);
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
                            g.ItemIds.Add(MinimalJsonParser.ToInt(idObj));
                    }
                    if (!string.IsNullOrWhiteSpace(g.Name))
                        data.Groups.Add(g);
                }
            }

            return data;
        }
    }
}
