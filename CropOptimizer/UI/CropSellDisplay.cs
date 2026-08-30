using CropOptimizer.Data;
using SunhavenMods.Shared;

namespace CropOptimizer.UI
{
    /// <summary>Formats gold / orbs / tickets for the HUD, hover tooltip, and Almanac summary.</summary>
    internal static class CropSellDisplay
    {
        public static string FormatHudLine(CropShopValue value)
        {
            bool gold = value.Gold > 0;
            bool orbs = value.Orbs > 0;
            bool tickets = value.Tickets > 0;

            if (orbs && !gold && !tickets)
                return ModLocalization.T("crop.hud.projectedOrbs", value.Orbs);
            if (tickets && !gold && !orbs)
                return ModLocalization.T("crop.hud.projectedTickets", value.Tickets);
            if (gold && !orbs && !tickets)
                return ModLocalization.T("crop.hud.projected", value.Gold);
            if (!gold && !orbs && !tickets)
                return ModLocalization.T("crop.hud.projected", 0);

            var parts = new System.Collections.Generic.List<string>(3);
            if (gold)
                parts.Add(ModLocalization.T("crop.hud.amountGold", value.Gold));
            if (orbs)
                parts.Add(ModLocalization.T("crop.hud.amountOrbs", value.Orbs));
            if (tickets)
                parts.Add(ModLocalization.T("crop.hud.amountTickets", value.Tickets));
            return ModLocalization.T("crop.hud.projectedJoin", string.Join(" · ", parts));
        }

        public static string FormatPlain(CropShopValue value)
        {
            bool gold = value.Gold > 0;
            bool orbs = value.Orbs > 0;
            bool tickets = value.Tickets > 0;
            if (!gold && !orbs && !tickets)
                return "0g";

            var parts = new System.Collections.Generic.List<string>(3);
            if (gold)
                parts.Add($"{value.Gold:N0}g");
            if (orbs)
                parts.Add($"{value.Orbs:N0} orbs");
            if (tickets)
                parts.Add($"{value.Tickets:N0} tickets");
            return string.Join(" · ", parts);
        }

        public static string FormatTooltipLine(string accentColor, int amount, string key)
        {
            return ModLocalization.T(key, accentColor, amount);
        }
    }
}
