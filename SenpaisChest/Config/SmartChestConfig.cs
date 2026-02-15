using BepInEx.Configuration;
using UnityEngine;

namespace SenpaisChest.Config
{
    public class SmartChestConfig
    {
        public ConfigEntry<float> ScanInterval { get; private set; }
        public ConfigEntry<bool> EnableNotifications { get; private set; }
        public ConfigEntry<int> MaxItemsPerScan { get; private set; }
        public ConfigEntry<KeyCode> ToggleKey { get; private set; }
        public ConfigEntry<bool> RequireCtrlModifier { get; private set; }
        public ConfigEntry<bool> CheckForUpdates { get; private set; }

        // Static copies for PersistentRunner access
        internal static KeyCode StaticToggleKey = KeyCode.F9;
        internal static bool StaticRequireCtrl = false;

        public void Initialize(ConfigFile config)
        {
            ScanInterval = config.Bind(
                "General",
                "ScanInterval",
                60f,
                "Seconds between automatic item scans (min: 10)"
            );

            EnableNotifications = config.Bind(
                "General",
                "EnableNotifications",
                true,
                "Show notifications when items are moved by Smart Chests"
            );

            MaxItemsPerScan = config.Bind(
                "General",
                "MaxItemsPerScan",
                50,
                "Maximum item stacks to move per scan cycle (prevents lag)"
            );

            ToggleKey = config.Bind(
                "UI",
                "ToggleKey",
                KeyCode.F9,
                "Key to open Smart Chest configuration UI while interacting with a chest"
            );

            RequireCtrlModifier = config.Bind(
                "UI",
                "RequireCtrlModifier",
                false,
                "Require Ctrl key to be held when pressing the toggle key"
            );

            CheckForUpdates = config.Bind(
                "Updates",
                "CheckForUpdates",
                true,
                "Check for mod updates on startup"
            );

            // Initialize static values
            StaticToggleKey = ToggleKey.Value;
            StaticRequireCtrl = RequireCtrlModifier.Value;

            // Subscribe to config changes
            ToggleKey.SettingChanged += (_, _) => StaticToggleKey = ToggleKey.Value;
            RequireCtrlModifier.SettingChanged += (_, _) => StaticRequireCtrl = RequireCtrlModifier.Value;
        }

        public float GetScanInterval()
        {
            return Mathf.Max(10f, ScanInterval.Value);
        }
    }
}