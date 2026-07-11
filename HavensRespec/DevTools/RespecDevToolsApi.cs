using HavensRespec.Services;
using Wish;

namespace HavensRespec.DevTools
{
    /// <summary>
    /// Public surface for Haven Dev Tools — dry-run profession reset preview without the in-game Simulate button.
    /// </summary>
    public static class RespecDevToolsApi
    {
        private static readonly ProfessionType[] Professions = ProfessionUiMap.OrderedProfessions;

        public static bool IsReady
        {
            get
            {
                Plugin.EnsureModReady();
                return Plugin.GetController() != null && Plugin.IsModEnabled;
            }
        }

        public static int ProfessionCount => Professions.Length;

        public static string GetProfessionName(int professionIndex)
        {
            if (!TryGetProfession(professionIndex, out var profession))
                return "?";
            return ProfessionUiMap.GetDisplayName(profession);
        }

        public static int GetActiveProfessionIndex()
        {
            var controller = Plugin.GetController();
            if (controller == null)
                return -1;

            var active = controller.TryGetActiveProfessionTab();
            if (!active.HasValue)
                return -1;

            return IndexOf(active.Value);
        }

        public static bool TryGetEstimate(int professionIndex, out int refund, out int cost, out bool canAfford, out string costLabel)
        {
            refund = 0;
            cost = 0;
            canAfford = true;
            costLabel = string.Empty;

            var controller = Plugin.GetController();
            if (controller == null || !TryGetProfession(professionIndex, out var profession))
                return false;

            return controller.TryGetEstimate(profession, out refund, out cost, out canAfford, out costLabel);
        }

        public static bool TrySimulate(int professionIndex, out string errorMessage)
        {
            errorMessage = null;
            var controller = Plugin.GetController();
            if (controller == null)
            {
                errorMessage = "Respec controller unavailable.";
                return false;
            }

            if (!TryGetProfession(professionIndex, out var profession))
            {
                errorMessage = "Invalid profession.";
                return false;
            }

            return controller.TrySimulateProfession(profession, out errorMessage);
        }

        public static bool HasPendingSimulation(
            out int professionIndex,
            out int refunded,
            out int cost,
            out bool canAfford,
            out string costLabel)
        {
            professionIndex = -1;
            refunded = 0;
            cost = 0;
            canAfford = true;
            costLabel = string.Empty;

            var controller = Plugin.GetController();
            if (controller == null || !controller.TryGetPendingSimulation(out var pending))
                return false;

            professionIndex = IndexOf(pending.Profession);
            refunded = pending.RefundedPoints;
            cost = pending.Cost;
            canAfford = pending.CanAfford;
            costLabel = pending.CostLabel;
            return true;
        }

        public static bool TryCommitSimulation(out string errorMessage)
        {
            errorMessage = null;
            var controller = Plugin.GetController();
            if (controller == null)
            {
                errorMessage = "Respec controller unavailable.";
                return false;
            }

            return controller.TryCommitPendingSimulation(out errorMessage);
        }

        public static bool TryCancelSimulation(out string errorMessage)
        {
            errorMessage = null;
            var controller = Plugin.GetController();
            if (controller == null)
            {
                errorMessage = "Respec controller unavailable.";
                return false;
            }

            return controller.TryCancelPendingSimulation(out errorMessage);
        }

        private static bool TryGetProfession(int professionIndex, out ProfessionType profession)
        {
            profession = default;
            if (professionIndex < 0 || professionIndex >= Professions.Length)
                return false;
            profession = Professions[professionIndex];
            return true;
        }

        private static int IndexOf(ProfessionType profession)
        {
            for (int i = 0; i < Professions.Length; i++)
            {
                if (Professions[i] == profession)
                    return i;
            }

            return -1;
        }
    }
}
