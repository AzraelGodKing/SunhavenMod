using System;
using System.Collections.Generic;
using System.Linq;
using HavenDevTools.API;
using HavenDevTools.Services;
using UnityEngine;

namespace TrinketFortune.DevTools
{
    /// <summary>
    /// DevTools panel for Trinket Fortune. Shows missing aquarium/fishing trinkets and
    /// probability of getting them when that data is available (SMUT + fishing loot patch).
    /// </summary>
    public class TrinketFortunePanel : IDevToolsPanel
    {
        public string ModGuid => "com.azraelgodking.trinketfortune";
        public string DisplayName => "Trinket Fortune";

        public void Draw(GUIStyle boxStyle, GUIStyle buttonStyle, GUIStyle labelStyle)
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Trinket Fortune - Fishing Trinket Bias", labelStyle);
            GUILayout.Space(4);

            // Always show mod status
            GUILayout.Label($"Enabled: {TrinketFortune.Config.Enabled.Value}", labelStyle);
            GUILayout.Label($"Bonus: +{TrinketFortune.Config.MuseumProgressBonusPercent.Value}% per 10% aquarium", labelStyle);
            GUILayout.Space(6);

            var inspector = HavenDevTools.Plugin.GetBundleInspector();
            if (inspector == null)
            {
                GUILayout.Label("S.M.U.T. not loaded - install Sun Haven Museum Utility Tracker for aquarium data.", labelStyle);
                GUILayout.EndVertical();
                return;
            }

            var sections = inspector.GetAllSections();
            var aquarium = sections?.FirstOrDefault(s =>
                (s.Name?.IndexOf("aquarium", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);

            if (aquarium == null || aquarium.Bundles == null || aquarium.Bundles.Count == 0)
            {
                GUILayout.Label("Aquarium data not available. Ensure S.M.U.T. is installed and loaded.", labelStyle);
                GUILayout.EndVertical();
                return;
            }

            var missing = new List<(string Name, int GameItemId)>();
            int total = 0;
            foreach (var bundle in aquarium.Bundles)
            {
                if (bundle?.Items == null) continue;
                foreach (var item in bundle.Items)
                {
                    total++;
                    if (!inspector.HasDonated(item.Id))
                        missing.Add((item.Name, item.GameItemId));
                }
            }

            var stats = inspector.GetDonationStats();
            if (stats.IsLoaded)
                GUILayout.Label($"Character: {stats.CharacterName}", labelStyle);
            GUILayout.Label($"Aquarium: {total - missing.Count}/{total} donated", labelStyle);
            GUILayout.Label($"Missing trinkets: {missing.Count}", labelStyle);
            GUILayout.Space(6);

            if (missing.Count > 0)
            {
                GUILayout.Label("Missing items (bias applied when fishing):", labelStyle);
                foreach (var (name, gameItemId) in missing)
                {
                    string idText = gameItemId > 0 ? $" (ID: {gameItemId})" : " (in-game only)";
                    GUILayout.Label($"  • {name}{idText}", labelStyle);
                }
            }
            else
                GUILayout.Label("All aquarium trinkets donated!", labelStyle);

            GUILayout.Space(6);
            GUILayout.Label("Note: Main fish and 5% bonus museum items biased toward missing trinkets when S.M.U.T. loaded.", labelStyle);

            GUILayout.EndVertical();
        }
    }
}
