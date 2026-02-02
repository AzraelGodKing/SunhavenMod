using System;
using System.Collections.Generic;
using BirthdayReminder.Data;
using UnityEngine;

namespace BirthdayReminder.UI
{
    /// <summary>
    /// Small movable HUD showing today's birthdays.
    /// Displays on wake up and can be dismissed.
    /// </summary>
    public class BirthdayHUD : MonoBehaviour
    {
        // Window settings
        private const int WINDOW_ID = 98770;
        private const int GIFT_WINDOW_ID = 98771;
        private const float WINDOW_WIDTH = 320f;
        private const float MIN_HEIGHT = 100f;
        private const float MAX_HEIGHT = 500f;
        private const float HEADER_HEIGHT = 28f;
        private const float ITEM_HEIGHT = 65f;

        // Gift popup window settings
        private const float GIFT_WINDOW_WIDTH = 300f;
        private const float GIFT_WINDOW_HEIGHT = 400f;

        // Manager reference
        private BirthdayManager _manager;

        // Display state
        private bool _isVisible;
        private Rect _windowRect;
        private float _showTimer;
        private const float AUTO_HIDE_DELAY = 15f;

        // Gift popup state
        private bool _showGiftPopup;
        private Rect _giftPopupRect;
        private BirthdayDisplayInfo _selectedNPC;
        private Vector2 _giftScrollPosition;

        // Position persistence callback
        public Action<float, float> OnPositionChanged;

        // Color scheme - warm festive theme
        private readonly Color _bgColor = new Color(0.18f, 0.14f, 0.12f, 0.95f);  // Dark brown
        private readonly Color _headerColor = new Color(0.75f, 0.35f, 0.45f, 1f);  // Rose/pink
        private readonly Color _borderColor = new Color(0.85f, 0.65f, 0.50f, 1f);  // Gold border
        private readonly Color _textLight = new Color(0.95f, 0.92f, 0.88f);  // Cream text
        private readonly Color _textDark = new Color(0.25f, 0.20f, 0.15f);
        private readonly Color _giftedColor = new Color(0.5f, 0.85f, 0.5f);  // Bright green
        private readonly Color _ungiftedColor = new Color(1f, 0.75f, 0.3f);  // Gold/amber
        private readonly Color _hintColor = new Color(0.75f, 0.70f, 0.65f);
        private readonly Color _lovedColor = new Color(0.95f, 0.45f, 0.55f);  // Pink for loved
        private readonly Color _likedColor = new Color(0.55f, 0.70f, 0.95f);  // Blue for liked
        private readonly Color _universalColor = new Color(0.85f, 0.75f, 0.45f);  // Gold for universal

        // Styles
        private bool _stylesInitialized;
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _nameStyle;
        private GUIStyle _giftedStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _closeButtonStyle;
        private GUIStyle _moreButtonStyle;
        private GUIStyle _giftItemStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _universalItemStyle;

        // Textures
        private Texture2D _windowBackground;
        private Texture2D _headerBackground;
        private Texture2D _itemBackground;
        private Texture2D _separatorTex;

        private void Awake()
        {
            _windowRect = new Rect(100, 100, WINDOW_WIDTH, MIN_HEIGHT);
            _isVisible = false;  // Don't show by default - only show when player is in-game with birthdays
            Plugin.Log?.LogInfo("[BirthdayHUD] Awake called - isVisible set to false (waiting for player init)");
        }

        public void Initialize(BirthdayManager manager)
        {
            _manager = manager;
            Plugin.Log?.LogInfo($"[BirthdayHUD] Initialize called, manager: {manager != null}, screen: ({Screen.width}x{Screen.height})");

            // Set default position (top-right corner)
            if (Screen.width > 0 && Screen.height > 0)
            {
                _windowRect = new Rect(
                    Screen.width - WINDOW_WIDTH - 20,
                    80,
                    WINDOW_WIDTH,
                    MIN_HEIGHT
                );
                Plugin.Log?.LogInfo($"[BirthdayHUD] Default position set to ({_windowRect.x}, {_windowRect.y})");
            }
            else
            {
                // Fallback position if screen isn't ready
                _windowRect = new Rect(100, 100, WINDOW_WIDTH, MIN_HEIGHT);
                Plugin.Log?.LogInfo($"[BirthdayHUD] Using fallback position (100, 100) - screen not ready");
            }

            if (_manager != null)
            {
                _manager.OnBirthdaysUpdated += OnBirthdaysUpdated;
            }
        }

        public void SetPosition(float x, float y)
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;

            if (x >= 0)
            {
                _windowRect.x = Mathf.Clamp(x, 0, Screen.width - _windowRect.width);
            }
            if (y >= 0)
            {
                _windowRect.y = Mathf.Clamp(y, 0, Screen.height - _windowRect.height);
            }
        }

        public (float x, float y) GetPosition()
        {
            return (_windowRect.x, _windowRect.y);
        }

        public bool IsVisible => _isVisible;

        public void Show()
        {
            _isVisible = true;
            _showTimer = 0f;

            // Ensure window is within screen bounds
            EnsureOnScreen();

            Plugin.Log?.LogInfo($"[BirthdayHUD] Show() called - isVisible: {_isVisible}, pos: ({_windowRect.x}, {_windowRect.y}), screen: ({Screen.width}x{Screen.height}), birthdays: {_manager?.TodaysBirthdays?.Count ?? 0}");
        }

        /// <summary>
        /// Ensure the HUD window is within the visible screen area
        /// </summary>
        private void EnsureOnScreen()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;

            // If position is off-screen or invalid, reset to default position
            bool needsReset = _windowRect.x < 0 ||
                              _windowRect.y < 0 ||
                              _windowRect.x > Screen.width - 50 ||
                              _windowRect.y > Screen.height - 50;

            if (needsReset)
            {
                Plugin.Log?.LogInfo($"[BirthdayHUD] Resetting position - was ({_windowRect.x}, {_windowRect.y})");
                _windowRect.x = Screen.width - WINDOW_WIDTH - 20;
                _windowRect.y = 80;
            }

            // Clamp to screen bounds
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);
        }

        public void Hide()
        {
            _isVisible = false;
        }

        public void Toggle()
        {
            if (_isVisible)
                Hide();
            else
                Show();
        }

        private void OnBirthdaysUpdated()
        {
            if (_manager != null && _manager.HasBirthdays)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void Update()
        {
            if (!_isVisible) return;

            // Update status message timer
            _manager?.UpdateStatusMessage(Time.unscaledDeltaTime);

            if (_manager != null && !_manager.HasUngiftedBirthdays)
            {
                _showTimer += Time.unscaledDeltaTime;
                if (_showTimer >= AUTO_HIDE_DELAY)
                {
                    Hide();
                }
            }
            else
            {
                _showTimer = 0f;
            }
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            if (!_stylesInitialized)
            {
                InitializeStyles();
            }

            int birthdayCount = (_manager != null && _manager.TodaysBirthdays != null) ? _manager.TodaysBirthdays.Count : 0;

            // Calculate height: header + padding + items + bottom padding
            float contentHeight = HEADER_HEIGHT + 12;  // Header + top padding
            if (birthdayCount == 0)
            {
                contentHeight += 40;  // Empty message
            }
            else
            {
                contentHeight += birthdayCount * ITEM_HEIGHT;
            }
            contentHeight += 16;  // Bottom padding + margins
            _windowRect.height = Mathf.Clamp(contentHeight, MIN_HEIGHT, MAX_HEIGHT);

            var prevRect = _windowRect;

            // Draw shadow
            DrawShadow(_windowRect, 4);

            GUI.depth = -800;
            _windowRect = GUI.Window(WINDOW_ID, _windowRect, DrawWindow, "", _windowStyle);

            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);

            if (_showGiftPopup && _selectedNPC != null)
            {
                DrawShadow(_giftPopupRect, 4);

                GUI.depth = -900;
                _giftPopupRect = GUI.Window(GIFT_WINDOW_ID, _giftPopupRect, DrawGiftPopup, "", _windowStyle);

                _giftPopupRect.x = Mathf.Clamp(_giftPopupRect.x, 0, Screen.width - _giftPopupRect.width);
                _giftPopupRect.y = Mathf.Clamp(_giftPopupRect.y, 0, Screen.height - _giftPopupRect.height);
            }

            if (Math.Abs(_windowRect.x - prevRect.x) > 0.1f || Math.Abs(_windowRect.y - prevRect.y) > 0.1f)
            {
                OnPositionChanged?.Invoke(_windowRect.x, _windowRect.y);
            }
        }

        private void DrawShadow(Rect rect, int offset)
        {
            var shadowColor = new Color(0, 0, 0, 0.3f);
            var shadowRect = new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height);
            GUI.color = shadowColor;
            GUI.DrawTexture(shadowRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawWindow(int windowId)
        {
            // Draw border
            DrawBorder(_windowRect.width, _windowRect.height, 2);

            GUILayout.BeginVertical();
            string dateStr = _manager?.CurrentDateFormatted ?? "";
            string headerTitle = string.IsNullOrEmpty(dateStr) ? "Birthday Today!" : $"Birthday Today! - {dateStr}";
            DrawHeader(headerTitle, WINDOW_WIDTH);
            DrawBirthdays();
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, WINDOW_WIDTH - 24, HEADER_HEIGHT));
        }

        private void DrawBorder(float width, float height, int borderSize)
        {
            GUI.color = _borderColor;
            // Top
            GUI.DrawTexture(new Rect(0, 0, width, borderSize), Texture2D.whiteTexture);
            // Bottom
            GUI.DrawTexture(new Rect(0, height - borderSize, width, borderSize), Texture2D.whiteTexture);
            // Left
            GUI.DrawTexture(new Rect(0, 0, borderSize, height), Texture2D.whiteTexture);
            // Right
            GUI.DrawTexture(new Rect(width - borderSize, 0, borderSize, height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawHeader(string title, float width)
        {
            var headerRect = new Rect(0, 0, width, HEADER_HEIGHT);
            if (_headerBackground != null)
            {
                GUI.DrawTexture(headerRect, _headerBackground);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            // Cake icon using text
            GUILayout.Label("[*]", _headerStyle, GUILayout.Width(24), GUILayout.Height(HEADER_HEIGHT));
            GUILayout.Label(title, _headerStyle, GUILayout.Height(HEADER_HEIGHT));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("X", _closeButtonStyle, GUILayout.Width(24), GUILayout.Height(24)))
            {
                if (title.Contains("Gifts"))
                {
                    _showGiftPopup = false;
                    _selectedNPC = null;
                }
                else
                {
                    Hide();
                }
            }

            GUILayout.Space(6);
            GUILayout.EndHorizontal();
        }

        private void DrawBirthdays()
        {
            GUILayout.Space(6);

            // Show status message if any (e.g., "Refreshed!")
            if (_manager != null && _manager.HasStatusMessage)
            {
                var statusStyle = new GUIStyle(_hintStyle)
                {
                    normal = { textColor = _giftedColor },
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(_manager.StatusMessage, statusStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            if (_manager == null || _manager.TodaysBirthdays == null || _manager.TodaysBirthdays.Count == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("No birthdays today", _hintStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                foreach (var birthday in _manager.TodaysBirthdays)
                {
                    DrawBirthdayItem(birthday);
                }
            }

            GUILayout.Space(6);
        }

        private void DrawBirthdayItem(BirthdayDisplayInfo birthday)
        {
            // Use a box style for item background
            var itemBoxStyle = new GUIStyle
            {
                normal = { background = _itemBackground },
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(6, 6, 2, 2)
            };

            GUILayout.BeginVertical(itemBoxStyle);

            GUILayout.BeginHorizontal();

            // Status indicator
            var statusColor = birthday.HasBeenGifted ? _giftedColor : _ungiftedColor;
            var statusStyle = new GUIStyle(_nameStyle)
            {
                normal = { textColor = statusColor },
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };
            GUILayout.Label(birthday.HasBeenGifted ? "[OK]" : "[!!]", statusStyle, GUILayout.Width(40));

            GUILayout.BeginVertical();

            // Name row
            GUILayout.BeginHorizontal();
            var nameStyle = new GUIStyle(_nameStyle)
            {
                normal = { textColor = birthday.HasBeenGifted ? new Color(_textLight.r, _textLight.g, _textLight.b, 0.5f) : _textLight },
                fontStyle = birthday.HasBeenGifted ? FontStyle.Italic : FontStyle.Bold,
                fontSize = 13
            };
            GUILayout.Label(birthday.NPCName, nameStyle);

            GUILayout.FlexibleSpace();

            if (birthday.AllLovedGifts.Count > 0 || birthday.AllLikedGifts.Count > 0)
            {
                if (GUILayout.Button("Gifts", _moreButtonStyle, GUILayout.Width(50), GUILayout.Height(20)))
                {
                    OpenGiftPopup(birthday);
                }
            }
            GUILayout.EndHorizontal();

            // Gift hint
            if (!birthday.HasBeenGifted && !string.IsNullOrEmpty(birthday.GiftHint))
            {
                GUILayout.Space(2);
                GUILayout.Label(birthday.GiftHint, _hintStyle);
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void OpenGiftPopup(BirthdayDisplayInfo birthday)
        {
            _selectedNPC = birthday;
            _showGiftPopup = true;
            _giftScrollPosition = Vector2.zero;

            _giftPopupRect = new Rect(
                _windowRect.x + _windowRect.width + 10,
                _windowRect.y,
                GIFT_WINDOW_WIDTH,
                GIFT_WINDOW_HEIGHT
            );

            if (_giftPopupRect.x + GIFT_WINDOW_WIDTH > Screen.width)
            {
                _giftPopupRect.x = _windowRect.x - GIFT_WINDOW_WIDTH - 10;
            }
        }

        private void DrawGiftPopup(int windowId)
        {
            // Draw border
            DrawBorder(GIFT_WINDOW_WIDTH, GIFT_WINDOW_HEIGHT, 2);

            GUILayout.BeginVertical();

            // Header
            var headerRect = new Rect(0, 0, GIFT_WINDOW_WIDTH, HEADER_HEIGHT);
            if (_headerBackground != null)
            {
                GUI.DrawTexture(headerRect, _headerBackground);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label($"{_selectedNPC.NPCName}'s Gifts", _headerStyle, GUILayout.Height(HEADER_HEIGHT));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", _closeButtonStyle, GUILayout.Width(24), GUILayout.Height(24)))
            {
                _showGiftPopup = false;
                _selectedNPC = null;
            }
            GUILayout.Space(6);
            GUILayout.EndHorizontal();

            // Scrollable gift list
            _giftScrollPosition = GUILayout.BeginScrollView(_giftScrollPosition, GUILayout.Height(GIFT_WINDOW_HEIGHT - HEADER_HEIGHT - 10));

            // === LOVED GIFTS (NPC-specific + Universal) ===
            GUILayout.Space(6);
            DrawSectionHeader("LOVED GIFTS", _lovedColor);
            GUILayout.Space(4);

            // NPC's loved gifts
            foreach (var gift in _selectedNPC.AllLovedGifts)
            {
                DrawGiftItem(gift, _lovedColor);
            }

            // Universal loved gifts
            if (BirthdayCache.UniversalLoved.Count > 0)
            {
                GUILayout.Space(4);
                DrawSubHeader("Universal Loved:", _universalColor);
                foreach (var gift in BirthdayCache.UniversalLoved)
                {
                    DrawGiftItem(gift, _universalColor);
                }
            }

            // Separator
            GUILayout.Space(8);
            DrawSeparator();

            // === LIKED GIFTS (NPC-specific + Universal) ===
            GUILayout.Space(8);
            DrawSectionHeader("LIKED GIFTS", _likedColor);
            GUILayout.Space(4);

            // NPC's liked gifts
            foreach (var gift in _selectedNPC.AllLikedGifts)
            {
                DrawGiftItem(gift, _likedColor);
            }

            // Universal liked gifts
            if (BirthdayCache.UniversalLiked.Count > 0)
            {
                GUILayout.Space(4);
                DrawSubHeader("Universal Liked:", _universalColor);
                foreach (var gift in BirthdayCache.UniversalLiked)
                {
                    DrawGiftItem(gift, _universalColor);
                }
            }

            GUILayout.Space(10);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, GIFT_WINDOW_WIDTH - 24, HEADER_HEIGHT));
        }

        private void DrawSectionHeader(string text, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            var style = new GUIStyle(_sectionHeaderStyle) { normal = { textColor = color } };
            GUILayout.Label(text, style);
            GUILayout.EndHorizontal();
        }

        private void DrawSubHeader(string text, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            var style = new GUIStyle(_hintStyle)
            {
                normal = { textColor = color },
                fontStyle = FontStyle.Bold,
                fontSize = 9
            };
            GUILayout.Label(text, style);
            GUILayout.EndHorizontal();
        }

        private void DrawGiftItem(string gift, Color bulletColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(16);

            // Colored bullet
            var bulletStyle = new GUIStyle(_giftItemStyle) { normal = { textColor = bulletColor } };
            GUILayout.Label("\u2022", bulletStyle, GUILayout.Width(12));

            // Gift name
            GUILayout.Label(gift, _giftItemStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawSeparator()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            var rect = GUILayoutUtility.GetRect(GIFT_WINDOW_WIDTH - 40, 1);
            GUI.color = new Color(_borderColor.r, _borderColor.g, _borderColor.b, 0.4f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, GIFT_WINDOW_WIDTH - 40, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }

        #region Style Initialization

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            try
            {
                CreateTextures();
                CreateStyles();
                _stylesInitialized = true;
                Plugin.Log?.LogInfo("[BirthdayHUD] Styles initialized successfully");
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.LogError($"[BirthdayHUD] Failed to initialize styles: {ex.Message}");
                _windowStyle = GUI.skin.box;
                _stylesInitialized = true;
            }
        }

        private void CreateTextures()
        {
            _windowBackground = MakeTex(4, 4, _bgColor);
            _headerBackground = MakeGradientTex(4, 8, _headerColor, new Color(_headerColor.r * 0.7f, _headerColor.g * 0.7f, _headerColor.b * 0.7f, 1f));
            _itemBackground = MakeTex(4, 4, new Color(0.25f, 0.20f, 0.18f, 0.6f));
            _separatorTex = MakeTex(1, 1, _borderColor);
        }

        private void CreateStyles()
        {
            _windowStyle = new GUIStyle
            {
                normal = { background = _windowBackground, textColor = _textLight },
                padding = new RectOffset(4, 4, 4, 4),
                border = new RectOffset(2, 2, 2, 2)
            };

            _headerStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(2, 2, 2, 2)
            };

            _nameStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _textLight },
                alignment = TextAnchor.MiddleLeft
            };

            _giftedStyle = new GUIStyle(_nameStyle)
            {
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(_textLight.r, _textLight.g, _textLight.b, 0.5f) }
            };

            _hintStyle = new GUIStyle
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                normal = { textColor = _hintColor },
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };

            _closeButtonStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.9f, 0.9f) },
                hover = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _moreButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _borderColor, background = MakeTex(1, 1, new Color(0.3f, 0.25f, 0.22f, 0.8f)) },
                hover = { textColor = Color.white, background = MakeTex(1, 1, new Color(0.5f, 0.35f, 0.3f, 0.9f)) },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 2, 2),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _giftItemStyle = new GUIStyle
            {
                fontSize = 11,
                normal = { textColor = _textLight },
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                padding = new RectOffset(0, 0, 1, 1)
            };

            _sectionHeaderStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _textLight },
                alignment = TextAnchor.MiddleLeft
            };

            _universalItemStyle = new GUIStyle(_giftItemStyle)
            {
                normal = { textColor = _universalColor }
            };
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

        private Texture2D MakeGradientTex(int width, int height, Color topColor, Color bottomColor)
        {
            var tex = new Texture2D(width, height);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                Color rowColor = Color.Lerp(bottomColor, topColor, t);
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = rowColor;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        #endregion

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnBirthdaysUpdated -= OnBirthdaysUpdated;
            }
        }
    }
}
