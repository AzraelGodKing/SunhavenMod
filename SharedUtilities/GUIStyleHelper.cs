using UnityEngine;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Utility methods for creating and caching GUIStyles and Textures.
    /// Reduces boilerplate and prevents texture recreation each frame.
    /// </summary>
    public static class GUIStyleHelper
    {
        /// <summary>
        /// Creates a solid color Texture2D (1x1 pixel).
        /// Remember to cache the result - don't call this every frame.
        /// </summary>
        public static Texture2D MakeSolidTexture(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Creates a Texture2D with a specified width and height, filled with a solid color.
        /// </summary>
        public static Texture2D MakeSolidTexture(int width, int height, Color color)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Creates a vertical gradient texture.
        /// </summary>
        /// <param name="height">Height of the texture in pixels</param>
        /// <param name="topColor">Color at the top</param>
        /// <param name="bottomColor">Color at the bottom</param>
        public static Texture2D MakeGradientTexture(int height, Color topColor, Color bottomColor)
        {
            var tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                tex.SetPixel(0, y, Color.Lerp(bottomColor, topColor, t));
            }
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Creates a texture with a border (outlined rectangle).
        /// </summary>
        public static Texture2D MakeBorderedTexture(int width, int height, Color fillColor, Color borderColor, int borderWidth = 1)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = x < borderWidth || x >= width - borderWidth ||
                                   y < borderWidth || y >= height - borderWidth;
                    pixels[y * width + x] = isBorder ? borderColor : fillColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Creates a basic window-style GUIStyle with the given background texture.
        /// </summary>
        public static GUIStyle CreateWindowStyle(Texture2D background, int padding = 10)
        {
            return new GUIStyle(GUI.skin.window)
            {
                normal = { background = background },
                onNormal = { background = background },
                padding = new RectOffset(padding, padding, padding, padding),
                border = new RectOffset(4, 4, 4, 4)
            };
        }

        /// <summary>
        /// Creates a label style with custom text color and optional background.
        /// </summary>
        public static GUIStyle CreateLabelStyle(Color textColor, int fontSize = 14, TextAnchor alignment = TextAnchor.MiddleLeft, Texture2D background = null)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = alignment,
                wordWrap = true,
                normal = { textColor = textColor }
            };

            if (background != null)
                style.normal.background = background;

            return style;
        }

        /// <summary>
        /// Creates a button style with custom colors.
        /// </summary>
        public static GUIStyle CreateButtonStyle(Color textColor, Texture2D normalBg, Texture2D hoverBg = null, int fontSize = 14)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = textColor, background = normalBg },
                hover = { textColor = textColor, background = hoverBg ?? normalBg },
                active = { textColor = textColor, background = normalBg }
            };
            return style;
        }

        /// <summary>
        /// Creates a text field style with custom colors.
        /// </summary>
        public static GUIStyle CreateTextFieldStyle(Color textColor, Color bgColor, int fontSize = 14, int padding = 4)
        {
            var bg = MakeSolidTexture(bgColor);
            return new GUIStyle(GUI.skin.textField)
            {
                fontSize = fontSize,
                padding = new RectOffset(padding, padding, padding, padding),
                normal = { textColor = textColor, background = bg },
                focused = { textColor = textColor, background = bg },
                hover = { textColor = textColor, background = bg }
            };
        }

        /// <summary>
        /// Creates a header/title style with larger font and bold text.
        /// </summary>
        public static GUIStyle CreateHeaderStyle(Color textColor, int fontSize = 18, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = alignment,
                normal = { textColor = textColor }
            };
        }

        /// <summary>
        /// Creates a scrollview style with a custom background.
        /// </summary>
        public static GUIStyle CreateScrollViewStyle(Texture2D background)
        {
            return new GUIStyle(GUI.skin.scrollView)
            {
                normal = { background = background }
            };
        }

        /// <summary>
        /// Common color palette for Sun Haven-themed UIs.
        /// </summary>
        public static class SunHavenColors
        {
            public static readonly Color Parchment = new Color(0.96f, 0.93f, 0.85f);
            public static readonly Color ParchmentDark = new Color(0.85f, 0.82f, 0.72f);
            public static readonly Color Wood = new Color(0.45f, 0.32f, 0.22f);
            public static readonly Color WoodLight = new Color(0.55f, 0.42f, 0.32f);
            public static readonly Color Gold = new Color(0.85f, 0.65f, 0.13f);
            public static readonly Color GoldDark = new Color(0.72f, 0.53f, 0.04f);
            public static readonly Color TextDark = new Color(0.2f, 0.15f, 0.1f);
            public static readonly Color TextLight = new Color(0.95f, 0.92f, 0.85f);
            public static readonly Color Success = new Color(0.2f, 0.6f, 0.2f);
            public static readonly Color Warning = new Color(0.8f, 0.6f, 0.2f);
            public static readonly Color Error = new Color(0.8f, 0.2f, 0.2f);
            public static readonly Color TransparentDark = new Color(0f, 0f, 0f, 0.7f);
            public static readonly Color TransparentLight = new Color(1f, 1f, 1f, 0.1f);
        }
    }
}
