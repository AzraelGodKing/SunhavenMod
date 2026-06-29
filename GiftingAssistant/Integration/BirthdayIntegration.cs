using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace GiftingAssistant.Integration
{
    /// <summary>
    /// Chainloader-gated, reflection-only bridge to A Squirrel's Birthday Reminder.
    /// Surfaces which rostered NPCs have a birthday today so the window can flag them.
    /// </summary>
    public sealed class BirthdayIntegration
    {
        private const string PluginGuid = "com.azraelgodking.squirrelsbirthdayreminder";

        public bool IsAvailable =>
            Chainloader.PluginInfos != null &&
            Chainloader.PluginInfos.ContainsKey(PluginGuid);

        /// <summary>
        /// Returns a case-insensitive set of NPC names that have a birthday today.
        /// Empty when the mod is not installed or no birthdays are present.
        /// </summary>
        public HashSet<string> GetTodaysBirthdayNames()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!IsAvailable)
                return names;

            try
            {
                if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out var info) || info?.Instance == null)
                    return names;

                var pluginType = info.Instance.GetType();
                var getManager = AccessTools.Method(pluginType, "GetManager", Type.EmptyTypes);
                var manager = getManager?.Invoke(null, null);
                if (manager == null)
                    return names;

                var mgrType = manager.GetType();
                var hasProp = AccessTools.Property(mgrType, "HasBirthdays");
                if (hasProp?.GetValue(manager) is bool has && !has)
                    return names;

                var listObj = AccessTools.Property(mgrType, "TodaysBirthdays")?.GetValue(manager);
                if (!(listObj is IList list))
                    return names;

                foreach (var item in list)
                {
                    if (item == null)
                        continue;
                    var npc = AccessTools.Property(item.GetType(), "NPCName")?.GetValue(item) as string;
                    if (!string.IsNullOrEmpty(npc))
                        names.Add(npc);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[GiftingAssistant] Birthday integration: {ex.Message}");
            }

            return names;
        }
    }
}
