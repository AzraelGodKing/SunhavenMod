using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CropOptimizer.UI
{
    /// <summary>
    /// Sun Haven-flavored palette + procedural sprites/icons for the HUD and hover tooltip.
    /// All textures are generated once at runtime — no asset files, no external dependencies.
    /// </summary>
    internal static class UiStyle
    {
        // Palette — Sun Haven-flavored dark card: warm wood border around a near-black fill so
        // cream/gold text stays readable over the game's colorful farm tiles. Avoids 9-slice
        // complexity by using solid fills + a simple nested border.
        public static readonly Color32 WoodBorder = new Color32(0x6B, 0x42, 0x22, 0xFF);
        public static readonly Color32 WoodBorderLit = new Color32(0xB0, 0x7A, 0x3E, 0xFF);
        public static readonly Color32 PanelFill = new Color32(0x24, 0x1A, 0x12, 0xF2);
        public static readonly Color32 HeaderFill = new Color32(0x3A, 0x28, 0x1A, 0xFF);
        public static readonly Color32 Divider = new Color32(0x6B, 0x42, 0x22, 0x80);
        public static readonly Color32 HeaderGold = new Color32(0xF7, 0xD9, 0x82, 0xFF);
        public static readonly Color32 TextDark = new Color32(0xF0, 0xE6, 0xCC, 0xFF);   // cream text for body rows (repurposed name)
        public static readonly Color32 TextSub = new Color32(0xB8, 0xA0, 0x78, 0xFF);
        public static readonly Color32 Shadow = new Color32(0x00, 0x00, 0x00, 0xB0);

        // Light accent colors kept for subtitles / muted labels (separate from panel/fill colors).
        public static readonly Color32 ParchmentLight = new Color32(0xEA, 0xD5, 0x9A, 0xFF);
        public static readonly Color32 ParchmentDeep = new Color32(0xC9, 0xAE, 0x75, 0xFF);

        // Quality stripe tints
        public static readonly Color32 QualityNormal = new Color32(0xCF, 0xBE, 0x8E, 0xFF);
        public static readonly Color32 QualitySilver = new Color32(0xCC, 0xD3, 0xD8, 0xFF);
        public static readonly Color32 QualityGold = new Color32(0xE8, 0xC3, 0x50, 0xFF);

        // Row-icon tints
        public static readonly Color32 Water = new Color32(0x4F, 0xB0, 0xE0, 0xFF);
        public static readonly Color32 Fertilizer = new Color32(0x7C, 0xB3, 0x42, 0xFF);
        public static readonly Color32 Mana = new Color32(0xB5, 0x6A, 0xE0, 0xFF);
        public static readonly Color32 Coin = new Color32(0xFF, 0xC2, 0x3C, 0xFF);
        public static readonly Color32 Clock = new Color32(0xD3, 0x5A, 0x2D, 0xFF);
        public static readonly Color32 Tile = new Color32(0x8B, 0x6A, 0x3E, 0xFF);
        public static readonly Color32 Sprout = new Color32(0x6E, 0xB8, 0x4A, 0xFF);

        private static Sprite _panelSprite;
        private static Sprite _headerSprite;
        private static Sprite _stripeSprite;
        private static readonly Dictionary<string, Sprite> IconCache = new Dictionary<string, Sprite>();
        private static TMP_FontAsset _font;

        /// <summary>9-slice wood-frame sprite with parchment fill. Use with <see cref="UnityEngine.UI.Image.Type.Sliced"/>.</summary>
        public static Sprite PanelSprite => _panelSprite ??= BuildPanelSprite();

        /// <summary>9-slice header strip (darker wood for the title bar). Draws against the panel top.</summary>
        public static Sprite HeaderSprite => _headerSprite ??= BuildHeaderSprite();

        /// <summary>Solid colored pixel for thin accent strips.</summary>
        public static Sprite SolidSprite => _stripeSprite ??= BuildSolidSprite();

        /// <summary>Sun Haven uses TMP; grab whatever default the game loaded. Null if TMP isn't initialized yet.</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                if (_font != null) return _font;
                try
                {
                    _font = TMP_Settings.defaultFontAsset;
                    if (_font == null)
                    {
                        var all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                        if (all != null && all.Length > 0)
                            _font = all[0];
                    }
                }
                catch { }
                return _font;
            }
        }

        // -------- Sprite builders --------

        // Panel + header are now plain 4x4 solid sprites. The "wood frame" look is built by
        // nesting a smaller inner Image inside a slightly larger outer Image at the view level.
        // This avoids any 9-slice scaling artifacts (the stretched-center rectangles that were
        // showing up over body rows in the previous iteration).
        private static Sprite BuildPanelSprite()
        {
            return BuildSolidColoredSprite(Color.white);
        }

        private static Sprite BuildHeaderSprite()
        {
            return BuildSolidColoredSprite(Color.white);
        }

        private static Sprite BuildSolidColoredSprite(Color c)
        {
            Texture2D tex = NewTexture(4, 4);
            var px = new Color32[16];
            Color32 c32 = c;
            for (int i = 0; i < px.Length; i++) px[i] = c32;
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }

        private static Sprite BuildSolidSprite()
        {
            return BuildSolidColoredSprite(Color.white);
        }

        // -------- Icons (16x16 procedural pixel art, tintable via Image.color) --------

        public enum IconKind
        {
            Sprout,
            Water,
            Fertilizer,
            Mana,
            Coin,
            Clock,
            Tile,
            Quality,
            Ready,
            Tooltip,
            TooltipOff,
        }

        public static Sprite GetIcon(IconKind kind)
        {
            string key = kind.ToString();
            if (IconCache.TryGetValue(key, out Sprite s) && s != null) return s;
            s = BuildIcon(kind);
            IconCache[key] = s;
            return s;
        }

        private static Sprite BuildIcon(IconKind kind)
        {
            const int size = 16;
            var px = new Color32[size * size];
            Color32 fg = Color.white;
            Color32 outline = new Color32(0, 0, 0, 0xC0);
            switch (kind)
            {
                case IconKind.Sprout: DrawSprout(px, size, fg, outline); break;
                case IconKind.Water: DrawWaterDrop(px, size, fg, outline); break;
                case IconKind.Fertilizer: DrawLeaf(px, size, fg, outline); break;
                case IconKind.Mana: DrawStar(px, size, fg, outline); break;
                case IconKind.Coin: DrawCoin(px, size, fg, outline); break;
                case IconKind.Clock: DrawClock(px, size, fg, outline); break;
                case IconKind.Tile: DrawTile(px, size, fg, outline); break;
                case IconKind.Quality: DrawDiamond(px, size, fg, outline); break;
                case IconKind.Ready: DrawCheck(px, size, fg, outline); break;
                case IconKind.Tooltip: DrawTooltip(px, size, fg, outline, strike: false); break;
                case IconKind.TooltipOff: DrawTooltip(px, size, fg, outline, strike: true); break;
            }

            Texture2D tex = NewTexture(size, size);
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        // Each draw function fills a 16x16 pixel buffer with a shape outlined in `outline` and filled in `fg`.

        private static void DrawWaterDrop(Color32[] px, int s, Color32 fg, Color32 outline)
        {
            // Teardrop: point at top (y=1), round bottom around (8, 11)
            int cx = 8, cy = 10, rx = 4, ry = 5;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - cx) / (float)rx;
                    float dy = (y - cy) / (float)ry;
                    float d = dx * dx + dy * dy;

                    if (y < cy && Mathf.Abs(x - cx) <= Mathf.RoundToInt((y - 1) * 0.55f))
                    {
                        Plot(px, s, x, y, fg, outline, Mathf.Abs(x - cx) == Mathf.RoundToInt((y - 1) * 0.55f));
                        continue;
                    }

                    if (d <= 1.0f) Plot(px, s, x, y, fg, outline, d > 0.75f);
                }
            }
        }

        private static void DrawLeaf(Color32[] px, int s, Color32 fg, Color32 outline)
        {
            // Diagonal leaf shape with central vein
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float u = x / (float)(s - 1);
                    float v = y / (float)(s - 1);
                    float d = Mathf.Abs((u + v) - 1f);
                    float t = Mathf.Abs(u - v);
                    if (d < 0.55f && t < 0.55f)
                    {
                        bool edge = d > 0.48f || t > 0.48f;
                        Plot(px, s, x, y, fg, outline, edge);
                        if (Mathf.Abs(u + v - 1f) < 0.05f && Mathf.Abs(u - v) < 0.45f)
                            px[y * s + x] = outline;
                    }
                }
            }
        }

        private static void DrawStar(Color32[] px, int s, Color32 fg, Color32 outline)
        {
            // 4-point star / sparkle
            int cx = s / 2, cy = s / 2;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int dx = Mathf.Abs(x - cx);
                    int dy = Mathf.Abs(y - cy);
                    int m = dx + dy;
                    if (m <= 6 && (dx <= 1 || dy <= 1))
                    {
                        bool edge = m == 6 || (dx == 1 && dy >= 1) || (dy == 1 && dx >= 1);
                        Plot(px, s, x, y, fg, outline, edge);
                    }
                }
            }
        }

        private static void DrawCoin(Color32[] px, int s, Color32 fg, Color32 outline)
        {
            int cx = s / 2, cy = s / 2, r = s / 2 - 1;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    int d2 = dx * dx + dy * dy;
                    if (d2 <= r * r)
                    {
                        bool edge = d2 >= (r - 1) * (r - 1);
                        Plot(px, s, x, y, fg, outline, edge);
                    }
                }
            }
            // "$" glyph center
            int mx = cx;
            for (int y = cy - 3; y <= cy + 3; y++)
                if (y >= 0 && y < s) px[y * s + mx] = outline;
            px[(cy - 2) * s + (mx - 1)] = outline;
            px[(cy - 2) * s + (mx - 2)] = outline;
            px[cy * s + (mx + 1)] = outline;
            px[cy * s + (mx + 2)] = outline;
            px[(cy + 2) * s + (mx - 1)] = outline;
            px[(cy + 2) * s + (mx - 2)] = outline;
        }

        private static void DrawClock(Color32[] px, int s, Color32 fg, Color32 outline)
        {
            int cx = s / 2, cy = s / 2, r = s / 2 - 1;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    int d2 = dx * dx + dy * dy;
                    if (d2 <= r * r)
                    {
                        bool edge = d2 >= (r - 1) * (r - 1);
                        Plot(px, s, x, y, fg, outline, edge);
                    }
                }
            }
            // hands
            for (int i = 0; i < 3; i++) px[(cy + i) * s + cx] = outline;
            for (int i = 0; i < 4; i++) px[cy * s + (cx + i)] = outline;
        }

        private static void DrawTile(Color32[] px, int s, Color32 fg, Color32 outline)
        {
            // Isometric diamond (tile)
            int cx = s / 2, cy = s / 2;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int dx = Mathf.Abs(x - cx);
                    int dy = Mathf.Abs(y - cy);
                    if (dx + dy * 2 <= s - 2)
                    {
                        bool edge = dx + dy * 2 >= s - 3;
                        Plot(px, s, x, y, fg, outline, edge);
                    }
                }
            }
        }

        private static void DrawDiamond(Color32[] px, int s, Color32 fg, Color32 outline)
        {
            int cx = s / 2, cy = s / 2;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int m = Mathf.Abs(x - cx) + Mathf.Abs(y - cy);
                    if (m <= 6)
                    {
                        Plot(px, s, x, y, fg, outline, m == 6);
                    }
                }
            }
        }

        private static void DrawCheck(Color32[] px, int s, Color32 fg, Color32 outline)
        {
            // simple checkmark stroke
            int[,] pts =
            {
                { 3, 8 }, { 4, 9 }, { 5, 10 }, { 6, 11 }, { 7, 10 }, { 8, 9 },
                { 9, 8 }, { 10, 7 }, { 11, 6 }, { 12, 5 },
            };
            for (int i = 0; i < pts.GetLength(0); i++)
            {
                int x = pts[i, 0], y = pts[i, 1];
                px[y * s + x] = fg;
                px[y * s + x - 1] = outline;
                if (y + 1 < s) px[(y + 1) * s + x] = outline;
            }
        }

        private static void DrawTooltip(Color32[] px, int s, Color32 fg, Color32 outline, bool strike)
        {
            // Speech-bubble: rounded rect (x=2..13, y=3..10) with a little tail pointing down-left.
            for (int y = 3; y <= 10; y++)
            {
                for (int x = 2; x <= 13; x++)
                {
                    bool corner = (x <= 2 && y <= 3) || (x >= 13 && y <= 3)
                               || (x <= 2 && y >= 10) || (x >= 13 && y >= 10);
                    if (corner) continue;
                    bool edge = x == 2 || x == 13 || y == 3 || y == 10;
                    Plot(px, s, x, y, fg, outline, edge);
                }
            }
            // Tail
            px[11 * s + 5] = outline;
            px[11 * s + 6] = fg;
            px[12 * s + 5] = outline;

            // Three "dots" inside the bubble — the universal tooltip/more indicator.
            px[7 * s + 5] = outline;
            px[7 * s + 8] = outline;
            px[7 * s + 11] = outline;

            if (strike)
            {
                // Diagonal slash across the bubble
                for (int i = 0; i < s; i++)
                {
                    int x = i;
                    int y = s - 1 - i;
                    if (x < 1 || x >= s - 1 || y < 1 || y >= s - 1) continue;
                    px[y * s + x] = outline;
                }
            }
        }

        private static void DrawSprout(Color32[] px, int s, Color32 fg, Color32 outline)
        {
            int cx = s / 2;
            // stem
            for (int y = 8; y < s - 1; y++)
            {
                px[y * s + cx] = fg;
                px[y * s + cx - 1] = outline;
                px[y * s + cx + 1] = outline;
            }
            // left leaf
            int[,] left = { { 4, 7 }, { 3, 8 }, { 4, 8 }, { 5, 8 }, { 5, 9 }, { 6, 9 } };
            for (int i = 0; i < left.GetLength(0); i++)
            {
                int x = left[i, 0], y = left[i, 1];
                px[y * s + x] = fg;
            }
            // right leaf
            int[,] right = { { 11, 7 }, { 12, 8 }, { 11, 8 }, { 10, 8 }, { 10, 9 }, { 9, 9 } };
            for (int i = 0; i < right.GetLength(0); i++)
            {
                int x = right[i, 0], y = right[i, 1];
                px[y * s + x] = fg;
            }
        }

        private static void Plot(Color32[] px, int s, int x, int y, Color32 fg, Color32 outline, bool edge)
        {
            if (x < 0 || x >= s || y < 0 || y >= s) return;
            px[y * s + x] = edge ? outline : fg;
        }

        private static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
        }
    }
}
