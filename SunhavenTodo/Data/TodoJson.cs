using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SunhavenMods.Shared;

namespace SunhavenTodo.Data
{
    internal static class TodoJson
    {
        internal static string Serialize(TodoListData data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");

            sb.Append("  \"CharacterName\": ");
            MinimalJsonParser.WriteJsonString(sb, data.CharacterName);
            sb.AppendLine(",");

            sb.Append("  \"LastUpdated\": ");
            MinimalJsonParser.WriteJsonString(sb, data.LastUpdated.ToString("o"));
            sb.AppendLine(",");

            sb.AppendLine("  \"Items\": [");

            for (int i = 0; i < data.Items.Count; i++)
            {
                var item = data.Items[i];
                sb.AppendLine("    {");

                sb.Append("      \"Id\": ");
                MinimalJsonParser.WriteJsonString(sb, item.Id ?? "");
                sb.AppendLine(",");

                sb.Append("      \"Title\": ");
                MinimalJsonParser.WriteJsonString(sb, item.Title ?? "");
                sb.AppendLine(",");

                sb.Append("      \"Description\": ");
                MinimalJsonParser.WriteJsonString(sb, item.Description ?? "");
                sb.AppendLine(",");

                sb.AppendLine($"      \"IconItemId\": {item.IconItemId},");

                sb.Append("      \"MuseumDestination\": ");
                MinimalJsonParser.WriteJsonString(sb, item.MuseumDestination ?? "");
                sb.AppendLine(",");

                sb.AppendLine($"      \"Priority\": {(int)item.Priority},");
                sb.AppendLine($"      \"Category\": {(int)item.Category},");
                sb.AppendLine($"      \"IsCompleted\": {(item.IsCompleted ? "true" : "false")},");

                sb.Append("      \"CreatedAt\": ");
                MinimalJsonParser.WriteJsonString(sb, item.CreatedAt.ToString("o"));
                sb.AppendLine(",");

                sb.Append("      \"CompletedAt\": ");
                MinimalJsonParser.WriteJsonString(sb, item.CompletedAt?.ToString("o") ?? "");
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
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return DeserializeCore(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TodoJson] Deserialize failed: {ex}");
                return null;
            }
        }

        private static TodoListData DeserializeCore(string json)
        {
            int pos = 0;
            var root = MinimalJsonParser.ParseObject(json, ref pos);
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
                        item.IconItemId = MinimalJsonParser.ToInt(iconVal);
                    else
                        item.IconItemId = -1;
                    if (itemDict.TryGetValue("MuseumDestination", out var hallVal))
                        item.MuseumDestination = hallVal as string ?? "";
                    else
                        item.MuseumDestination = "";
                    if (itemDict.TryGetValue("Priority", out var prioVal))
                        item.Priority = (TodoPriority)MinimalJsonParser.ToInt(prioVal);
                    if (itemDict.TryGetValue("Category", out var catVal))
                        item.Category = (TodoCategory)MinimalJsonParser.ToInt(catVal);
                    if (itemDict.TryGetValue("IsCompleted", out var compVal))
                        item.IsCompleted = compVal is bool b && b;
                    if (itemDict.TryGetValue("CreatedAt", out var createdVal) && createdVal is string createdStr)
                        item.CreatedAt = DateTime.TryParse(createdStr, null, DateTimeStyles.RoundtripKind, out var cdt) ? cdt : DateTime.Now;
                    if (itemDict.TryGetValue("CompletedAt", out var compAtVal) && compAtVal is string compAtStr && !string.IsNullOrEmpty(compAtStr))
                        item.CompletedAt = DateTime.TryParse(compAtStr, null, DateTimeStyles.RoundtripKind, out var cat) ? cat : (DateTime?)null;
                    if (itemDict.TryGetValue("IsRecurring", out var recurVal))
                        item.IsRecurring = recurVal is bool rb && rb;
                    if (itemDict.TryGetValue("RecurInterval", out var recurIntVal))
                        item.RecurInterval = (RecurInterval)MinimalJsonParser.ToInt(recurIntVal);

                    data.Items.Add(item);
                }
            }

            return data;
        }
    }
}
