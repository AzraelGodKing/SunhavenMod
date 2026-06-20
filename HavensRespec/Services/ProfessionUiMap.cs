using SunhavenMods.Shared;
using Wish;

namespace HavensRespec.Services
{
    internal static class ProfessionUiMap
    {
        public static readonly ProfessionType[] OrderedProfessions =
        {
            ProfessionType.Combat,
            ProfessionType.Farming,
            ProfessionType.Fishing,
            ProfessionType.Mining,
            ProfessionType.Exploration
        };

        public static string ResolvePanelFieldName(ProfessionType profession) => profession switch
        {
            ProfessionType.Combat => "_combatPanel",
            ProfessionType.Farming => "_farmingPanel",
            ProfessionType.Mining => "_miningPanel",
            ProfessionType.Exploration => "_artisanryPanel",
            ProfessionType.Fishing => "_fishingPanel",
            _ => null,
        };

        public static string GetDisplayName(ProfessionType profession) =>
            ModLocalization.T($"respec.profession.{profession}");
    }
}
