using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace CropOptimizer.Integration
{
    internal sealed class BirthdayIntegration
    {
        private const string PluginGuid = "com.azraelgodking.squirrelsbirthdayreminder";

        public bool IsAvailable =>
            Chainloader.PluginInfos != null &&
            Chainloader.PluginInfos.ContainsKey(PluginGuid);

        public IReadOnlyList<string> GetSuggestedReserveProduce()
        {
            if (!IsAvailable)
                return Array.Empty<string>();

            try
            {
                if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out var info) || info?.Instance == null)
                    return Array.Empty<string>();

                var pluginType = info.Instance.GetType();
                var getManager = AccessTools.Method(pluginType, "GetManager", Type.EmptyTypes);
                if (getManager == null)
                    return Array.Empty<string>();

                var manager = getManager.Invoke(null, null);
                if (manager == null)
                    return Array.Empty<string>();

                var mgrType = manager.GetType();
                var hasProp = AccessTools.Property(mgrType, "HasBirthdays");
                if (hasProp?.GetValue(manager) is bool has && !has)
                    return Array.Empty<string>();

                var listProp = AccessTools.Property(mgrType, "TodaysBirthdays");
                var listObj = listProp?.GetValue(manager);
                if (!(listObj is IList list) || list.Count == 0)
                    return Array.Empty<string>();

                var suggestions = new List<string>();
                foreach (var item in list)
                {
                    if (item == null)
                        continue;
                    var t = item.GetType();
                    var npc = AccessTools.Property(t, "NPCName")?.GetValue(item) as string ?? "?";
                    var giftsObj = AccessTools.Property(t, "AllLovedGifts")?.GetValue(item);
                    if (giftsObj is IList gifts && gifts.Count > 0)
                        suggestions.Add($"{npc}: {gifts[0]}");
                }

                return suggestions;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[CropOptimizer] Birthday integration: {ex.Message}");
                return Array.Empty<string>();
            }
        }
    }
}
