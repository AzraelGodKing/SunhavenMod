namespace SunhavenMods.Shared
{
    /// <summary>Sprite dimensions for relationship heart IMGUI grids.</summary>
    public readonly struct RelationshipHeartLayout
    {
        public float BgWidth { get; }
        public float BgHeight { get; }
        public float IconWidth { get; }
        public float IconHeight { get; }
        public float FillWidth { get; }
        public float FillHeight { get; }
        public float Gutter { get; }

        public RelationshipHeartLayout(
            float bgWidth,
            float bgHeight,
            float iconWidth,
            float iconHeight,
            float fillWidth,
            float fillHeight,
            float gutter)
        {
            BgWidth = bgWidth;
            BgHeight = bgHeight;
            IconWidth = iconWidth;
            IconHeight = iconHeight;
            FillWidth = fillWidth;
            FillHeight = fillHeight;
            Gutter = gutter;
        }

        /// <summary>5-wide dashboard grid (Haven's Almanac).</summary>
        public static RelationshipHeartLayout Dashboard { get; } = new RelationshipHeartLayout(16f, 14f, 22f, 19f, 12f, 10f, 2f);

        /// <summary>Single-row inline strip (Gifting Assistant).</summary>
        public static RelationshipHeartLayout Compact { get; } = new RelationshipHeartLayout(8f, 7f, 12f, 10f, 12f, 10f, 2f);
    }
}
