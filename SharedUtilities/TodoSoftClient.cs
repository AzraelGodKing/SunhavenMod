using System;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Shared soft-dependency client for Sun Haven Todo (AZR-255).
    /// One reflection surface instead of four diverged copies.
    /// </summary>
    public static class TodoSoftClient
    {
        public const string PluginGuid = "com.azraelgodking.sunhaventodo";

        public static bool IsAvailable =>
            Chainloader.PluginInfos != null &&
            Chainloader.PluginInfos.ContainsKey(PluginGuid);

        /// <summary>
        /// Adds a todo when Todo is installed. Returns false if unavailable or the call failed.
        /// </summary>
        public static bool TryAddTodo(
            string title,
            string description,
            string priorityName = "Normal",
            string categoryName = "General",
            ManualLogSource log = null)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(title))
                return false;

            try
            {
                if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out var info) || info?.Instance == null)
                    return false;

                var pluginType = info.Instance.GetType();
                var getManager = AccessTools.Method(pluginType, "GetTodoManager", Type.EmptyTypes);
                if (getManager == null)
                    return false;

                var manager = getManager.Invoke(null, null);
                if (manager == null)
                    return false;

                var asm = pluginType.Assembly;
                var prioType = asm.GetType("SunhavenTodo.Data.TodoPriority");
                var catType = asm.GetType("SunhavenTodo.Data.TodoCategory");
                if (prioType == null || catType == null)
                    return false;

                var addTodo = AccessTools.Method(manager.GetType(), "AddTodo", new[]
                {
                    typeof(string), typeof(string), prioType, catType
                });
                if (addTodo == null)
                    return false;

                object priority = Enum.Parse(prioType, priorityName);
                object category = Enum.Parse(catType, categoryName);
                addTodo.Invoke(manager, new[] { title, description ?? string.Empty, priority, category });
                return true;
            }
            catch (Exception ex)
            {
                log?.LogDebug($"[TodoSoftClient] AddTodo failed: {ex.Message}");
                return false;
            }
        }
    }
}
