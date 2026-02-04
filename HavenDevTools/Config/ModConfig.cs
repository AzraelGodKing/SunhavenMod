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

        // Debug Window Settings
        public static ConfigEntry<bool> ShowItemIds { get; private set; }
        public static ConfigEntry<bool> ShowCurrencies { get; private set; }
        public static ConfigEntry<bool> ShowBundles { get; private set; }
        public static ConfigEntry<bool> ShowRaceBonuses { get; private set; }

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

            // Debug Window Settings
            ShowItemIds = config.Bind(
                "DebugWindow",
                "ShowItemIds",
                true,
                "Show item ID lookup section in debug window"
            );

            ShowCurrencies = config.Bind(
                "DebugWindow",
                "ShowCurrencies",
                true,
                "Show currency tracker section in debug window (requires The Vault)"
            );

            ShowBundles = config.Bind(
                "DebugWindow",
                "ShowBundles",
                true,
                "Show bundle inspector section in debug window (requires S.M.U.T.)"
            );

            ShowRaceBonuses = config.Bind(
                "DebugWindow",
                "ShowRaceBonuses",
                true,
                "Show race modifier tracker section in debug window (requires Haven's Birthright)"
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
