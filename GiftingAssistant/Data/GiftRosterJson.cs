using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SunhavenMods.Shared;

namespace GiftingAssistant.Data
{
    internal static class GiftRosterJson
    {
        internal static string Serialize(GiftRosterData data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");

            sb.Append("  \"CharacterName\": ");
            MinimalJsonParser.WriteJsonString(sb, data.CharacterName);
            sb.AppendLine(",");

            sb.Append("  \"LastResetDateKey\": ");
            MinimalJsonParser.WriteJsonString(sb, data.LastResetDateKey ?? "");
            sb.AppendLine(",");

            sb.Append("  \"LastUpdated\": ");
            MinimalJsonParser.WriteJsonString(sb, data.LastUpdated.ToString("o"));
            sb.AppendLine(",");

            sb.AppendLine("  \"Entries\": [");

            for (int i = 0; i < data.Entries.Count; i++)
            {
                var entry = data.Entries[i];
                sb.AppendLine("    {");

                sb.Append("      \"NpcName\": ");
                MinimalJsonParser.WriteJsonString(sb, entry.NpcName ?? "");
                sb.AppendLine(",");

                sb.AppendLine($"      \"Priority\": {(int)entry.Priority},");
                sb.AppendLine($"      \"ManualGiftedToday\": {(entry.ManualGiftedToday ? "true" : "false")},");

                sb.Append("      \"PreferredGiftIds\": [");
                if (entry.PreferredGiftIds != null)
                {
                    for (int p = 0; p < entry.PreferredGiftIds.Count; p++)
                    {
                        sb.Append(entry.PreferredGiftIds[p].ToString(CultureInfo.InvariantCulture));
                        if (p < entry.PreferredGiftIds.Count - 1)
                            sb.Append(", ");
                    }
                }
                sb.AppendLine("]");

                sb.Append("    }");
                if (i < data.Entries.Count - 1)
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.Append("}");
            return sb.ToString();
        }

        internal static GiftRosterData Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            try
            {
                return DeserializeCore(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GiftRosterJson] Deserialize failed: {ex}");
                return null;
            }
        }

        private static GiftRosterData DeserializeCore(string json)
        {
            int pos = 0;
            var root = MinimalJsonParser.ParseObject(json, ref pos);
            if (root == null)
                return null;

            var data = new GiftRosterData();

            if (root.TryGetValue("CharacterName", out var charNameObj))
                data.CharacterName = charNameObj as string ?? "";

            if (root.TryGetValue("LastResetDateKey", out var resetObj))
                data.LastResetDateKey = resetObj as string ?? "";

            if (root.TryGetValue("LastUpdated", out var lastUpdObj) && lastUpdObj is string lastUpdStr)
                data.LastUpdated = DateTime.TryParse(lastUpdStr, null, DateTimeStyles.RoundtripKind, out var dt) ? dt : DateTime.Now;

            if (root.TryGetValue("Entries", out var entriesObj) && entriesObj is List<object> entriesList)
            {
                foreach (var entryObj in entriesList)
                {
                    if (!(entryObj is Dictionary<string, object> entryDict))
                        continue;

                    var entry = new GiftRosterEntry();

                    if (entryDict.TryGetValue("NpcName", out var nameVal))
                        entry.NpcName = nameVal as string ?? "";
                    if (entryDict.TryGetValue("Priority", out var prioVal))
                        entry.Priority = (GiftPriority)MinimalJsonParser.ToInt(prioVal);
                    if (entryDict.TryGetValue("ManualGiftedToday", out var giftedVal))
                        entry.ManualGiftedToday = giftedVal is bool b && b;
                    if (entryDict.TryGetValue("PreferredGiftIds", out var prefVal) && prefVal is List<object> prefList)
                    {
                        foreach (var idObj in prefList)
                        {
                            int id = MinimalJsonParser.ToInt(idObj);
                            if (id > 0 && !entry.PreferredGiftIds.Contains(id))
                                entry.PreferredGiftIds.Add(id);
                        }
                    }

                    if (!string.IsNullOrEmpty(entry.NpcName))
                        data.Entries.Add(entry);
                }
            }

            return data;
        }
    }
}
