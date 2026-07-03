using System;
using System.Collections.Generic;
using System.Linq;
using HavensAlmanac.Config;
using HavensAlmanac.Data;
using HavensAlmanac.Services;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    public class RelationshipDataProvider : IModDataProvider
    {
        public string ModName => "Relationships";
        public string ModIcon => "\u2665";

        private readonly List<RelationshipRow> _rows = new List<RelationshipRow>();
        private string _hudSummary = "Loading...";
        private bool _isReady;
        private int _ungiftedCount;
        private string _nameFilter = string.Empty;

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;
        public bool HasBriefingContent =>
            _isReady
            && AlmanacConfig.RelationshipsEnabled.Value
            && _ungiftedCount > 0;

        public void Refresh()
        {
            _rows.Clear();
            _ungiftedCount = 0;

            if (!AlmanacConfig.RelationshipsEnabled.Value)
            {
                _hudSummary = ModLocalization.T("almanac.provider.relationship.disabled");
                _isReady = true;
                return;
            }

            try
            {
                if (!GameRelationshipReader.IsAvailable)
                {
                    _hudSummary = ModLocalization.T("almanac.provider.relationship.notReady");
                    _isReady = false;
                    return;
                }

                bool romanceOnly = AlmanacConfig.RelationshipsRomanceOnly.Value;
                _rows.AddRange(GameRelationshipReader.ReadRows(romanceOnly, _nameFilter));
                SortRows();

                foreach (RelationshipRow row in _rows)
                {
                    if (!row.GiftedToday)
                        _ungiftedCount++;
                }

                if (_rows.Count == 0)
                    _hudSummary = ModLocalization.T("almanac.provider.relationship.noNpcs");
                else
                    _hudSummary = ModLocalization.T(
                        "almanac.provider.relationship.summary",
                        _rows.Count,
                        _ungiftedCount);

                _isReady = true;
            }
            catch (Exception ex)
            {
                _hudSummary = "Error";
                _isReady = false;
                Plugin.Log?.LogWarning($"[RelationshipProvider] Refresh error: {ex.Message}");
            }
        }

        public void DrawDashboardSection()
        {
            if (!AlmanacConfig.RelationshipsEnabled.Value)
            {
                GUILayout.Label(ModLocalization.T("almanac.provider.relationship.disabled"));
                return;
            }

            if (_rows.Count == 0)
            {
                GUILayout.Label(ModLocalization.T("almanac.provider.relationship.noNpcs"));
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("almanac.provider.relationship.filter"), GUILayout.Width(48f));
            _nameFilter = GUILayout.TextField(_nameFilter ?? string.Empty, GUILayout.MinWidth(120f));
            if (GUILayout.Button(ModLocalization.T("almanac.provider.relationship.apply"), GUILayout.Width(64f)))
                Refresh();
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            float scale = Mathf.Clamp(AlmanacConfig.StaticUIScale, 0.5f, 2.5f);

            foreach (RelationshipRow row in _rows)
            {
                DrawRow(row, scale);
                GUILayout.Space(6f);
            }
        }

        public bool DrawBriefingSection()
        {
            if (!HasBriefingContent)
                return false;

            int shown = 0;
            const int maxLines = 5;
            foreach (RelationshipRow row in _rows)
            {
                if (row.GiftedToday)
                    continue;

                GUILayout.Label(ModLocalization.T(
                    "almanac.provider.relationship.briefingRow",
                    row.DisplayName));
                shown++;
                if (shown >= maxLines)
                    break;
            }

            if (_ungiftedCount > shown)
            {
                GUILayout.Label(ModLocalization.T(
                    "almanac.provider.relationship.briefingMore",
                    _ungiftedCount - shown));
            }

            return true;
        }

        private void DrawRow(RelationshipRow row, float scale)
        {
            var layout = RelationshipHeartLayout.Dashboard;
            int maxHearts = RelationshipHeartRules.GetMaxHearts(row.Points);
            float gridW = RelationshipHeartRenderer.ScaledIconWidth(layout, scale) * RelationshipHeartRules.HeartsPerRow
                + Scaled(layout.Gutter, scale) * (RelationshipHeartRules.HeartsPerRow - 1);
            float gridH = RelationshipHeartRenderer.GridHeight(maxHearts, layout, scale);
            float rowH = Mathf.Max(gridH, GUI.skin.label.lineHeight);

            GUILayout.BeginHorizontal();
            GUILayout.Label(row.DisplayName, GUILayout.Height(rowH));
            GUILayout.Space(Scaled(8f, scale));

            GUILayout.BeginVertical(GUILayout.Width(gridW), GUILayout.Height(rowH));
            GUILayout.FlexibleSpace();
            Rect gridRect = GUILayoutUtility.GetRect(gridW, gridH, GUILayout.ExpandWidth(false));
            RelationshipHeartRenderer.DrawGrid(gridRect, row.Points, row.Romanceable, scale, layout);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawStatusBadges(row);
        }

        private static void DrawStatusBadges(RelationshipRow row)
        {
            if (row.IsPrimarySpouse)
                GUILayout.Label(ModLocalization.T("almanac.provider.relationship.spouse"));
            else if (row.IsMarriedTo)
                GUILayout.Label(ModLocalization.T("almanac.provider.relationship.married"));
            else if (row.IsDating)
                GUILayout.Label(ModLocalization.T("almanac.provider.relationship.dating"));

            if (!row.GiftedToday && AlmanacConfig.RelationshipsShowGiftBadge.Value)
                GUILayout.Label(ModLocalization.T("almanac.provider.relationship.ungifted"));
        }

        private void SortRows()
        {
            switch (AlmanacConfig.RelationshipsSortMode.Value)
            {
                case RelationshipSortMode.HeartsAsc:
                    _rows.Sort((a, b) => a.Points.CompareTo(b.Points));
                    break;
                case RelationshipSortMode.HeartsDesc:
                    _rows.Sort((a, b) => b.Points.CompareTo(a.Points));
                    break;
                case RelationshipSortMode.UngiftedFirst:
                    _rows.Sort((a, b) =>
                    {
                        int gifted = a.GiftedToday.CompareTo(b.GiftedToday);
                        return gifted != 0 ? gifted : string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
                    });
                    break;
                default:
                    _rows.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
                    break;
            }
        }

        private static float Scaled(float value, float scale) => value * scale;
    }
}
