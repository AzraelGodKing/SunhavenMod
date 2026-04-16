using BepInEx.Configuration;
using UnityEngine;

namespace CropOptimizer.Config
{
    internal sealed class CropOptimizerConfig
    {
        public ConfigEntry<bool> Enabled { get; }
        public ConfigEntry<bool> HudEnabled { get; }
        public ConfigEntry<float> HudScale { get; }
        public ConfigEntry<float> HudPositionX { get; }
        public ConfigEntry<float> HudPositionY { get; }
        public ConfigEntry<KeyCode> ToggleHudKey { get; }
        public ConfigEntry<bool> DebugLogging { get; }

        public CropOptimizerConfig(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true, "Enable Crop Optimizer");
            HudEnabled = config.Bind("HUD", "Enabled", true, "Show Crop Optimizer HUD");
            HudScale = config.Bind("HUD", "Scale", 1.0f, new ConfigDescription("HUD scale", new AcceptableValueRange<float>(0.5f, 2.5f)));
            HudPositionX = config.Bind("HUD", "PositionX", 20f, "HUD window X (pixels); updated when you drag the panel)");
            HudPositionY = config.Bind("HUD", "PositionY", 80f, "HUD window Y (pixels); updated when you drag the panel)");
            ToggleHudKey = config.Bind("HUD", "ToggleKey", KeyCode.F3, "Toggle crop HUD");
            DebugLogging = config.Bind("Debug", "DebugLogging", false, "Enable debug logging");
        }
    }
}
