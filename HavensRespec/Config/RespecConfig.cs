using BepInEx.Configuration;
using UnityEngine;

namespace HavensRespec.Config
{
    /// <summary>
    /// What, if anything, the player pays to reset a profession.
    /// Default is <see cref="None"/> — match what players usually expect from free skill respecs
    /// in other mods, while keeping a path to add friction.
    /// </summary>
    public enum RespecCostMode
    {
        None = 0,
        Gold = 1,
        Gems = 2,
    }

    internal sealed class RespecConfig
    {
        public ConfigEntry<bool> Enabled { get; }
        public ConfigEntry<bool> InjectButtons { get; }
        public ConfigEntry<bool> RequireConfirmation { get; }
        public ConfigEntry<bool> ShiftSkipsConfirmation { get; }
        public ConfigEntry<bool> EnableUndo { get; }
        public ConfigEntry<bool> EnableResetAll { get; }
        public ConfigEntry<RespecCostMode> CostMode { get; }
        public ConfigEntry<int> GoldPerPoint { get; }
        public ConfigEntry<int> GemsPerPoint { get; }
        public ConfigEntry<KeyCode> ResetCurrentTabHotkey { get; }
        public ConfigEntry<KeyCode> UndoHotkey { get; }
        public ConfigEntry<bool> DebugLogging { get; }

        public RespecConfig(ConfigFile config)
        {
            Enabled = config.Bind(
                "General",
                "Enabled",
                true,
                "Enable Haven's Respec. When false the mod loads but installs no patches and adds no buttons.");

            InjectButtons = config.Bind(
                "UI",
                "InjectButtons",
                true,
                "Add a styled \"Reset\" button to every skill tab. Disable this if you only want to use the hotkeys or an external integration.");

            RequireConfirmation = config.Bind(
                "UI",
                "RequireConfirmation",
                true,
                "Show a confirmation dialog before wiping a skill tree. Strongly recommended — there is no recovery if Undo is also disabled.");

            ShiftSkipsConfirmation = config.Bind(
                "UI",
                "ShiftSkipsConfirmation",
                true,
                "If RequireConfirmation is true, holding Shift while clicking the Reset button bypasses the confirmation dialog.");

            EnableUndo = config.Bind(
                "UI",
                "EnableUndo",
                true,
                "Keep a one-step in-memory snapshot of the most recent reset per profession and show an Undo button next to the Reset button. Snapshots do not survive a game restart.");

            EnableResetAll = config.Bind(
                "UI",
                "EnableResetAll",
                true,
                "Add a \"Reset All\" button above the profession tabs that confirms once and then resets every profession in sequence.");

            CostMode = config.Bind(
                "Cost",
                "Mode",
                RespecCostMode.None,
                "Cost model for a reset. None = free (original mod behaviour). Gold and Gems deduct based on the number of skill points about to be refunded.");

            GoldPerPoint = config.Bind(
                "Cost",
                "GoldPerPoint",
                100,
                new ConfigDescription(
                    "How many coins (orange gold) per refunded skill point when Mode = Gold.",
                    new AcceptableValueRange<int>(0, 100_000)));

            GemsPerPoint = config.Bind(
                "Cost",
                "GemsPerPoint",
                1,
                new ConfigDescription(
                    "How many tickets/gems per refunded skill point when Mode = Gems.",
                    new AcceptableValueRange<int>(0, 1_000)));

            ResetCurrentTabHotkey = config.Bind(
                "Hotkeys",
                "ResetCurrentTab",
                KeyCode.None,
                "Optional hotkey that triggers a reset of the currently-open skill tab (as if you had clicked its Reset button). Default None = unbound.");

            UndoHotkey = config.Bind(
                "Hotkeys",
                "Undo",
                KeyCode.None,
                "Optional hotkey that undoes the last reset for the currently-open skill tab. Default None = unbound.");

            DebugLogging = config.Bind(
                "Debug",
                "DebugLogging",
                false,
                "Verbose logging for every patch hit, button injection, node zero-out, and snapshot operation. Leave off unless diagnosing an issue.");
        }
    }
}
