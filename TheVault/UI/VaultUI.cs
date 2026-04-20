using System;
using System.Collections.Generic;
using SunhavenMods.Shared;
using TheVault;
using TheVault.Patches;
using TheVault.Vault;
using UnityEngine;
using Wish;

namespace TheVault.UI
{
    /// <summary>
    /// Unity IMGUI-based vault interface.
    /// Displays all stored currencies and allows deposit/withdraw operations.
    /// </summary>
    public class VaultUI : MonoBehaviour
    {
        private VaultManager _vaultManager;
        private bool _isVisible;
        private Rect _windowRect;
        private Vector2 _scrollPosition;
        private Vector2 _debugVaultScroll;

        // UI State
        private CurrencyCategory _selectedCategory = CurrencyCategory.SeasonalToken;
        private string _depositAmount = "1";
        private string _selectedCurrencyId = "";
        private bool _showDebugVaultDump;

        // Styling
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _withdrawButtonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _categoryButtonStyle;
        private GUIStyle _selectedCategoryStyle;
        private GUIStyle _rowStyle;
        private GUIStyle _closeButtonStyle;
        private GUIStyle _textFieldStyle;
        private Texture2D _windowBackground;
        private Texture2D _rowBackground;
        private Texture2D _rowAltBackground;
        private Texture2D _buttonNormal;
        private Texture2D _buttonHover;
        private Texture2D _withdrawNormal;
        private Texture2D _withdrawHover;
        private Texture2D _tabNormal;
        private Texture2D _tabSelected;
        private Texture2D _headerBarBg;
        private GUIStyle _headerBarStyle;

        // Colors - Enhanced color scheme
        private readonly Color _windowBgColor = new Color(0.12f, 0.12f, 0.18f, 0.98f);
        private readonly Color _headerGradientStart = new Color(0.25f, 0.45f, 0.65f);
        private readonly Color _headerGradientEnd = new Color(0.15f, 0.30f, 0.50f);
        private readonly Color _accentColor = new Color(0.4f, 0.7f, 0.95f);
        private readonly Color _goldColor = new Color(0.95f, 0.8f, 0.3f);
        private readonly Color _withdrawColor = new Color(0.7f, 0.35f, 0.35f);
        private readonly Color _withdrawHoverColor = new Color(0.85f, 0.4f, 0.4f);
        private readonly Color _rowEvenColor = new Color(0.15f, 0.15f, 0.22f, 0.9f);
        private readonly Color _rowOddColor = new Color(0.18f, 0.18f, 0.26f, 0.9f);
        private readonly Color _textColor = new Color(0.9f, 0.9f, 0.95f);
        private readonly Color _textDimColor = new Color(0.6f, 0.6f, 0.7f);

        // Cached styles and textures for DrawCurrencyRow (to avoid GC allocations every frame)
        private GUIStyle _subtitleStyle;
        private GUIStyle _emptyStyle;
        private GUIStyle _toggleOnStyle;
        private GUIStyle _toggleOffStyle;
        private GUIStyle _iconFallbackStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _selectedNameStyle;
        private GUIStyle _amountLabelStyle;
        private GUIStyle _statusOkStyle;
        private GUIStyle _statusErrorStyle;
        private string _vaultStatusMessage;
        private bool _vaultStatusIsError;
        private float _vaultStatusUntil;
        private Texture2D _toggleOnBg;
        private Texture2D _toggleOnHover;
        private Texture2D _toggleOffBg;
        private Texture2D _toggleOffHover;

        // Window dimensions (base values, scaled by _scale)
        private const float BASE_WINDOW_WIDTH = 520f;
        private const float BASE_ROW_HEIGHT = 40f;
        private const float BASE_HEADER_HEIGHT = 118f;
        private const float BASE_FOOTER_HEIGHT = 200f;
        private const float BASE_MIN_CONTENT_HEIGHT = 120f;
        private const float BASE_MAX_CONTENT_HEIGHT = 420f;
        private const float BASE_SETTINGS_CONTENT_HEIGHT = 340f;

        // UI scale (from config, 0.5-3.0)
        private float _scale = 1f;

        /// <summary>GUIStyle/GUI.skin may only be touched inside OnGUI; defer full style build until then.</summary>
        private bool _stylesDirty = true;

        private float WindowWidth => BASE_WINDOW_WIDTH * _scale;
        private float RowHeight => BASE_ROW_HEIGHT * _scale;
        private float HeaderHeight => BASE_HEADER_HEIGHT * _scale;
        private float FooterHeight => BASE_FOOTER_HEIGHT * _scale;
        private float MinContentHeight => BASE_MIN_CONTENT_HEIGHT * _scale;
        private float MaxContentHeight => BASE_MAX_CONTENT_HEIGHT * _scale;
        private float SettingsContentHeight => BASE_SETTINGS_CONTENT_HEIGHT * _scale;

        private int ScaledFont(int baseSize) => Mathf.Max(8, Mathf.RoundToInt(baseSize * _scale));
        private float Scaled(float value) => value * _scale;
        private int ScaledInt(float value) => Mathf.RoundToInt(value * _scale);

        // Toggle key
        private KeyCode _toggleKey = KeyCode.V;
        private bool _requiresModifier = true; // Requires Ctrl+V by default
        private KeyCode _altToggleKey = KeyCode.F8; // Alternative key for Steam Deck (no modifier)

        // Settings panel (in-game config editing)
        private bool _showSettings;
        private Vector2 _settingsScroll;
        private static readonly KeyCode[] SettingsKeyOptions = new[]
        {
            KeyCode.None, KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6,
            KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12,
            KeyCode.V, KeyCode.B, KeyCode.G, KeyCode.K, KeyCode.U, KeyCode.H
        };
        private static readonly string[] SettingsKeyNames = new[]
        {
            "None", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
            "V", "B", "G", "K", "U", "H"
        };

        public void Initialize(VaultManager vaultManager)
        {
            _vaultManager = vaultManager;
            _isVisible = false;

            // Center window on screen (initial size)
            float w = WindowWidth;
            float initialHeight = HeaderHeight + Scaled(260f) + FooterHeight;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - initialHeight) / 2f;
            _windowRect = new Rect(x, y, w, initialHeight);

            Plugin.Log?.LogInfo("VaultUI initialized");
        }

        public void SetToggleKey(KeyCode key, bool requireModifier = true)
        {
            _toggleKey = key;
            _requiresModifier = requireModifier;
        }

        public void SetAltToggleKey(KeyCode key)
        {
            _altToggleKey = key;
        }

        public void SetScale(float scale)
        {
            _scale = Mathf.Clamp(scale, 0.5f, 3.0f);
            _stylesDirty = true;
            _windowStyle = null;
            float w = WindowWidth;
            float h = HeaderHeight + 260f * _scale + FooterHeight;
            _windowRect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        }

        public bool IsVisible => _isVisible;

        // Unique identifier for pause object system
        private const string PAUSE_ID = "TheVault_UI";

        public void Show()
        {
            _isVisible = true;

            // Block game input while vault is open
            try
            {
                if (Player.Instance != null)
                {
                    Player.Instance.AddPauseObject(PAUSE_ID);
                }

                PlayerInput.DisableInput(PAUSE_ID);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Could not block game input: {ex.Message}");
            }

            Plugin.Log?.LogInfo("Vault UI opened");

            // Log icon cache status when opening
            IconCache.LogStatus();

            // Check for secret gifts on first open
            SecretGifts.CheckAndGiveFirstOpenGift(PlayerPatches.LoadedCharacterName);
        }

        public void Hide()
        {
            // Always clear pause/input if we were open (e.g. switching UI mode).
            if (!_isVisible) return;
            _isVisible = false;

            try
            {
                if (Player.Instance != null)
                    Player.Instance.RemovePauseObject(PAUSE_ID);

                PlayerInput.EnableInput(PAUSE_ID);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Could not re-enable game input: {ex.Message}");
            }

            Plugin.Log?.LogInfo("Vault UI closed");
        }

        public void Toggle()
        {
            // Don't allow toggle until vault is loaded
            if (!PlayerPatches.IsVaultLoaded)
            {
                Plugin.Log?.LogInfo("Cannot toggle vault UI - save not yet loaded");
                return;
            }

            if (_isVisible)
                Hide();
            else
                Show();
        }

        private void OnDestroy()
        {
            // Make sure to re-enable input if the component is destroyed while visible
            if (_isVisible)
            {
                Hide();
            }
        }

        private void Awake()
        {
            _stylesDirty = true;
            _windowStyle = null;
        }

        private void OnEnable()
        {
            _stylesDirty = true;
            _windowStyle = null;
        }

        private void Update()
        {
            if (!string.IsNullOrEmpty(_vaultStatusMessage) && Time.unscaledTime > _vaultStatusUntil)
                _vaultStatusMessage = null;

            // NOTE: Hotkey detection is now handled by PersistentRunner to avoid double-toggle.
            // PersistentRunner survives game cleanup and calls our Toggle() method directly.
            // We only handle Escape here since it's specific to closing this UI when visible.

            // Close on Escape
            if (_isVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        private void SetVaultStatus(string message, bool isError)
        {
            _vaultStatusMessage = message;
            _vaultStatusIsError = isError;
            _vaultStatusUntil = Time.unscaledTime + 8f;
        }

        private Texture2D MakeTex(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            var tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeGradientTex(int width, int height, Color top, Color bottom)
        {
            var tex = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
            {
                Color color = Color.Lerp(bottom, top, (float)y / height);
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, color);
            }
            tex.Apply();
            return tex;
        }

        private Texture2D MakeRoundedTex(int width, int height, Color color, int radius)
        {
            var tex = new Texture2D(width, height);
            var transparent = new Color(0, 0, 0, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Check corners
                    bool inCorner = false;
                    int dx = 0, dy = 0;

                    if (x < radius && y < radius) { dx = radius - x; dy = radius - y; inCorner = true; }
                    else if (x >= width - radius && y < radius) { dx = x - (width - radius - 1); dy = radius - y; inCorner = true; }
                    else if (x < radius && y >= height - radius) { dx = radius - x; dy = y - (height - radius - 1); inCorner = true; }
                    else if (x >= width - radius && y >= height - radius) { dx = x - (width - radius - 1); dy = y - (height - radius - 1); inCorner = true; }

                    if (inCorner && dx * dx + dy * dy > radius * radius)
                        tex.SetPixel(x, y, transparent);
                    else
                        tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();
            return tex;
        }

        private void InitializeStyles()
        {
            if (!_stylesDirty && _windowStyle != null && _headerBarStyle != null)
                return;

            // Create textures
            _windowBackground = MakeRoundedTex(64, 64, _windowBgColor, 8);
            _rowBackground = MakeTex(2, 2, _rowEvenColor);
            _rowAltBackground = MakeTex(2, 2, _rowOddColor);
            _buttonNormal = MakeRoundedTex(32, 32, new Color(0.25f, 0.45f, 0.65f, 0.9f), 4);
            _buttonHover = MakeRoundedTex(32, 32, new Color(0.35f, 0.55f, 0.75f, 0.95f), 4);
            _withdrawNormal = MakeRoundedTex(32, 32, _withdrawColor, 4);
            _withdrawHover = MakeRoundedTex(32, 32, _withdrawHoverColor, 4);
            _tabNormal = MakeRoundedTex(32, 32, new Color(0.2f, 0.2f, 0.28f, 0.9f), 6);
            _tabSelected = MakeGradientTex(32, 32, _accentColor, new Color(0.3f, 0.5f, 0.7f));
            _headerBarBg = MakeTex(2, 2, new Color(0.14f, 0.18f, 0.26f, 0.98f));
            _headerBarStyle = new GUIStyle { normal = { background = _headerBarBg }, padding = new RectOffset(ScaledInt(12), ScaledInt(12), ScaledInt(10), ScaledInt(10)) };

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(ScaledInt(15), ScaledInt(15), ScaledInt(10), ScaledInt(15)),
                border = new RectOffset(ScaledInt(12), ScaledInt(12), ScaledInt(12), ScaledInt(12)),
                normal = { background = _windowBackground, textColor = _textColor }
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = ScaledFont(24),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = _goldColor }
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = ScaledFont(14),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = _accentColor }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = ScaledFont(12),
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(ScaledInt(12), ScaledInt(12), ScaledInt(6), ScaledInt(6)),
                normal = { background = _buttonNormal, textColor = _textColor },
                hover = { background = _buttonHover, textColor = Color.white },
                active = { background = _buttonHover, textColor = Color.white }
            };

            _withdrawButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = ScaledFont(11),
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(ScaledInt(4), ScaledInt(4), ScaledInt(4), ScaledInt(4)),
                alignment = TextAnchor.MiddleCenter,
                normal = { background = _withdrawNormal, textColor = _textColor },
                hover = { background = _withdrawHover, textColor = Color.white },
                active = { background = _withdrawHover, textColor = Color.white }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = ScaledFont(13),
                normal = { textColor = _textColor }
            };

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = ScaledFont(16),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = _goldColor }
            };

            _categoryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = ScaledFont(12),
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(ScaledInt(14), ScaledInt(14), ScaledInt(8), ScaledInt(8)),
                margin = new RectOffset(ScaledInt(3), ScaledInt(3), 0, 0),
                normal = { background = _tabNormal, textColor = _textDimColor },
                hover = { background = _buttonHover, textColor = _textColor }
            };

            _selectedCategoryStyle = new GUIStyle(_categoryButtonStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { background = _tabSelected, textColor = Color.white },
                hover = { background = _tabSelected, textColor = Color.white }
            };

            _rowStyle = new GUIStyle()
            {
                padding = new RectOffset(ScaledInt(8), ScaledInt(8), ScaledInt(6), ScaledInt(6)),
                margin = new RectOffset(0, 0, ScaledInt(2), ScaledInt(2))
            };

            _closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = ScaledFont(13),
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(ScaledInt(20), ScaledInt(20), ScaledInt(10), ScaledInt(10)),
                normal = { background = _buttonNormal, textColor = _textColor },
                hover = { background = _buttonHover, textColor = Color.white }
            };

            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = ScaledFont(13),
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(ScaledInt(6), ScaledInt(6), ScaledInt(4), ScaledInt(4)),
                normal = { textColor = _textColor }
            };

            // Cached styles for DrawWindow subtitle
            _subtitleStyle = new GUIStyle(_labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = ScaledFont(11)
            };
            _subtitleStyle.normal.textColor = _textDimColor;

            // Cached style for empty category message
            _emptyStyle = new GUIStyle(_labelStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
            _emptyStyle.normal.textColor = _textDimColor;

            // Cached toggle button textures (created once, not every frame)
            _toggleOnBg = MakeTex(1, 1, new Color(0.2f, 0.5f, 0.2f, 0.9f));
            _toggleOnHover = MakeTex(1, 1, new Color(0.25f, 0.6f, 0.25f, 1f));
            _toggleOffBg = MakeTex(1, 1, new Color(0.3f, 0.3f, 0.35f, 0.9f));
            _toggleOffHover = MakeTex(1, 1, new Color(0.4f, 0.4f, 0.45f, 1f));

            // Cached toggle button styles
            _toggleOnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = ScaledFont(9),
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(1, 1, 1, 1),
                normal = { background = _toggleOnBg, textColor = new Color(0.5f, 1f, 0.5f) },
                hover = { background = _toggleOnHover, textColor = Color.white },
                active = { background = _toggleOnHover, textColor = Color.white }
            };

            _toggleOffStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = ScaledFont(9),
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(1, 1, 1, 1),
                normal = { background = _toggleOffBg, textColor = new Color(0.6f, 0.6f, 0.6f) },
                hover = { background = _toggleOffHover, textColor = Color.white },
                active = { background = _toggleOffHover, textColor = Color.white }
            };

            // Cached icon fallback style
            _iconFallbackStyle = new GUIStyle(_labelStyle)
            {
                fontSize = ScaledFont(10),
                alignment = TextAnchor.MiddleCenter
            };
            _iconFallbackStyle.normal.textColor = _accentColor;

            // Cached hint style for controls
            _hintStyle = new GUIStyle(_labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };
            _hintStyle.normal.textColor = _textDimColor;

            // Cached selected name style
            _selectedNameStyle = new GUIStyle(_labelStyle)
            {
                fontStyle = FontStyle.Bold
            };
            _selectedNameStyle.normal.textColor = _accentColor;

            // Cached amount label style
            _amountLabelStyle = new GUIStyle(_labelStyle);
            _amountLabelStyle.normal.textColor = _textDimColor;

            _statusErrorStyle = new GUIStyle(_hintStyle)
            {
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            _statusErrorStyle.normal.textColor = new Color(1f, 0.42f, 0.42f);

            _statusOkStyle = new GUIStyle(_hintStyle)
            {
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            _statusOkStyle.normal.textColor = new Color(0.5f, 0.95f, 0.65f);

            _stylesDirty = false;
        }

        private void OnGUI()
        {
            // Don't show UI until vault is loaded for the current character
            if (!_isVisible || _vaultManager == null || !PlayerPatches.IsVaultLoaded) return;

            if (_stylesDirty || _windowStyle == null || _headerBarStyle == null)
            {
                InitializeStyles();
                if (_windowStyle == null || _headerBarStyle == null)
                    return;
            }

            // Compute content-reactive height
            float contentHeight = GetContentHeight();
            float minH = Scaled(320f);
            float totalHeight = Mathf.Clamp(HeaderHeight + contentHeight + FooterHeight, minH, Screen.height - Scaled(60f));
            _windowRect.width = WindowWidth;
            _windowRect.height = totalHeight;
            // Keep window on screen when resizing
            if (_windowRect.yMax > Screen.height - Scaled(20f)) _windowRect.y = Screen.height - totalHeight - Scaled(20f);
            if (_windowRect.y < Scaled(10f)) _windowRect.y = Scaled(10f);
            if (_windowRect.xMax > Screen.width - Scaled(10f)) _windowRect.x = Screen.width - WindowWidth - Scaled(10f);
            if (_windowRect.x < Scaled(10f)) _windowRect.x = Scaled(10f);

            // Draw shadow/backdrop
            GUI.color = new Color(0, 0, 0, 0.3f);
            GUI.DrawTexture(new Rect(_windowRect.x + Scaled(4), _windowRect.y + Scaled(4), _windowRect.width, _windowRect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            _windowRect = GUI.Window(
                GetHashCode(),
                _windowRect,
                DrawWindow,
                "",
                _windowStyle
            );
        }

        private void DrawWindow(int windowId)
        {
            if (!Plugin.GetConfigDebugFullVaultInspector())
                _showDebugVaultDump = false;

            // --- Main UI header: always visible (including when Settings is open) ---
            GUILayout.BeginVertical(_headerBarStyle, GUILayout.MinHeight(Scaled(100)));
            {
                GUILayout.Space(Scaled(4));
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("\u2727  The Vault  \u2727", _titleStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(Scaled(2));
                GUILayout.Label("Currency Storage System", _subtitleStyle);
                GUILayout.Space(Scaled(10));
                DrawCategoryTabs();
                GUILayout.Space(Scaled(4));
            }
            GUILayout.EndVertical();

            DrawHorizontalLine(_accentColor, 1);
            GUILayout.Space(Scaled(6));

            if (_showSettings)
            {
                DrawSettingsPanel();
            }
            else if (_showDebugVaultDump)
            {
                float scrollHeight = GetContentHeight();
                _debugVaultScroll = GUILayout.BeginScrollView(_debugVaultScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(scrollHeight));
                DrawDebugVaultDumpContent();
                GUILayout.EndScrollView();

                GUILayout.Space(Scaled(8));
                DrawHorizontalLine(_accentColor, 1);
                GUILayout.Space(Scaled(8));
                GUILayout.Label("Withdraw / sweep are disabled in Debug view. Turn off Debug in Settings to use them.", _hintStyle);
            }
            else
            {
                float scrollHeight = GetContentHeight();
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(scrollHeight));
                DrawCurrencyList();
                GUILayout.EndScrollView();

                GUILayout.Space(Scaled(8));
                DrawHorizontalLine(_accentColor, 1);
                GUILayout.Space(Scaled(8));

                // Deposit/Withdraw controls
                DrawControls();
            }

            // Bottom: Close button only (Settings is in the tab bar)
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", _closeButtonStyle, GUILayout.Width(Scaled(120)), GUILayout.Height(Scaled(35))))
            {
                Hide();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Make window draggable from header area (covers title + tabs)
            GUI.DragWindow(new Rect(0, 0, WindowWidth, Scaled(108)));
        }

        /// <summary>
        /// Returns content area height based on current mode (settings vs currency list) and item count.
        /// </summary>
        private float GetContentHeight()
        {
            if (_showSettings) return SettingsContentHeight;
            if (_showDebugVaultDump) return MaxContentHeight;
            var currencies = GetCurrenciesForCategory(_selectedCategory);
            int count = currencies.Count;
            if (count == 0) return MinContentHeight;
            float desired = count * RowHeight + Scaled(8f); // +padding
            return Mathf.Clamp(desired, MinContentHeight, MaxContentHeight);
        }

        private void DrawHorizontalLine(Color color, float height)
        {
            var rect = GUILayoutUtility.GetRect(1, height, GUILayout.ExpandWidth(true));
            GUI.color = color * 0.6f;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Only show these categories in the UI
        private static readonly CurrencyCategory[] _enabledCategories = new[]
        {
            CurrencyCategory.SeasonalToken,
            CurrencyCategory.Key,
            CurrencyCategory.Special,
            CurrencyCategory.Custom
        };

        private void DrawCategoryTabs()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            foreach (CurrencyCategory category in _enabledCategories)
            {
                var style = !_showSettings && !_showDebugVaultDump && _selectedCategory == category ? _selectedCategoryStyle : _categoryButtonStyle;
                string icon = GetCategoryIcon(category);
                string label = $"{icon} {GetCategoryDisplayName(category)}";

                if (GUILayout.Button(label, style, GUILayout.MinWidth(88)))
                {
                    _showSettings = false;
                    _showDebugVaultDump = false;
                    _selectedCategory = category;
                    _selectedCurrencyId = "";
                }
            }

            if (Plugin.GetConfigDebugFullVaultInspector())
            {
                var dbgStyle = _showDebugVaultDump ? _selectedCategoryStyle : _categoryButtonStyle;
                if (GUILayout.Button("\u205C Debug", dbgStyle, GUILayout.MinWidth(72)))
                {
                    _showSettings = false;
                    _showDebugVaultDump = true;
                    _selectedCurrencyId = "";
                }
            }

            // Settings tab - always visible as part of the Vault UI
            var settingsStyle = _showSettings && !_showDebugVaultDump ? _selectedCategoryStyle : _categoryButtonStyle;
            if (GUILayout.Button("\u2699 Settings", settingsStyle, GUILayout.MinWidth(88)))
            {
                _showSettings = true;
                _showDebugVaultDump = false;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private string GetCategoryIcon(CurrencyCategory category)
        {
            return category switch
            {
                CurrencyCategory.SeasonalToken => "\u2600", // Sun symbol
                CurrencyCategory.CommunityToken => "\u2665", // Heart
                CurrencyCategory.Key => "\u26bf", // Key-like symbol
                CurrencyCategory.Special => "\u2605", // Star symbol
                CurrencyCategory.Custom => "\u2606", // Open star (distinct from Special \u2605)
                _ => "\u2022"
            };
        }

        private string GetCategoryDisplayName(CurrencyCategory category)
        {
            return category switch
            {
                CurrencyCategory.SeasonalToken => "Seasonal",
                CurrencyCategory.CommunityToken => "Community",
                CurrencyCategory.Key => "Keys",
                CurrencyCategory.Special => "Special",
                CurrencyCategory.Custom => "Custom",
                _ => category.ToString()
            };
        }

        private int _rowIndex = 0;

        private void DrawDebugVaultDumpContent()
        {
            if (_vaultManager == null)
            {
                GUILayout.Label("(Vault not initialized)", _emptyStyle);
                return;
            }

            GUILayout.Label("Raw snapshot of all vault dictionaries (Seasonal, Community, Keys, Tickets, Orbs, Custom).", _hintStyle);
            GUILayout.Space(Scaled(6));

            List<string> lines = _vaultManager.BuildDebugVaultReport();
            foreach (string line in lines)
                GUILayout.Label(line, _labelStyle);
        }

        private void DrawCurrencyList()
        {
            var currencies = GetCurrenciesForCategory(_selectedCategory);

            if (currencies.Count == 0)
            {
                GUILayout.Space(20);
                if (_selectedCategory == CurrencyCategory.Custom)
                {
                    GUILayout.Label("No custom currencies yet.", _emptyStyle);
                    GUILayout.Space(6);
                    GUILayout.Label("Other mods register entries with TheVault.Api.VaultIntegration.RegisterCustomCurrency (after The Vault loads).", _hintStyle);
                    GUILayout.Space(4);
                    GUILayout.Label("Use a short id (a-z, 0-9, underscore) and optional Sun Haven item id for withdraw / auto-deposit.", _hintStyle);
                }
                else if (_selectedCategory == CurrencyCategory.SeasonalToken)
                {
                    GUILayout.Label("No seasonal tokens in the vault.", _emptyStyle);
                    GUILayout.Space(6);
                    GUILayout.Label("Deposit festival tokens from your inventory, or enable auto-deposit for those items.", _hintStyle);
                }
                else if (_selectedCategory == CurrencyCategory.Key)
                {
                    GUILayout.Label("No keys stored in the vault.", _emptyStyle);
                    GUILayout.Space(6);
                    GUILayout.Label("Keys you move into the vault appear here. Use Sweep or per-item auto-deposit to add them.", _hintStyle);
                }
                else if (_selectedCategory == CurrencyCategory.Special)
                {
                    GUILayout.Label("No special currencies in the vault.", _emptyStyle);
                    GUILayout.Space(6);
                    GUILayout.Label("Doubloons, tickets, shards, and similar currencies show here once deposited.", _hintStyle);
                }
                else
                {
                    GUILayout.Label("No items in this category", _emptyStyle);
                }
                return;
            }

            _rowIndex = 0;
            foreach (var kvp in currencies)
            {
                DrawCurrencyRow(kvp.Key, kvp.Value, _rowIndex % 2 == 0);
                _rowIndex++;
            }
        }

        private Dictionary<string, int> GetCurrenciesForCategory(CurrencyCategory category) =>
            VaultUiShared.GetCurrenciesForCategory(_vaultManager, category);

        private void DrawCurrencyRow(string currencyId, int amount, bool isEvenRow)
        {
            bool isSelected = _selectedCurrencyId == currencyId;
            bool autoDepositEnabled = ItemPatches.IsAutoDepositEnabled(currencyId);

            // Row background
            var rowRect = GUILayoutUtility.GetRect(0, RowHeight, GUILayout.ExpandWidth(true));
            var bgColor = isSelected ? new Color(0.3f, 0.5f, 0.7f, 0.5f) : (isEvenRow ? _rowEvenColor : _rowOddColor);
            GUI.color = bgColor;
            GUI.DrawTexture(rowRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Draw content using absolute positioning within the row
            float yCenter = rowRect.y + (rowRect.height - Scaled(26)) / 2;
            float xPos = rowRect.x + Scaled(8);

            // Auto-deposit toggle button (leftmost) - using cached styles
            float toggleBtnY = rowRect.y + (rowRect.height - Scaled(20)) / 2;
            var toggleStyle = autoDepositEnabled ? _toggleOnStyle : _toggleOffStyle;

            var toggleRect = new Rect(xPos, toggleBtnY, 20, 20);
            string toggleText = autoDepositEnabled ? "ON" : "--";
            if (GUI.Button(toggleRect, toggleText, toggleStyle))
            {
                ItemPatches.ToggleAutoDeposit(currencyId);
                Plugin.Log?.LogInfo($"[UI] Toggle clicked for {currencyId}");
            }
            xPos += 24;

            // Currency icon - use game icon if available, fallback to text
            float iconSize = Scaled(28f);
            float iconY = rowRect.y + (rowRect.height - iconSize) / 2;
            Texture2D iconTexture = IconCache.GetIconForCurrency(currencyId);
            if (iconTexture != null && IconCache.IsIconLoaded(currencyId))
            {
                // Draw the actual game icon
                GUI.DrawTexture(new Rect(xPos, iconY, iconSize, iconSize), iconTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                // Fallback to text icon while loading or if icon unavailable (using cached style)
                string icon = VaultUiShared.GetCurrencyIcon(currencyId);
                GUI.Label(new Rect(xPos, yCenter, iconSize, Scaled(26)), icon, _iconFallbackStyle);
            }
            xPos += 32;

            // Currency name (wider column for long names like "King's Lost Mine Key")
            string displayName = VaultUiShared.GetDisplayName(_vaultManager, currencyId);
            GUI.Label(new Rect(xPos, yCenter, 140, 26), displayName, _labelStyle);
            xPos += 144;

            // Amount with gold color - make it prominent with "x" prefix
            // Use K/M formatting for large numbers
            string amountText = "x" + VaultUiShared.FormatNumber(amount);
            var amountStyle = new GUIStyle(_valueStyle) { fontSize = ScaledFont(16), alignment = TextAnchor.MiddleLeft };
            GUI.Label(new Rect(xPos, yCenter, 70, 26), amountText, amountStyle);
            xPos += 74;

            // Quick withdraw buttons - positioned from the right
            float btnY = rowRect.y + (rowRect.height - Scaled(28)) / 2;
            float rightEdge = rowRect.x + rowRect.width - Scaled(8);

            // -10 button (rightmost)
            GUI.enabled = amount >= 10;
            if (GUI.Button(new Rect(rightEdge - 40, btnY, 40, 28), "-10", _withdrawButtonStyle))
            {
                WithdrawToInventory(currencyId, 10);
            }

            // -5 button
            GUI.enabled = amount >= 5;
            if (GUI.Button(new Rect(rightEdge - 78, btnY, 34, 28), "-5", _withdrawButtonStyle))
            {
                WithdrawToInventory(currencyId, 5);
            }

            // -1 button
            GUI.enabled = amount >= 1;
            if (GUI.Button(new Rect(rightEdge - 116, btnY, 34, 28), "-1", _withdrawButtonStyle))
            {
                WithdrawToInventory(currencyId, 1);
            }

            GUI.enabled = true;

            // Row selection on click (only if not clicking a button)
            // Check if mouse is in row but not over any button
            if (Event.current.type == UnityEngine.EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                // Check if click is outside button areas
                bool overToggle = toggleRect.Contains(Event.current.mousePosition);
                bool overWithdraw1 = new Rect(rightEdge - 116, btnY, 34, 28).Contains(Event.current.mousePosition);
                bool overWithdraw5 = new Rect(rightEdge - 78, btnY, 34, 28).Contains(Event.current.mousePosition);
                bool overWithdraw10 = new Rect(rightEdge - 40, btnY, 40, 28).Contains(Event.current.mousePosition);

                if (!overToggle && !overWithdraw1 && !overWithdraw5 && !overWithdraw10)
                {
                    _selectedCurrencyId = currencyId;
                }
            }
        }

        private void DrawSettingsPanel()
        {
            GUILayout.Space(Scaled(4));
            GUILayout.Label("\u2699  Settings (saved to config file)", _subtitleStyle);
            GUILayout.Space(Scaled(6));
            float settingsScrollHeight = Mathf.Min(SettingsContentHeight, MaxContentHeight);
            _settingsScroll = GUILayout.BeginScrollView(_settingsScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(settingsScrollHeight));

            // Window scale
            GUILayout.Label("Display", _labelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Window scale:", _labelStyle, GUILayout.Width(Scaled(90)));
            float winScale = Plugin.GetConfigWindowScale();
            float newWinScale = GUILayout.HorizontalSlider(winScale, 0.5f, 3f, GUILayout.Width(Scaled(120)));
            if (Math.Abs(newWinScale - winScale) > 0.01f) Plugin.SetConfigWindowScale(newWinScale);
            GUILayout.Label($"{newWinScale:F1}", _labelStyle, GUILayout.Width(Scaled(28)));
            GUILayout.EndHorizontal();
            GUILayout.Space(Scaled(6));

            // HUD
            GUILayout.Label("HUD", _labelStyle);
            GUILayout.Label("Drag the currency bar by its top strip to move it; position saves to config (like Sun Haven Todo). Changing [HUD] Position resets a custom placement.", _hintStyle);
            bool hudOn = Plugin.GetConfigHUDEnabled();
            bool newHudOn = GUILayout.Toggle(hudOn, " Show vault currency bar", _labelStyle);
            if (newHudOn != hudOn) Plugin.SetConfigHUDEnabled(newHudOn);

            GUILayout.BeginHorizontal();
            GUILayout.Label("HUD scale:", _labelStyle, GUILayout.Width(Scaled(80)));
            float scale = Plugin.GetConfigHUDScale();
            float newScale = GUILayout.HorizontalSlider(scale, 0.5f, 3f, GUILayout.Width(Scaled(120)));
            if (Math.Abs(newScale - scale) > 0.01f) Plugin.SetConfigHUDScale(newScale);
            GUILayout.Label($"{newScale:F1}", _labelStyle, GUILayout.Width(Scaled(28)));
            GUILayout.EndHorizontal();

            GUILayout.Label("HUD density:", _labelStyle);
            string density = Plugin.GetConfigHudDensity();
            string[] densityOpts = { "Normal", "Compact", "Minimal" };
            int dIdx = Array.IndexOf(densityOpts, density);
            if (dIdx < 0) dIdx = 0;
            int newDIdx = GUILayout.SelectionGrid(dIdx, densityOpts, 3, _buttonStyle);
            if (newDIdx != dIdx) Plugin.SetConfigHudDensity(densityOpts[newDIdx]);

            bool hudCompactLegacy = Plugin.GetConfigHUDCompactMode();
            bool newHudCompactLegacy = GUILayout.Toggle(hudCompactLegacy, " Legacy: Compact when density is Normal ([HUD] CompactMode)", _labelStyle);
            if (newHudCompactLegacy != hudCompactLegacy) Plugin.SetConfigHUDCompactMode(newHudCompactLegacy);

            GUILayout.Space(Scaled(6));

            GUILayout.Label("Debug (full vault inspector)", _labelStyle);
            bool dbgOn = Plugin.GetConfigDebugFullVaultInspector();
            GUILayout.Label(
                dbgOn
                    ? "ON — zeros visible in tabs, Debug tab available, HUD shows all slots."
                    : "OFF — enable in Haven Dev Tools (F11) → Azrael's Mods → The Vault, or set [The Vault] FullVaultInspector in HavenDevTools config.",
                _hintStyle);
            GUILayout.Space(Scaled(6));

            // Open vault keys
            GUILayout.Label("Open Vault", _labelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Key:", _labelStyle, GUILayout.Width(Scaled(50)));
            int toggleIdx = IndexOfKey(Plugin.GetConfigToggleKey());
            int newToggleIdx = GUILayout.SelectionGrid(toggleIdx, SettingsKeyNames, 6, _buttonStyle);
            if (newToggleIdx != toggleIdx && newToggleIdx >= 0) Plugin.SetConfigToggleKey(SettingsKeyOptions[newToggleIdx]);
            GUILayout.EndHorizontal();

            bool reqCtrl = Plugin.GetConfigRequireCtrl();
            bool newReqCtrl = GUILayout.Toggle(reqCtrl, " Require Ctrl (e.g. Ctrl+V)", _labelStyle);
            if (newReqCtrl != reqCtrl) Plugin.SetConfigRequireCtrl(newReqCtrl);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Alt key:", _labelStyle, GUILayout.Width(Scaled(50)));
            int altIdx = IndexOfKey(Plugin.GetConfigAltToggleKey());
            int newAltIdx = GUILayout.SelectionGrid(altIdx, SettingsKeyNames, 6, _buttonStyle);
            if (newAltIdx != altIdx && newAltIdx >= 0) Plugin.SetConfigAltToggleKey(SettingsKeyOptions[newAltIdx]);
            GUILayout.EndHorizontal();

            GUILayout.Space(Scaled(8));
            GUILayout.Label("Changes save automatically.", _hintStyle);
            GUILayout.EndScrollView();
        }

        private static int IndexOfKey(KeyCode k)
        {
            for (int i = 0; i < SettingsKeyOptions.Length; i++)
                if (SettingsKeyOptions[i] == k) return i;
            return 0;
        }

        private void DrawControls()
        {
            if (!string.IsNullOrEmpty(_vaultStatusMessage))
            {
                var st = _vaultStatusIsError ? _statusErrorStyle : _statusOkStyle;
                GUILayout.Label(_vaultStatusMessage, st);
                GUILayout.Space(Scaled(4));
            }

            if (string.IsNullOrEmpty(_selectedCurrencyId))
            {
                GUILayout.Label("Click a row to select, or use quick withdraw / deposit buttons", _hintStyle);
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUILayout.Label($"\u25b6 {VaultUiShared.GetDisplayName(_vaultManager, _selectedCurrencyId)}", _selectedNameStyle);

                GUILayout.Space(Scaled(15));

                GUILayout.Label("Qty:", _amountLabelStyle, GUILayout.Width(Scaled(30)));
                _depositAmount = GUILayout.TextField(_depositAmount, 6, _textFieldStyle, GUILayout.Width(Scaled(55)));

                GUILayout.Space(Scaled(8));

                if (GUILayout.Button("Withdraw", _buttonStyle, GUILayout.Width(Scaled(86))))
                {
                    if (!int.TryParse(_depositAmount, out int wAmt) || wAmt <= 0)
                        SetVaultStatus("Enter a positive number for quantity.", true);
                    else
                        WithdrawToInventory(_selectedCurrencyId, wAmt);
                }

                if (GUILayout.Button("Deposit", _buttonStyle, GUILayout.Width(Scaled(86))))
                {
                    if (!int.TryParse(_depositAmount, out int dAmt) || dAmt <= 0)
                        SetVaultStatus("Enter a positive number for quantity.", true);
                    else
                        DepositFromInventory(_selectedCurrencyId, dAmt);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                int mapId = ItemPatches.GetItemForCurrency(_selectedCurrencyId);
                if (mapId > 0)
                {
                    int inBag = ItemPatches.GetRawInventoryCount(mapId);
                    GUILayout.Label($"In inventory (bag): {inBag}", _hintStyle);
                }
                else
                {
                    GUILayout.Label("No matching inventory item for this row — deposit is unavailable.", _hintStyle);
                }
            }

            // Global sweep button to force auto-deposit from inventory
            GUILayout.Space(Scaled(6));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Sweep Inventory (Auto-Deposit Now)", _buttonStyle, GUILayout.Width(Scaled(260)), GUILayout.Height(Scaled(32))))
            {
                Plugin.Log?.LogInfo("[Vault UI] Sweep button clicked");
                ItemPatches.ForceAutoDepositAll();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void WithdrawToInventory(string currencyId, int amount) =>
            VaultInventoryOperations.WithdrawToInventory(_vaultManager, currencyId, amount, SetVaultStatus);

        private void DepositFromInventory(string currencyId, int amount) =>
            VaultInventoryOperations.DepositFromInventory(_vaultManager, currencyId, amount, SetVaultStatus);
    }
}
