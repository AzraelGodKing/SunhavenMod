using System;
using System.Collections.Generic;

namespace HavensBirthright
{
    /// <summary>
    /// Optional cross-race grants: copy a passive racial bonus percentage from one race's table onto another.
    /// Config: [BonusTransfers] EnableBonusTransfers, Rules (TargetRace|SourceRace|BonusType; semicolon-separated).
    /// </summary>
    internal static class BonusTransferRules
    {
        private static readonly List<Rule> Rules = new List<Rule>();

        private readonly struct Rule
        {
            public readonly Race TargetRace;
            public readonly Race SourceRace;
            public readonly BonusType BonusType;

            public Rule(Race targetRace, Race sourceRace, BonusType bonusType)
            {
                TargetRace = targetRace;
                SourceRace = sourceRace;
                BonusType = bonusType;
            }
        }

        internal static void RebuildFromConfig(string raw)
        {
            Rules.Clear();
            if (string.IsNullOrWhiteSpace(raw))
                return;

            foreach (var segment in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var part = segment.Trim();
                if (part.Length == 0)
                    continue;

                var fields = part.Split('|');
                if (fields.Length != 3)
                {
                    Plugin.Log.LogWarning(
                        $"[BonusTransfer] Skip invalid rule (need Target|Source|BonusType): \"{part}\"");
                    continue;
                }

                if (!Enum.TryParse(fields[0].Trim(), ignoreCase: true, out Race target) ||
                    !Enum.TryParse(fields[1].Trim(), ignoreCase: true, out Race source) ||
                    !Enum.TryParse(fields[2].Trim(), ignoreCase: true, out BonusType bonus))
                {
                    Plugin.Log.LogWarning(
                        $"[BonusTransfer] Skip rule with unknown Race/BonusType: \"{part}\"");
                    continue;
                }

                Rules.Add(new Rule(target, source, bonus));
            }

            Plugin.Log.LogInfo($"[BonusTransfer] Loaded {Rules.Count} rule(s)");
        }

        /// <summary>
        /// Sum of native bonus % from source races for rules matching current race and bonus type.
        /// </summary>
        internal static float GetTransferredPercent(RacialBonusManager manager, Race currentRace, BonusType type)
        {
            if (Rules.Count == 0 || manager == null ||
                RacialConfig.EnableBonusTransfers == null || !RacialConfig.EnableBonusTransfers.Value)
                return 0f;

            float sum = 0f;
            foreach (var rule in Rules)
            {
                if (rule.TargetRace != currentRace || rule.BonusType != type)
                    continue;

                foreach (var bonus in manager.GetBonusesForRace(rule.SourceRace))
                {
                    if (bonus.Type == type)
                    {
                        sum += bonus.Value;
                        break;
                    }
                }
            }

            return sum;
        }
    }
}
