using System;
using BepInEx.Configuration;
using System.Reflection;
using UnityEngine;
using SunhavenMods.Shared;

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

        /// <summary>
        /// The Vault: full inspector (zeros in tabs, Debug dump tab, full HUD). Pushed to TheVault.Plugin at runtime.
        /// </summary>
        public static ConfigEntry<bool> TheVaultFullVaultInspector { get; private set; }

        private static bool _theVaultInspectorHooked;
        private static Type _cachedTheVaultPluginType;
        private static MethodInfo _cachedSetTheVaultFullInspector;

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

            TheVaultFullVaultInspector = config.Bind(
                "The Vault",
                "FullVaultInspector",
                false,
                "When true: The Vault lists every defined currency (including 0), adds the Debug tab with a raw vault dump, and the HUD shows all slots. Same as the former [Debug] FullVaultInspector in TheVault.cfg (moved here)."
            );

            if (!_theVaultInspectorHooked)
            {
                TheVaultFullVaultInspector.SettingChanged += (_, __) => SyncTheVaultFullVaultInspectorToPlugin();
                _theVaultInspectorHooked = true;
            }

            global::HavenDevTools.Plugin.Log?.LogInfo("Configuration initialized");
        }

        /// <summary>
        /// Push <see cref="TheVaultFullVaultInspector"/> to The Vault plugin (reflection; no compile reference).
        /// Call after mod detection so <see cref="Plugin.HasTheVault"/> is accurate.
        /// </summary>
        public static void SyncTheVaultFullVaultInspectorToPlugin()
        {
            try
            {
                if (!global::HavenDevTools.Plugin.HasTheVault || TheVaultFullVaultInspector == null) return;

                _cachedTheVaultPluginType ??= ReflectionHelper.FindType("Plugin", "TheVault");
                if (_cachedTheVaultPluginType == null) return;

                _cachedSetTheVaultFullInspector ??= _cachedTheVaultPluginType.GetMethod(
                    "SetConfigDebugFullVaultInspector",
                    BindingFlags.Public | BindingFlags.Static);
                if (_cachedSetTheVaultFullInspector == null) return;

                _cachedSetTheVaultFullInspector.Invoke(null, new object[] { TheVaultFullVaultInspector.Value });
            }
            catch (Exception ex)
            {
                global::HavenDevTools.Plugin.Log?.LogDebug($"[ModConfig] SyncTheVaultFullVaultInspectorToPlugin: {ex.Message}");
            }
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
