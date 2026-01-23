using System;
using System.Collections.Generic;
using TheVault.Patches;
using TheVault.Vault;
using HarmonyLib;
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

        // UI State
        private CurrencyCategory _selectedCategory = CurrencyCategory.SeasonalToken;
        private string _depositAmount = "1";
        private string _selectedCurrencyId = "";

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
        private bool _stylesInitialized;
        private Texture2D _windowBackground;
        private Texture2D _rowBackground;
        private Texture2D _rowAltBackground;
        private Texture2D _buttonNormal;
        private Texture2D _buttonHover;
        private Texture2D _withdrawNormal;
        private Texture2D _withdrawHover;
        private Texture2D _tabNormal;
        private Texture2D _tabSelected;

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

        // Window dimensions
        private const float WINDOW_WIDTH = 460f;
        private const float WINDOW_HEIGHT = 480f;

        // Toggle key
        private KeyCode _toggleKey = KeyCode.V;
        private bool _requiresModifier = true; // Requires Ctrl+V by default
        private KeyCode _altToggleKey = KeyCode.F8; // Alternative key for Steam Deck (no modifier)

        public void Initialize(VaultManager vaultManager)
        {
            _vaultManager = vaultManager;
            _isVisible = false;

            // Center window on screen
            float x = (Screen.width - WINDOW_WIDTH) / 2f;
            float y = (Screen.height - WINDOW_HEIGHT) / 2f;
            _windowRect = new Rect(x, y, WINDOW_WIDTH, WINDOW_HEIGHT);

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

                // Also try to disable input through PlayerInput if available
                var playerInputType = Type.GetType("PlayerInput, Assembly-CSharp");
                if (playerInputType != null)
                {
                    var disableMethod = playerInputType.GetMethod("DisableInput",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                        null, new[] { typeof(string) }, null);
                    disableMethod?.Invoke(null, new object[] { PAUSE_ID });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Could not block game input: {ex.Message}");
            }

            Plugin.Log?.LogInfo("Vault UI opened");
        }

        public void Hide()
        {
            _isVisible = false;

            // Re-enable game input
            try
            {
                if (Player.Instance != null)
                {
                    Player.Instance.RemovePauseObject(PAUSE_ID);
                }

                // Re-enable PlayerInput
                var playerInputType = Type.GetType("PlayerInput, Assembly-CSharp");
                if (playerInputType != null)
                {
                    var enableMethod = playerInputType.GetMethod("EnableInput",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                        null, new[] { typeof(string) }, null);
                    enableMethod?.Invoke(null, new object[] { PAUSE_ID });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Could not re-enable game input: {ex.Message}");
            }

            Plugin.Log?.LogInfo("Vault UI closed");
        }

        public void Toggle()
        {
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

        private void Update()
        {
            // Check for toggle key (with modifier)
            bool modifierHeld = !_requiresModifier || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (modifierHeld && Input.GetKeyDown(_toggleKey))
            {
                Toggle();
            }

            // Check for alternative toggle key (no modifier required - for Steam Deck)
            if (_altToggleKey != KeyCode.None && Input.GetKeyDown(_altToggleKey))
            {
                Toggle();
            }

            // Close on Escape
            if (_isVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
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
            if (_stylesInitialized) return;

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

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(15, 15, 10, 15),
                border = new RectOffset(12, 12, 12, 12),
                normal = { background = _windowBackground, textColor = _textColor }
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = _goldColor }
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = _accentColor }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(12, 12, 6, 6),
                normal = { background = _buttonNormal, textColor = _textColor },
                hover = { background = _buttonHover, textColor = Color.white },
                active = { background = _buttonHover, textColor = Color.white }
            };

            _withdrawButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(4, 4, 4, 4),
                alignment = TextAnchor.MiddleCenter,
                normal = { background = _withdrawNormal, textColor = _textColor },
                hover = { background = _withdrawHover, textColor = Color.white },
                active = { background = _withdrawHover, textColor = Color.white }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = _textColor }
            };

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = _goldColor }
            };

            _categoryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(14, 14, 8, 8),
                margin = new RectOffset(3, 3, 0, 0),
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
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(0, 0, 2, 2)
            };

            _closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(20, 20, 10, 10),
                normal = { background = _buttonNormal, textColor = _textColor },
                hover = { background = _buttonHover, textColor = Color.white }
            };

            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 4, 4),
                normal = { textColor = _textColor }
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!_isVisible || _vaultManager == null) return;

            InitializeStyles();

            // Draw shadow/backdrop
            GUI.color = new Color(0, 0, 0, 0.3f);
            GUI.DrawTexture(new Rect(_windowRect.x + 4, _windowRect.y + 4, _windowRect.width, _windowRect.height), Texture2D.whiteTexture);
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
            // Title with decorative elements
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("\u2727 The Vault \u2727", _titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Subtitle
            var subtitleStyle = new GUIStyle(_labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
            subtitleStyle.normal.textColor = _textDimColor;
            GUILayout.Label("Currency Storage System", subtitleStyle);
            GUILayout.Space(12);

            // Category tabs with better spacing
            DrawCategoryTabs();
            GUILayout.Space(8);

            // Divider line
            DrawHorizontalLine(_accentColor, 1);
            GUILayout.Space(8);

            // Main content area with scroll
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(260));
            DrawCurrencyList();
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            DrawHorizontalLine(_accentColor, 1);
            GUILayout.Space(8);

            // Deposit/Withdraw controls
            DrawControls();

            // Close button centered
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", _closeButtonStyle, GUILayout.Width(120), GUILayout.Height(35)))
            {
                Hide();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Make window draggable from header area
            GUI.DragWindow(new Rect(0, 0, WINDOW_WIDTH, 50));
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
            CurrencyCategory.Special
        };

        private void DrawCategoryTabs()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            foreach (CurrencyCategory category in _enabledCategories)
            {
                var style = _selectedCategory == category ? _selectedCategoryStyle : _categoryButtonStyle;
                string icon = GetCategoryIcon(category);
                string label = $"{icon} {GetCategoryDisplayName(category)}";

                if (GUILayout.Button(label, style, GUILayout.MinWidth(100)))
                {
                    _selectedCategory = category;
                    _selectedCurrencyId = "";
                }
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
                _ => category.ToString()
            };
        }

        private int _rowIndex = 0;

        private void DrawCurrencyList()
        {
            var currencies = GetCurrenciesForCategory(_selectedCategory);

            if (currencies.Count == 0)
            {
                GUILayout.Space(20);
                var emptyStyle = new GUIStyle(_labelStyle) { alignment = TextAnchor.MiddleCenter };
                emptyStyle.normal.textColor = _textDimColor;
                GUILayout.Label("No items in this category", emptyStyle);
                return;
            }

            _rowIndex = 0;
            foreach (var kvp in currencies)
            {
                DrawCurrencyRow(kvp.Key, kvp.Value, _rowIndex % 2 == 0);
                _rowIndex++;
            }
        }

        private Dictionary<string, int> GetCurrenciesForCategory(CurrencyCategory category)
        {
            var result = new Dictionary<string, int>();

            // Get all currency definitions for this category
            foreach (var currency in _vaultManager.GetCurrenciesByCategory(category))
            {
                string keyId = currency.Id;
                int amount = 0;

                switch (category)
                {
                    case CurrencyCategory.SeasonalToken:
                        // Map token_spring -> Spring enum
                        string tokenName = keyId.Replace("token_", "");
                        tokenName = char.ToUpper(tokenName[0]) + tokenName.Substring(1);
                        if (Enum.TryParse<SeasonalTokenType>(tokenName, out var tokenType))
                        {
                            amount = _vaultManager.GetSeasonalTokens(tokenType);
                        }
                        result[$"seasonal_{tokenName}"] = amount;
                        break;

                    case CurrencyCategory.CommunityToken:
                        // key_id is like "token_community", we store as "token" in dictionary
                        string communityId = keyId.Replace("token_", "");
                        amount = _vaultManager.GetCommunityTokens(communityId);
                        result[$"community_{communityId}"] = amount;
                        break;

                    case CurrencyCategory.Key:
                        // key_id is like "key_copper", strip the "key_" prefix for storage lookup
                        string keyName = keyId.Replace("key_", "");
                        amount = _vaultManager.GetKeys(keyName);
                        result[$"key_{keyName}"] = amount;
                        break;

                    case CurrencyCategory.Special:
                        string specialId = keyId.Replace("special_", "");
                        amount = _vaultManager.GetSpecial(specialId);
                        result[$"special_{specialId}"] = amount;
                        break;

                    case CurrencyCategory.Orb:
                        string orbId = keyId.Replace("orb_", "");
                        amount = _vaultManager.GetOrbs(orbId);
                        result[$"orb_{orbId}"] = amount;
                        break;

                    case CurrencyCategory.Custom:
                        amount = _vaultManager.GetCustomCurrency(keyId);
                        result[$"custom_{keyId}"] = amount;
                        break;
                }
            }

            return result;
        }

        private void DrawCurrencyRow(string currencyId, int amount, bool isEvenRow)
        {
            bool isSelected = _selectedCurrencyId == currencyId;
            bool autoDepositEnabled = ItemPatches.IsAutoDepositEnabled(currencyId);

            // Row background
            var rowRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            var bgColor = isSelected ? new Color(0.3f, 0.5f, 0.7f, 0.5f) : (isEvenRow ? _rowEvenColor : _rowOddColor);
            GUI.color = bgColor;
            GUI.DrawTexture(rowRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Draw content using absolute positioning within the row
            float yCenter = rowRect.y + (rowRect.height - 26) / 2;
            float xPos = rowRect.x + 8;

            // Auto-deposit toggle button (leftmost) - styled to fit the UI
            float toggleBtnY = rowRect.y + (rowRect.height - 20) / 2;
            var toggleBg = new Texture2D(1, 1);
            toggleBg.SetPixel(0, 0, autoDepositEnabled ? new Color(0.2f, 0.5f, 0.2f, 0.9f) : new Color(0.3f, 0.3f, 0.35f, 0.9f));
            toggleBg.Apply();
            var toggleHover = new Texture2D(1, 1);
            toggleHover.SetPixel(0, 0, autoDepositEnabled ? new Color(0.25f, 0.6f, 0.25f, 1f) : new Color(0.4f, 0.4f, 0.45f, 1f));
            toggleHover.Apply();

            var toggleStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(1, 1, 1, 1),
                normal = { background = toggleBg, textColor = autoDepositEnabled ? new Color(0.5f, 1f, 0.5f) : new Color(0.6f, 0.6f, 0.6f) },
                hover = { background = toggleHover, textColor = Color.white },
                active = { background = toggleHover, textColor = Color.white }
            };

            var toggleRect = new Rect(xPos, toggleBtnY, 20, 20);
            string toggleText = autoDepositEnabled ? "ON" : "--";
            if (GUI.Button(toggleRect, toggleText, toggleStyle))
            {
                ItemPatches.ToggleAutoDeposit(currencyId);
                Plugin.Log?.LogInfo($"[UI] Toggle clicked for {currencyId}");
            }
            xPos += 24;

            // Currency icon (using simple text that renders reliably)
            string icon = GetCurrencyIcon(currencyId);
            var iconStyle = new GUIStyle(_labelStyle) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
            iconStyle.normal.textColor = _accentColor;
            GUI.Label(new Rect(xPos, yCenter, 28, 26), icon, iconStyle);
            xPos += 32;

            // Currency name (wider column for long names like "King's Lost Mine Key")
            string displayName = GetDisplayName(currencyId);
            GUI.Label(new Rect(xPos, yCenter, 140, 26), displayName, _labelStyle);
            xPos += 144;

            // Amount with gold color - make it prominent with "x" prefix
            string amountText = "x" + amount.ToString("N0");
            var amountStyle = new GUIStyle(_valueStyle) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
            GUI.Label(new Rect(xPos, yCenter, 50, 26), amountText, amountStyle);
            xPos += 54;

            // Quick withdraw buttons - positioned from the right
            float btnY = rowRect.y + (rowRect.height - 28) / 2;
            float rightEdge = rowRect.x + rowRect.width - 8;

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

        private string GetCurrencyIcon(string currencyId)
        {
            // Use simple text icons that render reliably in Unity's default font
            if (currencyId.StartsWith("seasonal_"))
            {
                string season = currencyId.Substring("seasonal_".Length).ToLower();
                return season switch
                {
                    "spring" => "[Sp]",
                    "summer" => "[Su]",
                    "fall" => "[Fa]",
                    "winter" => "[Wi]",
                    _ => "[T]"
                };
            }
            else if (currencyId.StartsWith("community_"))
            {
                return "[C]";
            }
            else if (currencyId.StartsWith("key_"))
            {
                return "[K]";
            }
            else if (currencyId.StartsWith("special_"))
            {
                string specialName = currencyId.Substring("special_".Length).ToLower();
                return specialName switch
                {
                    "communitytoken" => "[C]",
                    "doubloon" => "[D]",
                    "blackbottlecap" => "[B]",
                    "redcarnivalticket" => "[R]",
                    "candycornpieces" => "[CC]",
                    "manashard" => "[M]",
                    _ => "[S]"
                };
            }
            else if (currencyId.StartsWith("orb_"))
            {
                return "[O]";
            }
            return "[?]";
        }

        private string GetDisplayName(string currencyId)
        {
            // Convert currency ID to display name
            if (currencyId.StartsWith("seasonal_"))
            {
                return currencyId.Substring("seasonal_".Length) + " Token";
            }
            else if (currencyId.StartsWith("community_"))
            {
                string id = currencyId.Substring("community_".Length);
                return id == "community" ? "Community Token" : "Community " + CapitalizeFirst(id);
            }
            else if (currencyId.StartsWith("key_"))
            {
                string keyName = currencyId.Substring("key_".Length);
                return FormatKeyName(keyName);
            }
            else if (currencyId.StartsWith("special_"))
            {
                string specialName = currencyId.Substring("special_".Length);
                return FormatSpecialName(specialName);
            }
            else if (currencyId.StartsWith("orb_"))
            {
                return CapitalizeFirst(currencyId.Substring("orb_".Length)) + " Orb";
            }

            return currencyId;
        }

        private string CapitalizeFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        private string FormatKeyName(string keyName)
        {
            // Special handling for key names
            return keyName switch
            {
                "copper" => "Copper Key",
                "iron" => "Iron Key",
                "adamant" => "Adamant Key",
                "mithril" => "Mithril Key",
                "sunite" => "Sunite Key",
                "glorite" => "Glorite Key",
                "kingslostmine" => "King's Lost Mine Key",
                _ => CapitalizeFirst(keyName) + " Key"
            };
        }

        private string FormatSpecialName(string specialName)
        {
            // Special handling for special currencies
            return specialName switch
            {
                "communitytoken" => "Community Token",
                "doubloon" => "Doubloon",
                "blackbottlecap" => "Black Bottle Cap",
                "redcarnivalticket" => "Red Carnival Ticket",
                "candycornpieces" => "Candy Corn Pieces",
                "manashard" => "Mana Shard",
                _ => CapitalizeFirst(specialName)
            };
        }

        private void DrawControls()
        {
            if (string.IsNullOrEmpty(_selectedCurrencyId))
            {
                var hintStyle = new GUIStyle(_labelStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Italic
                };
                hintStyle.normal.textColor = _textDimColor;
                GUILayout.Label("Click a row to select, or use quick withdraw buttons", hintStyle);
                return;
            }

            // Selected item info box
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Selection indicator
            var selectedStyle = new GUIStyle(_labelStyle) { fontStyle = FontStyle.Bold };
            selectedStyle.normal.textColor = _accentColor;
            GUILayout.Label($"\u25b6 {GetDisplayName(_selectedCurrencyId)}", selectedStyle);

            GUILayout.Space(15);

            var amountLabelStyle = new GUIStyle(_labelStyle);
            amountLabelStyle.normal.textColor = _textDimColor;
            GUILayout.Label("Qty:", amountLabelStyle, GUILayout.Width(30));
            _depositAmount = GUILayout.TextField(_depositAmount, 6, _textFieldStyle, GUILayout.Width(55));

            GUILayout.Space(8);

            // Withdraw button
            if (GUILayout.Button("Withdraw", _buttonStyle, GUILayout.Width(90)))
            {
                if (int.TryParse(_depositAmount, out int amount) && amount > 0)
                {
                    WithdrawToInventory(_selectedCurrencyId, amount);
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void RemoveCurrency(string currencyId, int amount)
        {
            if (currencyId.StartsWith("seasonal_"))
            {
                string typeName = currencyId.Substring("seasonal_".Length);
                if (Enum.TryParse<SeasonalTokenType>(typeName, out var tokenType))
                {
                    _vaultManager.RemoveSeasonalTokens(tokenType, amount);
                }
            }
            else if (currencyId.StartsWith("community_"))
            {
                _vaultManager.RemoveCommunityTokens(currencyId.Substring("community_".Length), amount);
            }
            else if (currencyId.StartsWith("key_"))
            {
                _vaultManager.RemoveKeys(currencyId.Substring("key_".Length), amount);
            }
            else if (currencyId.StartsWith("special_"))
            {
                _vaultManager.RemoveSpecial(currencyId.Substring("special_".Length), amount);
            }
            else if (currencyId.StartsWith("orb_"))
            {
                _vaultManager.RemoveOrbs(currencyId.Substring("orb_".Length), amount);
            }
            else if (currencyId.StartsWith("custom_"))
            {
                _vaultManager.RemoveCustomCurrency(currencyId.Substring("custom_".Length), amount);
            }
        }

        /// <summary>
        /// Withdraw currency from vault and spawn as items in inventory.
        /// </summary>
        private void WithdrawToInventory(string currencyId, int amount)
        {
            // Check vault has enough
            int current = _vaultManager.GetCurrency(currencyId);
            if (current < amount)
            {
                Plugin.Log?.LogWarning($"Not enough {currencyId} in vault (have {current}, need {amount})");
                return;
            }

            // Get the item ID for this currency
            int itemId = ItemPatches.GetItemForCurrency(currencyId);
            if (itemId < 0)
            {
                Plugin.Log?.LogWarning($"No item mapping found for currency {currencyId}");
                return;
            }

            // Get player inventory
            if (Player.Instance == null)
            {
                Plugin.Log?.LogWarning("Player instance not found");
                return;
            }

            var inventory = Player.Instance.Inventory;
            if (inventory == null)
            {
                Plugin.Log?.LogWarning("Player inventory not found");
                return;
            }

            try
            {
                // Set both global and item-specific withdrawal flags to bypass ALL auto-deposit logic
                // The item-specific flag persists even if there's async processing
                ItemPatches.IsWithdrawing = true;
                ItemPatches.StartWithdrawing(itemId);

                try
                {
                    // Remove from vault first
                    RemoveCurrency(currencyId, amount);

                    // Add to player inventory using AddItem(int id, int amount, bool sendNotification)
                    // This calls the overload that just takes item ID and amount
                    var addItemMethod = AccessTools.Method(inventory.GetType(), "AddItem",
                        new[] { typeof(int), typeof(int), typeof(bool) });

                    if (addItemMethod != null)
                    {
                        addItemMethod.Invoke(inventory, new object[] { itemId, amount, true });
                        Plugin.Log?.LogInfo($"Withdrew {amount} of {currencyId} (itemId={itemId}) to inventory");
                    }
                    else
                    {
                        // Fallback: try simple AddItem(int id)
                        var simpleAddMethod = AccessTools.Method(inventory.GetType(), "AddItem", new[] { typeof(int) });
                        if (simpleAddMethod != null)
                        {
                            for (int i = 0; i < amount; i++)
                            {
                                simpleAddMethod.Invoke(inventory, new object[] { itemId });
                            }
                            Plugin.Log?.LogInfo($"Withdrew {amount} of {currencyId} (itemId={itemId}) to inventory (simple method)");
                        }
                        else
                        {
                            Plugin.Log?.LogError("Could not find AddItem method on inventory");
                            // Re-add to vault since we couldn't add to inventory
                            AddCurrencyBack(currencyId, amount);
                        }
                    }
                }
                finally
                {
                    // Clear global withdrawal flag immediately
                    ItemPatches.IsWithdrawing = false;
                    // Keep item-specific flag set briefly to catch any delayed postfix calls
                    // We'll clear it after a short delay using a coroutine or just leave it
                    // For safety, clear it after inventory operation completes
                    ItemPatches.StopWithdrawing(itemId);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error withdrawing to inventory: {ex.Message}");
                ItemPatches.StopWithdrawing(itemId);
                // Try to re-add to vault on error
                AddCurrencyBack(currencyId, amount);
            }
        }

        /// <summary>
        /// Re-add currency to vault (used if inventory add fails)
        /// </summary>
        private void AddCurrencyBack(string currencyId, int amount)
        {
            if (currencyId.StartsWith("seasonal_"))
            {
                string typeName = currencyId.Substring("seasonal_".Length);
                if (Enum.TryParse<SeasonalTokenType>(typeName, out var tokenType))
                {
                    _vaultManager.AddSeasonalTokens(tokenType, amount);
                }
            }
            else if (currencyId.StartsWith("community_"))
            {
                _vaultManager.AddCommunityTokens(currencyId.Substring("community_".Length), amount);
            }
            else if (currencyId.StartsWith("key_"))
            {
                _vaultManager.AddKeys(currencyId.Substring("key_".Length), amount);
            }
            else if (currencyId.StartsWith("special_"))
            {
                _vaultManager.AddSpecial(currencyId.Substring("special_".Length), amount);
            }
            else if (currencyId.StartsWith("orb_"))
            {
                _vaultManager.AddOrbs(currencyId.Substring("orb_".Length), amount);
            }
            else if (currencyId.StartsWith("custom_"))
            {
                _vaultManager.AddCustomCurrency(currencyId.Substring("custom_".Length), amount);
            }
        }
    }
}
