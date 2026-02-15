namespace HavensAlmanac.Data
{
    /// <summary>
    /// Interface for JIT-safe data providers. Each implementation
    /// is only instantiated when its corresponding mod is confirmed loaded.
    /// </summary>
    public interface IModDataProvider
    {
        /// <summary>Display name for this mod section (e.g., "Todo", "Birthday")</summary>
        string ModName { get; }

        /// <summary>Icon character for HUD display</summary>
        string ModIcon { get; }

        /// <summary>Short status string for the HUD (e.g., "3 active / 75%")</summary>
        string HudSummary { get; }

        /// <summary>Whether this provider has data ready to display</summary>
        bool IsReady { get; }

        /// <summary>Refresh cached data from the mod's API.</summary>
        void Refresh();

        /// <summary>Draw the full dashboard section using IMGUI.</summary>
        void DrawDashboardSection();

        /// <summary>Draw the daily briefing section using IMGUI. Return false to skip.</summary>
        bool DrawBriefingSection();
    }
}
