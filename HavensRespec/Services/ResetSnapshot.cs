using System.Collections.Generic;
using HavensRespec.Config;
using Wish;

namespace HavensRespec.Services
{
    /// <summary>
    /// Captures enough state to reverse a single profession reset. Snapshots live in memory only —
    /// they deliberately do not persist across game restarts so a restart + reset cannot compound
    /// into an undo bomb, and so save files stay untouched.
    /// </summary>
    internal sealed class ResetSnapshot
    {
        public ProfessionType Profession { get; }
        public IReadOnlyDictionary<int, int> Nodes { get; }
        public int SkillPointsUsed { get; }
        public int NumActiveNodes { get; }
        public int ChargedCost { get; }
        public RespecCostMode ChargedCostMode { get; }

        public ResetSnapshot(
            ProfessionType profession,
            IReadOnlyDictionary<int, int> nodes,
            int skillPointsUsed,
            int numActiveNodes,
            int chargedCost = 0,
            RespecCostMode chargedCostMode = RespecCostMode.None)
        {
            Profession = profession;
            Nodes = nodes;
            SkillPointsUsed = skillPointsUsed;
            NumActiveNodes = numActiveNodes;
            ChargedCost = chargedCost;
            ChargedCostMode = chargedCostMode;
        }
    }
}
