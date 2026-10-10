using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Wish;

namespace HavensRespec.Services
{
    /// <summary>
    /// Rolls back Max Mana granted by skill nodes when a profession is reset.
    /// <see cref="SkillNode.SetActive"/> often repaints the tree without reversing permanent
    /// mana bonuses (Mental Focus flat ranks; Town Spirit / Soul Food style progress caps).
    /// </summary>
    internal static class SkillManaRollback
    {
        private static readonly Regex SlashRankRegex = new Regex(
            @"(\d+(?:\.\d+)?)\s*/\s*(\d+(?:\.\d+)?)(?:\s*/\s*(\d+(?:\.\d+)?))?",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static MethodInfo _getProgressFloatCharacter;
        private static MethodInfo _setProgressFloatCharacter;
        private static MethodInfo _getProgressIntCharacter;
        private static MethodInfo _setProgressIntCharacter;
        private static bool _progressApisResolved;

        /// <summary>
        /// After nodes are deactivated, remove leftover Max Mana from mana-granting skills and
        /// clear matching character progress counters. Returns the amount of Max Mana removed
        /// (for Undo) plus any progress keys that were zeroed.
        /// </summary>
        public static ManaRollbackResult Apply(
            IReadOnlyList<ActiveManaNode> deactivatedManaNodes,
            ManualLogSource log,
            Func<bool> isDebug)
        {
            var result = new ManaRollbackResult();
            if (deactivatedManaNodes == null || deactivatedManaNodes.Count == 0)
                return result;

            float manaBefore = ReadMaxMana();
            if (manaBefore < 0f)
                return result;

            EnsureProgressApis();
            var save = SingletonBehaviour<GameSave>.Instance;

            float expectedRemove = 0f;
            foreach (var node in deactivatedManaNodes)
            {
                if (node == null || string.IsNullOrEmpty(node.NodeName))
                    continue;

                float fromProgress = TryConsumeProgress(save, node.NodeName, result, log, isDebug);
                if (fromProgress > 0f)
                {
                    expectedRemove += fromProgress;
                    continue;
                }

                float flat = EstimateFlatManaBonus(node.Description, node.NodeAmount);
                if (flat > 0f)
                    expectedRemove += flat;
            }

            if (expectedRemove <= 0f)
            {
                if (isDebug?.Invoke() == true)
                    log?.LogDebug("[Respec] SkillManaRollback: no mana contribution estimated from deactivated nodes.");
                return result;
            }

            // Prefer whatever SetActive already removed; only subtract the remainder.
            float manaAfterDeactivate = ReadMaxMana();
            float alreadyRemoved = manaBefore >= 0f && manaAfterDeactivate >= 0f
                ? Mathf.Max(0f, manaBefore - manaAfterDeactivate)
                : 0f;
            float stillToRemove = Mathf.Max(0f, expectedRemove - alreadyRemoved);
            if (stillToRemove <= 0.01f)
            {
                if (isDebug?.Invoke() == true)
                    log?.LogDebug($"[Respec] SkillManaRollback: SetActive already removed ~{alreadyRemoved:0.##} Max Mana.");
                result.ManaRemoved = alreadyRemoved;
                return result;
            }

            if (!TryWriteMaxMana(manaAfterDeactivate - stillToRemove, log))
            {
                log?.LogWarning($"[Respec] SkillManaRollback: could not reduce Max Mana by {stillToRemove:0.##}.");
                result.ManaRemoved = alreadyRemoved;
                return result;
            }

            result.ManaRemoved = alreadyRemoved + stillToRemove;
            log?.LogInfo($"[Respec] SkillManaRollback: removed {result.ManaRemoved:0.##} Max Mana from refunded skill nodes.");
            return result;
        }

        public static void Restore(ManaRollbackResult rollback, ManualLogSource log)
        {
            if (rollback == null)
                return;

            EnsureProgressApis();
            var save = SingletonBehaviour<GameSave>.Instance;
            if (save != null && rollback.ProgressFloats != null)
            {
                foreach (var kv in rollback.ProgressFloats)
                    TrySetProgressFloat(save, kv.Key, kv.Value);
            }

            if (save != null && rollback.ProgressInts != null)
            {
                foreach (var kv in rollback.ProgressInts)
                    TrySetProgressInt(save, kv.Key, kv.Value);
            }

            if (rollback.ManaRemoved > 0.01f)
            {
                float current = ReadMaxMana();
                if (current >= 0f)
                    TryWriteMaxMana(current + rollback.ManaRemoved, log);
            }
        }

        public static bool LooksLikeManaNode(SkillNode node)
        {
            if (node == null)
                return false;
            string text = BuildDescription(node);
            if (string.IsNullOrEmpty(text))
                return false;
            return text.IndexOf("mana", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string BuildDescription(SkillNode node)
        {
            if (node == null)
                return string.Empty;

            // Prefer localized skill blurb fields when present; fall back to nodeName.
            string[] parts =
            {
                TryReadString(node, "SkillDescription"),
                TryReadString(node, "description"),
                TryReadString(node, "singleDescriptionItem"),
                TryReadString(node, "SkillTitle"),
                TryReadString(node, "nodeTitle"),
                node.nodeName
            };

            return string.Join(" ", parts).Trim();
        }

        public static float EstimateFlatManaBonus(string description, int nodeAmount)
        {
            if (string.IsNullOrEmpty(description) || nodeAmount <= 0)
                return 0f;

            // Progressive / cap skills ("Max +10/20/30") are handled via character progress.
            if (description.IndexOf("max +", StringComparison.OrdinalIgnoreCase) >= 0
                || description.IndexOf("up to", StringComparison.OrdinalIgnoreCase) >= 0
                || description.IndexOf("each ", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 0f;
            }

            var match = SlashRankRegex.Match(description);
            if (!match.Success)
                return 0f;

            float[] ranks = ParseRanks(match);
            int idx = Mathf.Clamp(nodeAmount, 1, ranks.Length) - 1;
            return Mathf.Max(0f, ranks[idx]);
        }

        private static float[] ParseRanks(Match match)
        {
            var list = new List<float>(3);
            for (int i = 1; i < match.Groups.Count; i++)
            {
                if (!match.Groups[i].Success)
                    continue;
                if (float.TryParse(match.Groups[i].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                    list.Add(v);
            }

            if (list.Count == 0)
                return new[] { 0f };
            return list.ToArray();
        }

        private static float TryConsumeProgress(
            GameSave save,
            string nodeName,
            ManaRollbackResult result,
            ManualLogSource log,
            Func<bool> isDebug)
        {
            if (save == null || string.IsNullOrEmpty(nodeName))
                return 0f;

            // Game commonly keys skill progress by nodeName (and occasionally by StableHash).
            string[] keys =
            {
                nodeName,
                nodeName + "Mana",
                nodeName + "MaxMana",
                nodeName + "Progress"
            };

            foreach (var key in keys)
            {
                if (TryGetProgressFloat(save, key, out float f) && f > 0f)
                {
                    result.ProgressFloats[key] = f;
                    TrySetProgressFloat(save, key, 0f);
                    if (isDebug?.Invoke() == true)
                        log?.LogDebug($"[Respec] Cleared float progress '{key}' = {f:0.##}");
                    return f;
                }

                if (TryGetProgressInt(save, key, out int i) && i > 0)
                {
                    result.ProgressInts[key] = i;
                    TrySetProgressInt(save, key, 0);
                    if (isDebug?.Invoke() == true)
                        log?.LogDebug($"[Respec] Cleared int progress '{key}' = {i}");
                    return i;
                }
            }

            return 0f;
        }

        private static void EnsureProgressApis()
        {
            if (_progressApisResolved)
                return;
            _progressApisResolved = true;
            var t = typeof(GameSave);
            _getProgressFloatCharacter = AccessTools.Method(t, "GetProgressFloatCharacter", new[] { typeof(string) });
            _setProgressFloatCharacter = AccessTools.Method(t, "SetProgressFloatCharacter", new[] { typeof(string), typeof(float) });
            _getProgressIntCharacter = AccessTools.Method(t, "GetProgressIntCharacter", new[] { typeof(string) });
            _setProgressIntCharacter = AccessTools.Method(t, "SetProgressIntCharacter", new[] { typeof(string), typeof(int) });
        }

        private static bool TryGetProgressFloat(GameSave save, string key, out float value)
        {
            value = 0f;
            if (_getProgressFloatCharacter == null)
                return false;
            try
            {
                value = Convert.ToSingle(_getProgressFloatCharacter.Invoke(save, new object[] { key }), CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void TrySetProgressFloat(GameSave save, string key, float value)
        {
            try
            {
                _setProgressFloatCharacter?.Invoke(save, new object[] { key, value });
            }
            catch
            {
                // Best-effort — missing API on older builds is fine.
            }
        }

        private static bool TryGetProgressInt(GameSave save, string key, out int value)
        {
            value = 0;
            if (_getProgressIntCharacter == null)
                return false;
            try
            {
                value = Convert.ToInt32(_getProgressIntCharacter.Invoke(save, new object[] { key }), CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void TrySetProgressInt(GameSave save, string key, int value)
        {
            try
            {
                _setProgressIntCharacter?.Invoke(save, new object[] { key, value });
            }
            catch
            {
                // Best-effort.
            }
        }

        private static float ReadMaxMana()
        {
            try
            {
                var player = Player.Instance;
                if (player == null)
                    return -1f;
                return player.MaxMana;
            }
            catch
            {
                return -1f;
            }
        }

        private static bool TryWriteMaxMana(float newMax, ManualLogSource log)
        {
            try
            {
                var player = Player.Instance;
                if (player == null)
                    return false;

                newMax = Mathf.Max(1f, newMax);

                var prop = AccessTools.Property(typeof(Player), "MaxMana");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(player, newMax, null);
                    return true;
                }

                // Fallback: write StatType.Mana into Player.Stats when present.
                var statsProp = AccessTools.Property(typeof(Player), "Stats")
                    ?? AccessTools.Field(typeof(Player), "stats") as MemberInfo;
                object stats = null;
                if (statsProp is PropertyInfo pi)
                    stats = pi.GetValue(player, null);
                else if (statsProp is FieldInfo fi)
                    stats = fi.GetValue(player);

                if (stats != null)
                {
                    var indexer = stats.GetType().GetProperty("Item", new[] { typeof(StatType) });
                    if (indexer != null && indexer.CanWrite)
                    {
                        indexer.SetValue(stats, newMax, new object[] { StatType.Mana });
                        return true;
                    }
                }

                log?.LogDebug("[Respec] SkillManaRollback: no writable MaxMana/Stats path found.");
                return false;
            }
            catch (Exception ex)
            {
                log?.LogDebug($"[Respec] SkillManaRollback write failed: {ex.Message}");
                return false;
            }
        }

        private static string TryReadString(object obj, string memberName)
        {
            try
            {
                var field = AccessTools.Field(obj.GetType(), memberName);
                if (field != null && field.FieldType == typeof(string))
                    return field.GetValue(obj) as string ?? string.Empty;
                var prop = AccessTools.Property(obj.GetType(), memberName);
                if (prop != null && prop.PropertyType == typeof(string))
                    return prop.GetValue(obj, null) as string ?? string.Empty;
            }
            catch
            {
                // ignore
            }

            return string.Empty;
        }
    }

    internal sealed class ActiveManaNode
    {
        public string NodeName;
        public string Description;
        public int NodeAmount;
    }

    internal sealed class ManaRollbackResult
    {
        public float ManaRemoved;
        public Dictionary<string, float> ProgressFloats { get; } = new Dictionary<string, float>();
        public Dictionary<string, int> ProgressInts { get; } = new Dictionary<string, int>();
    }
}
