using UnityEngine;

namespace SunhavenMods.Shared
{
    public static class RelationshipHeartRenderer
    {
        private static GUIStyle _fallbackStyle;

        public static float GridWidth(int maxHearts, RelationshipHeartLayout layout, float scale, bool singleRow = false)
        {
            int cols = singleRow ? maxHearts : Mathf.Min(RelationshipHeartRules.HeartsPerRow, maxHearts);
            float cellW = ScaledIconWidth(layout, scale);
            return cols * cellW + Mathf.Max(0, cols - 1) * Scaled(layout.Gutter, scale);
        }

        public static float GridHeight(int maxHearts, RelationshipHeartLayout layout, float scale, bool singleRow = false)
        {
            if (singleRow)
                return ScaledIconHeight(layout, scale);

            int rows = Mathf.Max(1, Mathf.CeilToInt(maxHearts / (float)RelationshipHeartRules.HeartsPerRow));
            float cellH = ScaledIconHeight(layout, scale);
            return rows * cellH + Mathf.Max(0, rows - 1) * Scaled(layout.Gutter, scale);
        }

        public static void DrawGrid(
            Rect origin,
            float points,
            bool romanceable,
            float scale,
            RelationshipHeartLayout layout,
            bool singleRow = false)
        {
            RelationshipHeartAssetLoader.EnsureLoaded();
            int maxHearts = RelationshipHeartRules.GetMaxHearts(points);
            int fullHearts = RelationshipHeartRules.GetFullHearts(points, maxHearts);
            float cellW = ScaledIconWidth(layout, scale);
            float cellH = ScaledIconHeight(layout, scale);

            for (int slot = 0; slot < maxHearts; slot++)
            {
                int row = singleRow ? 0 : slot / RelationshipHeartRules.HeartsPerRow;
                int col = singleRow ? slot : slot % RelationshipHeartRules.HeartsPerRow;
                var cell = new Rect(
                    origin.x + col * (cellW + Scaled(layout.Gutter, scale)),
                    origin.y + row * (cellH + Scaled(layout.Gutter, scale)),
                    cellW,
                    cellH);

                bool filled = slot < fullHearts;
                bool milestone = RelationshipHeartRules.IsMilestoneSlot(slot, romanceable, maxHearts);
                DrawSlot(cell, filled, milestone, scale, layout);
            }
        }

        public static float ScaledIconWidth(RelationshipHeartLayout layout, float scale) =>
            Scaled(layout.IconWidth, scale);

        public static float ScaledIconHeight(RelationshipHeartLayout layout, float scale) =>
            Scaled(layout.IconHeight, scale);

        private static void DrawSlot(Rect cell, bool filled, bool milestone, float scale, RelationshipHeartLayout layout)
        {
            if (!RelationshipHeartAssetLoader.IsLoaded)
            {
                DrawFallback(cell, filled);
                return;
            }

            Texture2D bg = milestone
                ? RelationshipHeartAssetLoader.BgMilestone
                : RelationshipHeartAssetLoader.BgStandard;
            DrawTextureCentered(cell, bg, Scaled(layout.BgWidth, scale), Scaled(layout.BgHeight, scale));

            if (filled)
            {
                Texture2D fill = milestone
                    ? RelationshipHeartAssetLoader.FillMilestone
                    : RelationshipHeartAssetLoader.FillStandard;
                DrawTextureCentered(cell, fill, Scaled(layout.FillWidth, scale), Scaled(layout.FillHeight, scale));
            }
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
            if (_fallbackStyle == null)
            {
                _fallbackStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }
            _fallbackStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(cell.height * 0.7f));
            GUI.Label(cell, filled ? "\u2665" : "\u2661", _fallbackStyle);
        }

        private static float Scaled(float value, float scale) => value * scale;
    }
}
