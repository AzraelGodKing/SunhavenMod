using HavensAlmanac.Config;
using HavensAlmanac.Services;
using UnityEngine;

namespace HavensAlmanac.UI
{
    internal static class RelationshipHeartRenderer
    {
        private const float BgWidth = 16f;
        private const float BgHeight = 14f;
        private const float IconWidth = 22f;
        private const float IconHeight = 19f;
        private const float FillWidth = 12f;
        private const float FillHeight = 10f;
        private const float Gutter = 2f;

        public static float GridHeight(int maxHearts, float scale)
        {
            int rows = Mathf.Max(1, Mathf.CeilToInt(maxHearts / (float)RelationshipHeartRules.HeartsPerRow));
            float cellH = ScaledIconHeight(scale);
            return rows * cellH + Mathf.Max(0, rows - 1) * Scaled(Gutter, scale);
        }

        public static void DrawGrid(Rect origin, float points, bool romanceable, float scale)
        {
            RelationshipHeartAssetLoader.EnsureLoaded();
            int maxHearts = RelationshipHeartRules.GetMaxHearts(points);
            int fullHearts = RelationshipHeartRules.GetFullHearts(points, maxHearts);
            float cellW = ScaledIconWidth(scale);
            float cellH = ScaledIconHeight(scale);

            for (int slot = 0; slot < maxHearts; slot++)
            {
                int row = slot / RelationshipHeartRules.HeartsPerRow;
                int col = slot % RelationshipHeartRules.HeartsPerRow;
                var cell = new Rect(
                    origin.x + col * (cellW + Scaled(Gutter, scale)),
                    origin.y + row * (cellH + Scaled(Gutter, scale)),
                    cellW,
                    cellH);

                bool filled = slot < fullHearts;
                bool milestone = RelationshipHeartRules.IsMilestoneSlot(slot, romanceable, maxHearts);
                DrawSlot(cell, filled, milestone, scale);
            }
        }

        private static void DrawSlot(Rect cell, bool filled, bool milestone, float scale)
        {
            if (!RelationshipHeartAssetLoader.IsLoaded)
            {
                DrawFallback(cell, filled);
                return;
            }

            if (filled)
            {
                DrawTextureCentered(cell, RelationshipHeartAssetLoader.BgMilestone, Scaled(BgWidth, scale),
                    Scaled(BgHeight, scale));
                DrawTextureCentered(cell, RelationshipHeartAssetLoader.FillMilestone, Scaled(FillWidth, scale),
                    Scaled(FillHeight, scale));
                return;
            }

            Texture2D bg = milestone
                ? RelationshipHeartAssetLoader.BgMilestone
                : RelationshipHeartAssetLoader.BgStandard;
            DrawTextureCentered(cell, bg, Scaled(BgWidth, scale), Scaled(BgHeight, scale));
        }

        private static void DrawTexture(Rect rect, Texture2D tex)
        {
            if (tex == null)
                return;
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, true);
        }

        private static void DrawTextureCentered(Rect cell, Texture2D tex, float w, float h)
        {
            if (tex == null)
                return;
            var r = new Rect(
                cell.x + (cell.width - w) * 0.5f,
                cell.y + (cell.height - h) * 0.5f,
                w,
                h);
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);
        }

        private static void DrawFallback(Rect cell, bool filled)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(10, Mathf.RoundToInt(cell.height * 0.7f))
            };
            GUI.Label(cell, filled ? "\u2665" : "\u2661", style);
        }

        public static float ScaledIconWidth(float scale) => Scaled(IconWidth, scale);
        public static float ScaledIconHeight(float scale) => Scaled(IconHeight, scale);

        private static float Scaled(float value, float scale) => value * scale;
    }
}
