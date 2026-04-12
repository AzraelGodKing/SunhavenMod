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
        public ConfigEntry<bool> AutoEnableSmartChestOnRuleAdd { get; private set; }
        public ConfigEntry<bool> CheckForUpdates { get; private set; }

        // Chest Labels (integrated)
        public ConfigEntry<bool> EnableChestLabels { get; private set; }
        public ConfigEntry<ChestLabelVisibility> LabelVisibility { get; private set; }
        public ConfigEntry<ChestLabelVisibility> IconVisibility { get; private set; }
        public ConfigEntry<float> UIScale { get; private set; }
        public ConfigEntry<bool> BlockInputWhenTypingInConfig { get; private set; }

        // Static copies for PersistentRunner access
        internal static KeyCode StaticToggleKey = KeyCode.F9;
        internal static bool StaticRequireCtrl = false;
        internal static bool StaticBlockInputWhenTyping = true;

        public enum ChestLabelVisibility { Hidden, OnHover, Visible }

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

            AutoEnableSmartChestOnRuleAdd = config.Bind(
                "UI",
                "AutoEnableSmartChestOnRuleAdd",
                true,
                "Automatically enable Smart Chest when adding a new rule to a chest"
            );

            UIScale = config.Bind(
                "UI",
                "UIScale",
                1f,
                new BepInEx.Configuration.ConfigDescription(
                    "Scale factor for Smart Chest config window (1.0 = default)",
                    new BepInEx.Configuration.AcceptableValueRange<float>(0.5f, 2.5f)
                ));

            BlockInputWhenTypingInConfig = config.Bind(
                "UI",
                "BlockInputWhenTypingInConfig",
                true,
                "When true, Backspace/Cancel won't close the chest while typing in the config search. Set to FALSE if the in-game chat or cheat console cannot receive input (fixes conflict with some mods).");

            CheckForUpdates = config.Bind(
                "Updates",
                "CheckForUpdates",
                true,
                "Check for mod updates on startup"
            );

            EnableChestLabels = config.Bind(
                "ChestLabels",
                "EnableChestLabels",
                true,
                "Show labels above chests (Wooden Chest, Large Wooden Chest, etc.). Excludes Hoppers and Animal Feeders."
            );

            LabelVisibility = config.Bind(
                "ChestLabels",
                "LabelVisibility",
                ChestLabelVisibility.Visible,
                "When to show chest labels: Visible, OnHover, or Hidden"
            );

            IconVisibility = config.Bind(
                "ChestLabels",
                "IconVisibility",
                ChestLabelVisibility.Visible,
                "When to show item icons (when label starts with item ID): Visible, OnHover, or Hidden"
            );

            // Initialize static values
            StaticToggleKey = ToggleKey.Value;
            StaticRequireCtrl = RequireCtrlModifier.Value;
            StaticBlockInputWhenTyping = BlockInputWhenTypingInConfig.Value;

            // Subscribe to config changes
            ToggleKey.SettingChanged += (_, _) => StaticToggleKey = ToggleKey.Value;
            RequireCtrlModifier.SettingChanged += (_, _) => StaticRequireCtrl = RequireCtrlModifier.Value;
            BlockInputWhenTypingInConfig.SettingChanged += (_, _) => StaticBlockInputWhenTyping = BlockInputWhenTypingInConfig.Value;
        }

        public float GetScanInterval()
        {
            return Mathf.Max(10f, ScanInterval.Value);
        }
    }
}