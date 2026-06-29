using System.Collections.Generic;
using GiftingAssistant.Data;
using UnityEngine;

namespace GiftingAssistant.UI
{
    /// <summary>
    /// Builds and owns the Sun Haven-themed IMGUI styles/textures for the gifting window.
    /// Rebuilt whenever the UI scale changes; textures are destroyed on rebuild/teardown.
    /// </summary>
    internal sealed class UiStyle
    {
        public float Scale { get; private set; } = 1f;

        // Palette
        private readonly Color _parchmentLight = new Color(0.96f, 0.93f, 0.86f, 0.98f);
        private readonly Color _parchment = new Color(0.92f, 0.87f, 0.78f, 0.97f);
        private readonly Color _parchmentDark = new Color(0.85f, 0.78f, 0.65f, 0.95f);
        private readonly Color _woodDark = new Color(0.35f, 0.25f, 0.15f);
        private readonly Color _woodMedium = new Color(0.50f, 0.38f, 0.25f);
        private readonly Color _woodLight = new Color(0.65f, 0.52f, 0.38f);
        private readonly Color _goldRich = new Color(0.85f, 0.68f, 0.20f);
        private readonly Color _goldBright = new Color(0.95f, 0.80f, 0.30f);
        private readonly Color _goldPale = new Color(0.98f, 0.95f, 0.85f);
        private readonly Color _successGreen = new Color(0.35f, 0.65f, 0.35f);
        private readonly Color _textDark = new Color(0.25f, 0.20f, 0.15f);
        private readonly Color _borderDark = new Color(0.40f, 0.30f, 0.20f, 0.8f);

        public Color TextDark => _textDark;
        public Color WoodDark => _woodDark;
        public Color SuccessGreen => _successGreen;

        public readonly Dictionary<GiftPriority, Color> PriorityColors = new Dictionary<GiftPriority, Color>
        {
            { GiftPriority.Low, new Color(0.50f, 0.60f, 0.70f) },
            { GiftPriority.Normal, new Color(0.45f, 0.55f, 0.45f) },
            { GiftPriority.High, new Color(0.85f, 0.65f, 0.25f) },
            { GiftPriority.Urgent, new Color(0.80f, 0.30f, 0.25f) }
        };

        // Styles
        public GUIStyle Window;
        public GUIStyle Panel;
        public GUIStyle Title;
        public GUIStyle Header;
        public GUIStyle Label;
        public GUIStyle LabelBold;
        public GUIStyle Stats;
        public GUIStyle Footer;
        public GUIStyle Button;
        public GUIStyle TextField;
        public GUIStyle Tab;
        public GUIStyle TabActive;
        public GUIStyle Row;
        public GUIStyle PriorityBadge;
        public GUIStyle Checkbox;

        // Textures
        public Texture2D GoldLine;
        public Texture2D GiftSelectorRowSelected;
        private Texture2D _windowBg;
        private Texture2D _panelBg;
        private Texture2D _buttonNormal;
        private Texture2D _buttonHover;
        private Texture2D _buttonActive;
        private Texture2D _tabNormal;
        private Texture2D _tabActive;
        private Texture2D _textFieldBg;
        private Texture2D _rowEven;
        private Texture2D _rowOdd;
        private Texture2D _rowGifted;
        private Texture2D _rowSelected;
        private Texture2D _checkboxNormal;
        private Texture2D _checkboxChecked;

        public int Font(int baseSize) => Mathf.Max(8, Mathf.RoundToInt(baseSize * Scale));
        public float S(float value) => value * Scale;
        public int SInt(float value) => Mathf.RoundToInt(value * Scale);

        public Texture2D RowBackground(int index, bool gifted)
        {
            if (gifted)
                return _rowGifted;
            return index % 2 == 0 ? _rowEven : _rowOdd;
        }

        public void Build(float scale)
        {
            Scale = Mathf.Clamp(scale, 0.5f, 2.5f);
            DestroyTextures();
            CreateTextures();
            CreateStyles();
        }

        private void CreateTextures()
        {
            _windowBg = MakeRoundedRect(32, 32, _parchment, _borderDark, 4);
            _panelBg = MakeRoundedRect(16, 16, _parchmentLight, _borderDark, 2);
            _buttonNormal = MakeRoundedRect(8, 8, _parchmentDark, _borderDark, 2);
            _buttonHover = MakeRoundedRect(8, 8, _woodLight, _borderDark, 2);
            _buttonActive = MakeRoundedRect(8, 8, _goldPale, _goldRich, 2);
            _tabNormal = MakeRoundedRect(8, 8, _parchmentDark, _borderDark, 1);
            _tabActive = MakeRoundedRect(8, 8, _goldPale, _goldRich, 2);
            _textFieldBg = MakeSolid(new Color(1f, 1f, 1f, 0.9f));
            _rowEven = MakeSolid(new Color(_parchmentLight.r, _parchmentLight.g, _parchmentLight.b, 0.4f));
            _rowOdd = MakeSolid(new Color(_parchment.r, _parchment.g, _parchment.b, 0.4f));
            _rowGifted = MakeSolid(new Color(_successGreen.r, _successGreen.g, _successGreen.b, 0.18f));
            _rowSelected = MakeSolid(new Color(_goldPale.r, _goldPale.g, _goldPale.b, 0.55f));
            _checkboxNormal = MakeRoundedRect(8, 8, _parchmentLight, _woodMedium, 2);
            _checkboxChecked = MakeRoundedRect(8, 8, _goldPale, _goldRich, 2);
            GiftSelectorRowSelected = _rowSelected;
            GoldLine = MakeGradient(64, 3, _goldBright, _goldRich);
        }

        private void CreateStyles()
        {
            Window = new GUIStyle
            {
                normal = { background = _windowBg, textColor = _textDark },
                padding = new RectOffset(SInt(15), SInt(15), SInt(15), SInt(15)),
                border = new RectOffset(SInt(8), SInt(8), SInt(8), SInt(8))
            };

            Panel = new GUIStyle
            {
                normal = { background = _panelBg, textColor = _textDark },
                padding = new RectOffset(SInt(10), SInt(10), SInt(10), SInt(10)),
                border = new RectOffset(SInt(6), SInt(6), SInt(6), SInt(6))
            };

            Title = new GUIStyle
            {
                fontSize = Font(22),
                fontStyle = FontStyle.Bold,
                normal = { textColor = _woodDark },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(SInt(5), SInt(5), SInt(5), SInt(5))
            };

            Header = new GUIStyle
            {
                fontSize = Font(15),
                fontStyle = FontStyle.Bold,
                normal = { textColor = _woodDark },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(SInt(4), SInt(4), SInt(4), SInt(4))
            };

            Label = new GUIStyle
            {
                fontSize = Font(12),
                normal = { textColor = _textDark },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(SInt(2), SInt(2), SInt(2), SInt(2))
            };

            LabelBold = new GUIStyle(Label) { fontStyle = FontStyle.Bold };

            Stats = new GUIStyle(Label)
            {
                fontSize = Font(11),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = _woodMedium }
            };

            Footer = new GUIStyle(Label)
            {
                fontSize = Font(10),
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(_textDark.r, _textDark.g, _textDark.b, 0.6f) }
            };

            Button = new GUIStyle
            {
                fontSize = Font(11),
                fontStyle = FontStyle.Bold,
                normal = { background = _buttonNormal, textColor = _textDark },
                hover = { background = _buttonHover, textColor = _textDark },
                active = { background = _buttonActive, textColor = _woodDark },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(SInt(8), SInt(8), SInt(4), SInt(4)),
                border = new RectOffset(SInt(4), SInt(4), SInt(4), SInt(4))
            };

            TextField = new GUIStyle
            {
                fontSize = Font(12),
                normal = { background = _textFieldBg, textColor = _textDark },
                focused = { background = _textFieldBg, textColor = _textDark },
                padding = new RectOffset(SInt(6), SInt(6), SInt(4), SInt(4)),
                border = new RectOffset(SInt(2), SInt(2), SInt(2), SInt(2))
            };

            Tab = new GUIStyle
            {
                fontSize = Font(10),
                normal = { background = _tabNormal, textColor = _textDark },
                hover = { background = _buttonHover, textColor = _textDark },
                active = { background = _tabActive, textColor = _woodDark },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(SInt(6), SInt(6), SInt(3), SInt(3)),
                margin = new RectOffset(SInt(2), SInt(2), 0, 0),
                border = new RectOffset(SInt(4), SInt(4), SInt(4), SInt(4))
            };

            TabActive = new GUIStyle(Tab)
            {
                normal = { background = _tabActive, textColor = _woodDark },
                fontStyle = FontStyle.Bold
            };

            Row = new GUIStyle
            {
                padding = new RectOffset(SInt(8), SInt(8), SInt(6), SInt(6)),
                margin = new RectOffset(0, 0, SInt(2), SInt(2))
            };

            PriorityBadge = new GUIStyle(LabelBold)
            {
                fontSize = Font(11),
                alignment = TextAnchor.MiddleCenter
            };

            Checkbox = new GUIStyle
            {
                fontSize = Font(14),
                fontStyle = FontStyle.Bold,
                normal = { background = _checkboxNormal, textColor = _woodDark },
                hover = { background = _buttonHover, textColor = _woodDark },
                onNormal = { background = _checkboxChecked, textColor = _successGreen },
                onHover = { background = _checkboxChecked, textColor = _successGreen },
                onActive = { background = _checkboxChecked, textColor = _woodDark },
                active = { background = _buttonActive, textColor = _woodDark },
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                padding = new RectOffset(SInt(2), SInt(2), SInt(2), SInt(2)),
                border = new RectOffset(SInt(3), SInt(3), SInt(3), SInt(3))
            };
        }

        public void Destroy()
        {
            DestroyTextures();
        }

        private void DestroyTextures()
        {
            DestroyTex(ref _windowBg);
            DestroyTex(ref _panelBg);
            DestroyTex(ref _buttonNormal);
            DestroyTex(ref _buttonHover);
            DestroyTex(ref _buttonActive);
            DestroyTex(ref _tabNormal);
            DestroyTex(ref _tabActive);
            DestroyTex(ref _textFieldBg);
            DestroyTex(ref _rowEven);
            DestroyTex(ref _rowOdd);
            DestroyTex(ref _rowGifted);
            DestroyTex(ref _rowSelected);
            DestroyTex(ref _checkboxNormal);
            DestroyTex(ref _checkboxChecked);
            GiftSelectorRowSelected = null;
            DestroyTex(ref GoldLine);
        }

        private static void DestroyTex(ref Texture2D tex)
        {
            if (tex != null)
            {
                UnityEngine.Object.Destroy(tex);
                tex = null;
            }
        }

        private static Texture2D MakeSolid(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeGradient(int width, int height, Color top, Color bottom)
        {
            var tex = new Texture2D(width, height);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float t = height > 1 ? (float)y / (height - 1) : 0f;
                Color row = Color.Lerp(top, bottom, t);
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = row;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeRoundedRect(int width, int height, Color fill, Color border, int borderWidth)
        {
            var tex = new Texture2D(width, height);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = x < borderWidth || x >= width - borderWidth ||
                                    y < borderWidth || y >= height - borderWidth;
                    pixels[y * width + x] = isBorder ? border : fill;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
