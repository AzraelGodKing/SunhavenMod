using BepInEx.Configuration;
using UnityEngine;

namespace GiftingAssistant.Config
{
    /// <summary>
    /// How gift reminders are tracked. Default is built-in roster only; optional Todo push is opt-in.
    /// </summary>
    public enum GiftReminderMode
    {
        RosterOnly = 0,
        PushToTodo = 1
    }

    internal sealed class GiftingAssistantConfig
    {
        public ConfigEntry<bool> Enabled { get; }
        public ConfigEntry<bool> CheckForUpdates { get; }
        public ConfigEntry<KeyCode> ToggleKey { get; }
        public ConfigEntry<bool> RequireCtrl { get; }
        public ConfigEntry<bool> ShowInventoryPossession { get; }
        public ConfigEntry<bool> AutoSave { get; }
        public ConfigEntry<float> AutoSaveInterval { get; }
        public ConfigEntry<float> UIScale { get; }
        public ConfigEntry<GiftReminderMode> ReminderMode { get; }
        public ConfigEntry<bool> UseAlmanacIntegration { get; }

        public GiftingAssistantConfig(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true, "Enable Gifting Assistant");
            CheckForUpdates = config.Bind("General", "CheckForUpdates", true, "Check for Gifting Assistant updates on startup via GitHub Pages.");
            ToggleKey = config.Bind("Hotkeys", "ToggleKey", KeyCode.G, "Key to toggle the Gifting Assistant window");
            RequireCtrl = config.Bind("Hotkeys", "RequireCtrl", true, "Require Ctrl to be held when pressing the toggle key");
            ShowInventoryPossession = config.Bind("Display", "ShowInventoryPossession", true, "Show how many of each gift item you currently carry in your bag");
            AutoSave = config.Bind("Saving", "AutoSave", true, "Automatically save the gift roster periodically");
            AutoSaveInterval = config.Bind("Saving", "AutoSaveInterval", 60f, "Auto-save interval in seconds");
            UIScale = config.Bind("Display", "UIScale", 1f,
                new ConfigDescription("Scale factor for the Gifting Assistant window (1.0 = default)",
                    new AcceptableValueRange<float>(0.5f, 2.5f)));

            bool legacyPushTodo = config.Bind(
                "Integrations",
                "UseTodoIntegration",
                false,
                "[Deprecated] Replaced by ReminderMode. If ReminderMode is missing, true migrates to PushToTodo on first load.").Value;

            ReminderMode = config.Bind(
                "Integrations",
                "ReminderMode",
                legacyPushTodo ? GiftReminderMode.PushToTodo : GiftReminderMode.PushToTodo,
                "RosterOnly = track gifts in this mod's daily roster (priorities, gifted-today flags). PushToTodo = same roster plus a +Todo button on each row to push reminders into Sun Haven Todo when that mod is installed (default; falls back to roster-only when Sun Haven Todo is not installed).");

            UseAlmanacIntegration = config.Bind("Integrations", "UseAlmanacIntegration", true,
                "When Haven's Almanac is installed, share gift roster progress (pending count, priorities) with its HUD, dashboard, and daily briefing. Off = Gifting Assistant keeps its data private.");
        }
    }
}
