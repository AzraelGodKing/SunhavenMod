using System;
using System.Collections.Generic;
using System.Linq;
using Wish;

namespace HavenDevTools.Services
{
    /// <summary>
    /// NPC relationship read/write (CheatEnabler / QuantumConsoleManager parity for Haven Dev Tools UI).
    /// </summary>
    public static class NpcRelationshipEditor
    {
        public sealed class NpcRelationshipRow
        {
            public string Key;
            public string DisplayName;
            public float Hearts;
            public bool Romanceable;
            public bool IsDating;
            public bool IsMarriedTo;
            public bool IsPrimarySpouse;
        }

        public static bool IsAvailable =>
            SingletonBehaviour<NPCManager>.Instance?._npcs != null
            && SingletonBehaviour<GameSave>.Instance?.CurrentSave?.characterData != null;

        public static IReadOnlyList<NpcRelationshipRow> GetRows(bool romanceOnly, string nameFilter)
        {
            var rows = new List<NpcRelationshipRow>();
            if (!IsAvailable)
                return rows;

            var save = SingletonBehaviour<GameSave>.Instance;
            var relationships = save.CurrentSave.characterData.Relationships;
            string primary = save.GetProgressStringCharacter("MarriedWith") ?? string.Empty;
            string filter = nameFilter?.Trim() ?? string.Empty;

            foreach (var npc in SingletonBehaviour<NPCManager>.Instance._npcs.Values
                         .Where(n => n != null)
                         .OrderBy(n => n.OriginalName, StringComparer.OrdinalIgnoreCase))
            {
                if (romanceOnly && !npc.Romanceable)
                    continue;

                string key = npc.OriginalName;
                if (!string.IsNullOrEmpty(filter)
                    && key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0
                    && (npc.LocalizedActualNPCName ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                relationships.TryGetValue(key, out float hearts);
                rows.Add(new NpcRelationshipRow
                {
                    Key = key,
                    DisplayName = npc.LocalizedActualNPCName ?? key,
                    Hearts = hearts,
                    Romanceable = npc.Romanceable,
                    IsDating = save.GetProgressBoolCharacter("Dating" + key),
                    IsMarriedTo = save.GetProgressBoolCharacter("MarriedTo" + key),
                    IsPrimarySpouse = !string.IsNullOrEmpty(primary)
                        && primary.Equals(key, StringComparison.Ordinal),
                });
            }

            return rows;
        }

        public static string ResolveNpcKey(string input) =>
            NpcDictionaryHelper.ResolveNpcKey(SingletonBehaviour<NPCManager>.Instance?._npcs, input);

        public static bool TryGetNpc(string key, out NPCAI npc)
        {
            npc = null;
            if (string.IsNullOrWhiteSpace(key))
                return false;

            string resolved = ResolveNpcKey(key);
            if (resolved == null)
                return false;

            return SingletonBehaviour<NPCManager>.Instance._npcs.TryGetValue(resolved, out npc);
        }

        public static bool SetHearts(string npcKey, int amount)
        {
            if (!TryGetNpc(npcKey, out _))
                return false;

            string key = ResolveNpcKey(npcKey);
            amount = Math.Max(0, Math.Min(100, amount));
            SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Relationships[key] = amount;
            return true;
        }

        public static bool AdjustHearts(string npcKey, int delta)
        {
            if (!TryGetNpc(npcKey, out _))
                return false;

            string key = ResolveNpcKey(npcKey);
            var relationships = SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Relationships;
            relationships.TryGetValue(key, out float current);
            relationships[key] = Math.Max(0, Math.Min(100, current + delta));
            return true;
        }

        public static bool DateNpc(string npcKey)
        {
            if (!TryGetNpc(npcKey, out var npc))
                return false;

            npc.DatePlayer();
            return true;
        }

        public static bool SetPlatonic(string npcKey)
        {
            if (!TryGetNpc(npcKey, out var npc))
                return false;

            npc.PlatonicPlayer();
            return true;
        }

        /// <summary>
        /// Marries without divorcing existing spouses — calls vanilla <see cref="NPCAI.MarryPlayer"/> only.
        /// Requires Ultra Polygamy's Harmony patch on <see cref="NPCAI.MarryPlayer"/>.
        /// </summary>
        public static bool MarryNpcPolygamy(string npcKey)
        {
            if (!UltraPolygamyHelper.IsAvailable)
                return false;

            if (!TryGetNpc(npcKey, out var npc) || !npc.Romanceable)
                return false;

            SetHearts(npc.OriginalName, 100);
            npc.MarryPlayer();
            return true;
        }

        /// <summary>Marries each key that is romanceable and not already married. Returns count married.</summary>
        public static int MarryMultiple(IReadOnlyList<string> npcKeys)
        {
            if (!UltraPolygamyHelper.IsAvailable || npcKeys == null || npcKeys.Count == 0)
                return 0;

            int married = 0;
            foreach (string key in npcKeys)
            {
                if (!TryGetNpc(key, out var npc) || !npc.Romanceable)
                    continue;

                if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("MarriedTo" + npc.OriginalName))
                    continue;

                if (MarryNpcPolygamy(npc.OriginalName))
                    married++;
            }

            return married;
        }

        /// <summary>Vanilla <c>/marryNPC</c> — divorces current <see cref="MarriedWith"/> spouse first.</summary>
        public static bool MarryNpc(string npcKey)
        {
            if (!TryGetNpc(npcKey, out var npc))
                return false;

            var save = SingletonBehaviour<GameSave>.Instance;
            string currentSpouse = save.GetProgressStringCharacter("MarriedWith");
            if (!string.IsNullOrWhiteSpace(currentSpouse))
            {
                save.SetProgressStringCharacter("MarriedWith", string.Empty);
                save.SetProgressBoolCharacter("Married", false);
                save.SetProgressBoolCharacter("MarriedTo" + currentSpouse, false);
                GameSave.CurrentCharacter.Relationships[currentSpouse] = 40f;
                SingletonBehaviour<NPCManager>.Instance.GetRealNPC(currentSpouse)?.GenerateCycle();
            }

            SetHearts(npc.OriginalName, 100);
            npc.MarryPlayer();
            return true;
        }

        /// <summary>Vanilla <c>/divorceNPC</c> — clears whoever is in <see cref="MarriedWith"/> (npc arg ignored).</summary>
        public static bool DivorcePrimarySpouse()
        {
            var save = SingletonBehaviour<GameSave>.Instance;
            string spouse = save.GetProgressStringCharacter("MarriedWith");
            if (string.IsNullOrWhiteSpace(spouse))
                return false;

            save.SetProgressStringCharacter("MarriedWith", string.Empty);
            save.SetProgressBoolCharacter("Married", false);
            save.SetProgressBoolCharacter("MarriedTo" + spouse, false);
            save.SetProgressBoolWorld(spouse + "MarriedWalkPath", false);

            var realNpc = SingletonBehaviour<NPCManager>.Instance.GetRealNPC(spouse);
            realNpc?.GeneratePath();
            if (realNpc != null)
                SingletonBehaviour<NPCManager>.Instance.StartNPCPath(realNpc);

            GameSave.CurrentCharacter.Relationships[spouse] = 40f;
            realNpc?.GenerateCycle(false);
            return true;
        }

        /// <summary>Divorce a specific NPC (useful with multi-marriage mods).</summary>
        public static bool DivorceNpc(string npcKey)
        {
            if (!TryGetNpc(npcKey, out var npc))
                return false;

            string key = npc.OriginalName;
            var save = SingletonBehaviour<GameSave>.Instance;
            bool wasPrimary = string.Equals(save.GetProgressStringCharacter("MarriedWith"), key, StringComparison.Ordinal);

            save.SetProgressBoolCharacter("MarriedTo" + key, false);
            save.SetProgressBoolCharacter("Dating" + key, false);
            save.SetProgressBoolWorld(key + "MarriedWalkPath", false);

            if (wasPrimary)
            {
                save.SetProgressStringCharacter("MarriedWith", string.Empty);
                save.SetProgressBoolCharacter("Married", false);
            }
            else if (!AnyMarriedRomanceables(out _))
            {
                save.SetProgressBoolCharacter("Married", false);
                save.SetProgressStringCharacter("MarriedWith", string.Empty);
            }

            GameSave.CurrentCharacter.Relationships[key] = 40f;
            npc.GenerateCycle(false);
            npc.GeneratePath();
            SingletonBehaviour<NPCManager>.Instance.StartNPCPath(npc);
            return true;
        }

        public static bool SkipToCycle(string npcKey, int cycle)
        {
            if (!TryGetNpc(npcKey, out var npc))
                return false;

            string key = ResolveNpcKey(npcKey);
            cycle = Math.Max(0, Math.Min(15, cycle));
            var save = SingletonBehaviour<GameSave>.Instance;
            for (int i = 0; i < 16; i++)
                save.SetProgressBoolCharacter(key + " Cycle " + i, i < cycle);

            npc.GenerateCycle(true);
            return true;
        }

        public static void ResetAllHearts()
        {
            if (!IsAvailable)
                return;

            SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.relationships =
                new Dictionary<string, float>();
        }

        public static bool SetPrimarySpouse(string npcKey)
        {
            if (!TryGetNpc(npcKey, out _))
                return false;

            string key = ResolveNpcKey(npcKey);
            var save = SingletonBehaviour<GameSave>.Instance;
            if (!save.GetProgressBoolCharacter("MarriedTo" + key))
                return false;

            string previous = save.GetProgressStringCharacter("MarriedWith");
            if (!string.IsNullOrWhiteSpace(previous) && !previous.Equals(key, StringComparison.Ordinal))
                save.SetProgressBoolWorld(previous + "MarriedWalkPath", false);

            save.SetProgressStringCharacter("MarriedWith", key);
            save.SetProgressBoolCharacter("Married", true);
            save.SetProgressBoolWorld(key + "MarriedWalkPath", true);

            var realNpc = SingletonBehaviour<NPCManager>.Instance.GetRealNPC(key);
            realNpc?.GeneratePath();
            if (realNpc != null)
                SingletonBehaviour<NPCManager>.Instance.StartNPCPath(realNpc);
            return true;
        }

        private static bool AnyMarriedRomanceables(out int count)
        {
            count = 0;
            if (!IsAvailable)
                return false;

            var save = SingletonBehaviour<GameSave>.Instance;
            foreach (var npc in SingletonBehaviour<NPCManager>.Instance._npcs.Values)
            {
                if (npc == null || !npc.Romanceable)
                    continue;
                if (save.GetProgressBoolCharacter("MarriedTo" + npc.OriginalName))
                    count++;
            }

            return count > 0;
        }
    }
}
