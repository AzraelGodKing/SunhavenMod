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
        public ConfigEntry<bool> HoverTooltipEnabled { get; }
        public ConfigEntry<float> HoverTooltipMaxWorldDistance { get; }
        public ConfigEntry<bool> DebugLogging { get; }
        public ConfigEntry<bool> CheckForUpdates { get; }

        public CropOptimizerConfig(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true, "Enable Crop Optimizer");
            CheckForUpdates = config.Bind("General", "CheckForUpdates", true, "Check for Crop Optimizer updates on startup via GitHub Pages.");
            HudEnabled = config.Bind("HUD", "Enabled", true, "Show Crop Optimizer HUD");
            HudScale = config.Bind("HUD", "Scale", 1.0f, new ConfigDescription("HUD scale", new AcceptableValueRange<float>(0.5f, 2.5f)));
            HudPositionX = config.Bind("HUD", "PositionX", 20f, "HUD window X (pixels); updated when you drag the panel)");
            HudPositionY = config.Bind("HUD", "PositionY", 80f, "HUD window Y (pixels); updated when you drag the panel)");
            ToggleHudKey = config.Bind("HUD", "ToggleKey", KeyCode.F3, "Toggle crop HUD");
            HoverTooltipEnabled = config.Bind("HUD", "HoverTooltip", true, "Experimental: show crop info (crop name, water/fertil guess, ETA) when the mouse is near a crop in-world");
            HoverTooltipMaxWorldDistance = config.Bind("HUD", "HoverTooltipMaxWorldDistance", 5f, new ConfigDescription("Max distance from mouse (world units) to treat a crop as hovered", new AcceptableValueRange<float>(0.25f, 16f)));
            DebugLogging = config.Bind("Debug", "DebugLogging", false, "Enable debug logging");
        }
    }
}
