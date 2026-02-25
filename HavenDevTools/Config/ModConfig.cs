using BepInEx.Configuration;
using UnityEngine;

namespace HavenDevTools.Config
{
    public static class ModConfig
    {
        // Hotkeys
        public static ConfigEntry<KeyCode> ToggleKey { get; private set; }
        public static ConfigEntry<KeyCode> OverlayToggleKey { get; private set; }

        // Overlay Settings
        public static ConfigEntry<bool> ShowOverlayOnStart { get; private set; }
        public static ConfigEntry<string> OverlayPosition { get; private set; }
        public static ConfigEntry<bool> ShowPerformance { get; private set; }

        // Log Viewer
        public static ConfigEntry<int> MaxLogEntries { get; private set; }
        public static ConfigEntry<string> LogLevelFilter { get; private set; }

        // Updates
        public static ConfigEntry<bool> CheckForUpdates { get; private set; }

        public static void Initialize(ConfigFile config)
        {
            // Hotkeys
            ToggleKey = config.Bind(
                "Hotkeys",
                "ToggleKey",
                KeyCode.F11,
                "Key to toggle the debug window (requires authorization)"
            );

            OverlayToggleKey = config.Bind(
                "Hotkeys",
                "OverlayToggleKey",
                KeyCode.F6,
                "Key to toggle the debug overlay"
            );

            // Overlay Settings
            ShowOverlayOnStart = config.Bind(
                "Overlay",
                "ShowOnStart",
                false,
                "Show the debug overlay when the game starts"
            );

            OverlayPosition = config.Bind(
                "Overlay",
                "Position",
                "TopRight",
                "Overlay position: TopLeft, TopRight, BottomLeft, BottomRight"
            );

            ShowPerformance = config.Bind(
                "Overlay",
                "ShowPerformance",
                true,
                "Show FPS and memory usage in the debug overlay"
            );

            // Log Viewer
            MaxLogEntries = config.Bind(
                "LogViewer",
                "MaxLogEntries",
                500,
                "Maximum number of log entries to keep in the in-game log viewer"
            );

            LogLevelFilter = config.Bind(
                "LogViewer",
                "LogLevelFilter",
                "Info",
                "Minimum log level to display: Debug, Info, Warning, Error"
            );

            // Updates
            CheckForUpdates = config.Bind(
                "Updates",
                "CheckForUpdates",
                true,
                "Check for mod updates on startup"
            );

            Plugin.Log?.LogInfo("Configuration initialized");
        }

        public static OverlayPositionType GetOverlayPosition()
        {
            return OverlayPosition.Value?.ToLower() switch
            {
                "topleft" => OverlayPositionType.TopLeft,
                "topright" => OverlayPositionType.TopRight,
                "bottomleft" => OverlayPositionType.BottomLeft,
                "bottomright" => OverlayPositionType.BottomRight,
                _ => OverlayPositionType.TopRight
            };
        }
    }

    public enum OverlayPositionType
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}
