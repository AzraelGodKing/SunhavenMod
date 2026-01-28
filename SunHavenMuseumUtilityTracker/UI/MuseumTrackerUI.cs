using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SunHavenMuseumUtilityTracker.Data;
using SunHavenMuseumUtilityTracker.Patches;
using UnityEngine;
using Wish;

namespace SunHavenMuseumUtilityTracker.UI
{
    /// <summary>
    /// Main UI window for the Museum Utility Tracker.
    /// </summary>
    public class MuseumTrackerUI : MonoBehaviour
    {
        // Window dimensions
        private const float WINDOW_WIDTH = 550f;
        private const float WINDOW_HEIGHT = 650f;

        // State
        private DonationManager _donationManager;
        private bool _isVisible;
        private Rect _windowRect;
        private Vector2 _scrollPosition;
        private int _windowId;

        // Hotkey
        private KeyCode _toggleKey = KeyCode.M;
        private bool _requireCtrl = true;

        // UI state
        private int _selectedSectionIndex = 0;
        private HashSet<string> _expandedBundles = new HashSet<string>();
        private bool _showOnlyNeeded = false;

        // Icon cache
        private Dictionary<int, Texture2D> _iconCache = new Dictionary<int, Texture2D>();
        private HashSet<int> _failedIconLoads = new HashSet<int>();

        // Styles
        private bool _stylesInitialized;
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionTabStyle;
        private GUIStyle _sectionTabActiveStyle;
        private GUIStyle _bundleHeaderStyle;
        private GUIStyle _bundleHeaderCompleteStyle;
        private GUIStyle _itemRowStyle;
        private GUIStyle _itemNameStyle;
        private GUIStyle _itemDonatedStyle;
        private GUIStyle _itemNeededStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _progressStyle;
        private GUIStyle _checkmarkStyle;
        private GUIStyle _toggleStyle;

        // Textures
        private Texture2D _windowBackground;
        private Texture2D _headerBackground;
        private Texture2D _tabNormal;
        private Texture2D _tabActive;
        private Texture2D _bundleNormal;
        private Texture2D _bundleComplete;
        private Texture2D _itemEven;
        private Texture2D _itemOdd;
        private Texture2D _itemDonated;
        private Texture2D _buttonNormal;
        private Texture2D _buttonHover;
        private Texture2D _progressBg;
        private Texture2D _progressFill;

        // Sun Haven color palette
        private readonly Color _bgDark = new Color(0.12f, 0.13f, 0.16f, 0.98f);
        private readonly Color _bgMedium = new Color(0.16f, 0.17f, 0.21f, 0.95f);
        private readonly Color _bgLight = new Color(0.22f, 0.24f, 0.28f, 0.9f);
        private readonly Color _accentBlue = new Color(0.35f, 0.55f, 0.75f);
        private readonly Color _accentBlueDark = new Color(0.25f, 0.40f, 0.58f);
        private readonly Color _gold = new Color(0.95f, 0.82f, 0.35f);
        private readonly Color _goldDark = new Color(0.75f, 0.62f, 0.25f);
        private readonly Color _textPrimary = new Color(0.92f, 0.90f, 0.85f);
        private readonly Color _textSecondary = new Color(0.65f, 0.65f, 0.70f);
        private readonly Color _textMuted = new Color(0.45f, 0.45f, 0.50f);
        private readonly Color _borderColor = new Color(0.35f, 0.38f, 0.45f, 0.6f);
        private readonly Color _successGreen = new Color(0.35f, 0.70f, 0.40f);
        private readonly Color _neededRed = new Color(0.70f, 0.35f, 0.35f);

        // Rarity colors
        private readonly Dictionary<Data.ItemRarity, Color> _rarityColors = new Dictionary<Data.ItemRarity, Color>
        {
            { Data.ItemRarity.Common, new Color(0.70f, 0.70f, 0.70f) },
            { Data.ItemRarity.Uncommon, new Color(0.40f, 0.75f, 0.40f) },
            { Data.ItemRarity.Rare, new Color(0.40f, 0.60f, 0.90f) },
            { Data.ItemRarity.Epic, new Color(0.70f, 0.45f, 0.85f) },
            { Data.ItemRarity.Legendary, new Color(0.95f, 0.75f, 0.25f) }
        };

        // Section colors
        private readonly Dictionary<string, Color> _sectionColors = new Dictionary<string, Color>
        {
            { "hall_of_gems", new Color(0.55f, 0.75f, 0.90f) },
            { "hall_of_culture", new Color(0.85f, 0.70f, 0.50f) },
            { "aquarium", new Color(0.40f, 0.75f, 0.85f) }
        };

        public bool IsVisible => _isVisible;

        public void Initialize(DonationManager donationManager)
        {
            _donationManager = donationManager;
            _isVisible = false;
            _windowId = GetHashCode();

            // Center window
            float x = (Screen.width - WINDOW_WIDTH) / 2f;
            float y = (Screen.height - WINDOW_HEIGHT) / 2f;
            _windowRect = new Rect(x, y, WINDOW_WIDTH, WINDOW_HEIGHT);

            Plugin.Log?.LogInfo("MuseumTrackerUI initialized");
        }

        public void SetToggleKey(KeyCode key, bool requireCtrl)
        {
            _toggleKey = key;
            _requireCtrl = requireCtrl;
        }

        public void Toggle()
        {
            if (!PlayerPatches.IsDataLoaded)
            {
                Plugin.Log?.LogWarning("Cannot toggle UI: data not loaded");
                return;
            }

            if (_isVisible)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            _isVisible = true;

            // Pause game
            if (Player.Instance != null)
                Player.Instance.AddPauseObject("MuseumTracker_UI");

            Plugin.Log?.LogInfo("Museum Tracker UI opened");
        }

        public void Hide()
        {
            _isVisible = false;

            // Unpause game
            if (Player.Instance != null)
                Player.Instance.RemovePauseObject("MuseumTracker_UI");

            Plugin.Log?.LogInfo("Museum Tracker UI closed");
        }

        private void Update()
        {
            // Close on Escape
            if (_isVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        private void OnGUI()
        {
            if (!_isVisible || _donationManager == null || !PlayerPatches.IsDataLoaded)
                return;

            InitializeStyles();

            // Draw main window
            _windowRect = GUI.Window(_windowId, _windowRect, DrawWindow, "", _windowStyle);
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            // Create textures
            _windowBackground = MakeGradientTex(8, 64, _bgDark, new Color(_bgDark.r * 0.9f, _bgDark.g * 0.9f, _bgDark.b * 0.9f, _bgDark.a));
            _headerBackground = MakeGradientTex(8, 32, _bgLight, _bgMedium);
            _tabNormal = MakeBorderedTex(8, 8, _bgMedium, _borderColor, 1);
            _tabActive = MakeBorderedTex(8, 8, _accentBlueDark, _accentBlue, 1);
            _bundleNormal = MakeBorderedTex(8, 8, new Color(_bgLight.r, _bgLight.g, _bgLight.b, 0.7f), _borderColor, 1);
            _bundleComplete = MakeBorderedTex(8, 8, new Color(0.20f, 0.30f, 0.22f, 0.8f), _successGreen, 1);
            _itemEven = MakeTex(1, 1, new Color(_bgMedium.r, _bgMedium.g, _bgMedium.b, 0.4f));
            _itemOdd = MakeTex(1, 1, new Color(_bgLight.r * 0.85f, _bgLight.g * 0.85f, _bgLight.b * 0.85f, 0.3f));
            _itemDonated = MakeTex(1, 1, new Color(0.18f, 0.28f, 0.20f, 0.4f));
            _buttonNormal = MakeBorderedTex(8, 8, _accentBlueDark, new Color(_accentBlue.r, _accentBlue.g, _accentBlue.b, 0.4f), 1);
            _buttonHover = MakeBorderedTex(8, 8, _accentBlue, new Color(_accentBlue.r, _accentBlue.g, _accentBlue.b, 0.8f), 1);
            _progressBg = MakeTex(1, 1, new Color(0.1f, 0.1f, 0.12f, 0.8f));
            _progressFill = MakeTex(1, 1, _successGreen);

            // Window style
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = _windowBackground, textColor = _textPrimary },
                padding = new RectOffset(12, 12, 12, 12),
                border = new RectOffset(8, 8, 8, 8)
            };

            // Title style
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = _gold }
            };

            // Section tab styles
            _sectionTabStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _tabNormal, textColor = _textSecondary },
                hover = { background = _tabActive, textColor = _textPrimary },
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(8, 8, 6, 6)
            };

            _sectionTabActiveStyle = new GUIStyle(_sectionTabStyle)
            {
                normal = { background = _tabActive, textColor = _gold }
            };

            // Bundle header style
            _bundleHeaderStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _bundleNormal, textColor = _textPrimary },
                hover = { background = _bundleNormal, textColor = Color.white },
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 8, 8)
            };

            _bundleHeaderCompleteStyle = new GUIStyle(_bundleHeaderStyle)
            {
                normal = { background = _bundleComplete, textColor = _successGreen }
            };

            // Item styles
            _itemRowStyle = new GUIStyle
            {
                padding = new RectOffset(15, 10, 4, 4),
                margin = new RectOffset(0, 0, 1, 1)
            };

            _itemNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = _textPrimary }
            };

            _itemDonatedStyle = new GUIStyle(_itemNameStyle)
            {
                normal = { textColor = _successGreen },
                fontStyle = FontStyle.Italic
            };

            _itemNeededStyle = new GUIStyle(_itemNameStyle)
            {
                normal = { textColor = _textSecondary }
            };

            // Button style
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _buttonNormal, textColor = _textPrimary },
                hover = { background = _buttonHover, textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 6, 6)
            };

            // Label style
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = _textPrimary }
            };

            // Progress style
            _progressStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = _textSecondary },
                alignment = TextAnchor.MiddleRight
            };

            // Checkmark style
            _checkmarkStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _successGreen },
                alignment = TextAnchor.MiddleCenter
            };

            // Toggle style
            _toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = 11,
                normal = { textColor = _textSecondary }
            };

            _stylesInitialized = true;
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
                Color rowColor = Color.Lerp(topColor, bottomColor, t);
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = rowColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeBorderedTex(int width, int height, Color fillColor, Color borderColor, int borderWidth = 1)
        {
            var tex = new Texture2D(width, height);
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

        // Cached reflection for icon loading
        private static bool _reflectionInitialized;
        private static Type _itemDatabaseType;
        private static PropertyInfo _itemDatabaseInstanceProp;
        private static MethodInfo _getItemMethod;

        /// <summary>
        /// Initialize reflection for accessing ItemDatabase.
        /// </summary>
        private void InitializeIconReflection()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            try
            {
                // Find ItemDatabase type
                _itemDatabaseType = AccessTools.TypeByName("Wish.ItemDatabase");
                if (_itemDatabaseType == null)
                {
                    Plugin.Log?.LogDebug("Could not find Wish.ItemDatabase type");
                    return;
                }

                // Find Instance property
                _itemDatabaseInstanceProp = _itemDatabaseType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (_itemDatabaseInstanceProp == null)
                {
                    Plugin.Log?.LogDebug("Could not find ItemDatabase.Instance property");
                    return;
                }

                // Find GetItem method
                _getItemMethod = _itemDatabaseType.GetMethod("GetItem", new[] { typeof(int) });
                if (_getItemMethod == null)
                {
                    Plugin.Log?.LogDebug("Could not find ItemDatabase.GetItem method");
                    return;
                }

                Plugin.Log?.LogInfo("Icon reflection initialized successfully");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"Failed to initialize icon reflection: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the icon for an item from the game database, with caching.
        /// </summary>
        private Texture2D GetItemIcon(int gameItemId)
        {
            // Return cached icon if available
            if (_iconCache.TryGetValue(gameItemId, out var cachedIcon))
                return cachedIcon;

            // Skip if we already failed to load this icon
            if (_failedIconLoads.Contains(gameItemId))
                return null;

            // Initialize reflection on first use
            InitializeIconReflection();

            try
            {
                // Get ItemDatabase instance via reflection
                if (_itemDatabaseInstanceProp != null && _getItemMethod != null)
                {
                    var instance = _itemDatabaseInstanceProp.GetValue(null);
                    if (instance != null)
                    {
                        var item = _getItemMethod.Invoke(instance, new object[] { gameItemId });
                        if (item != null)
                        {
                            // Get ItemData via ItemData() method
                            var itemDataMethod = item.GetType().GetMethod("ItemData", BindingFlags.Public | BindingFlags.Instance);
                            if (itemDataMethod != null)
                            {
                                var itemData = itemDataMethod.Invoke(item, null);
                                if (itemData != null)
                                {
                                    // Try to get icon field
                                    var iconField = itemData.GetType().GetField("icon", BindingFlags.Public | BindingFlags.Instance);
                                    if (iconField != null)
                                    {
                                        var sprite = iconField.GetValue(itemData) as Sprite;
                                        if (sprite != null && sprite.texture != null)
                                        {
                                            _iconCache[gameItemId] = sprite.texture;
                                            return sprite.texture;
                                        }
                                    }

                                    // Try property if field didn't work
                                    var iconProp = itemData.GetType().GetProperty("icon", BindingFlags.Public | BindingFlags.Instance);
                                    if (iconProp != null)
                                    {
                                        var sprite = iconProp.GetValue(itemData) as Sprite;
                                        if (sprite != null && sprite.texture != null)
                                        {
                                            _iconCache[gameItemId] = sprite.texture;
                                            return sprite.texture;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"Failed to load icon for item {gameItemId}: {ex.Message}");
            }

            // Mark as failed so we don't keep trying
            _failedIconLoads.Add(gameItemId);
            return null;
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // Header
            DrawHeader();

            GUILayout.Space(10);

            // Section tabs
            DrawSectionTabs();

            GUILayout.Space(10);

            // Filter options
            DrawFilterOptions();

            GUILayout.Space(5);

            // Content
            DrawContent();

            GUILayout.Space(10);

            // Footer
            DrawFooter();

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, WINDOW_WIDTH, 50));
        }

        private void DrawHeader()
        {
            var headerRect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(headerRect, _headerBackground, ScaleMode.StretchToFill);

            GUILayout.BeginArea(headerRect);
            GUILayout.BeginHorizontal();

            GUILayout.Space(10);

            // Title
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Museum Tracker", _titleStyle, GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Overall progress
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            var (donated, total) = _donationManager.GetOverallStats();
            float percent = _donationManager.GetOverallCompletionPercent();
            GUILayout.Label($"{donated}/{total} ({percent:F1}%)", _progressStyle, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Close button
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            var closeStyle = new GUIStyle(_buttonStyle);
            closeStyle.normal.textColor = new Color(0.9f, 0.5f, 0.5f);
            closeStyle.hover.textColor = new Color(1f, 0.6f, 0.6f);
            closeStyle.fontSize = 14;
            if (GUILayout.Button("X", closeStyle, GUILayout.Width(32), GUILayout.Height(32)))
            {
                Hide();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawSectionTabs()
        {
            var sections = MuseumContent.GetAllSections();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                var stats = _donationManager.GetSectionStats(section);
                bool isComplete = _donationManager.IsSectionComplete(section);

                var style = i == _selectedSectionIndex ? _sectionTabActiveStyle : _sectionTabStyle;
                string label = $"{section.Name}\n{stats.donated}/{stats.total}";

                if (isComplete)
                {
                    label = $"✓ {section.Name}\n{stats.donated}/{stats.total}";
                }

                if (GUILayout.Button(label, style, GUILayout.Width(160), GUILayout.Height(45)))
                {
                    _selectedSectionIndex = i;
                }

                if (i < sections.Count - 1)
                    GUILayout.Space(5);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawFilterOptions()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            _showOnlyNeeded = GUILayout.Toggle(_showOnlyNeeded, " Show only needed items", _toggleStyle);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawContent()
        {
            var sections = MuseumContent.GetAllSections();
            if (_selectedSectionIndex >= sections.Count)
                _selectedSectionIndex = 0;

            var section = sections[_selectedSectionIndex];

            // Scroll view
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            foreach (var bundle in section.Bundles)
            {
                DrawBundle(bundle);
                GUILayout.Space(5);
            }

            GUILayout.EndScrollView();
        }

        private void DrawBundle(MuseumBundle bundle)
        {
            bool isComplete = _donationManager.IsBundleComplete(bundle);
            bool isExpanded = _expandedBundles.Contains(bundle.Id);
            var stats = _donationManager.GetBundleStats(bundle);

            // Skip if filtering and bundle is complete
            if (_showOnlyNeeded && isComplete)
                return;

            // Bundle header
            var headerStyle = isComplete ? _bundleHeaderCompleteStyle : _bundleHeaderStyle;
            string expandIcon = isExpanded ? "▼" : "►";
            string completeIcon = isComplete ? " ✓" : "";
            string label = $"{expandIcon} {bundle.Name}{completeIcon} ({stats.donated}/{stats.total})";

            if (GUILayout.Button(label, headerStyle, GUILayout.ExpandWidth(true), GUILayout.Height(35)))
            {
                if (isExpanded)
                    _expandedBundles.Remove(bundle.Id);
                else
                    _expandedBundles.Add(bundle.Id);
            }

            // Draw items if expanded
            if (isExpanded)
            {
                DrawBundleItems(bundle);
            }
        }

        private void DrawBundleItems(MuseumBundle bundle)
        {
            const float ICON_SIZE = 28f;

            int index = 0;
            foreach (var item in bundle.Items)
            {
                bool isDonated = _donationManager.HasDonated(item.Id);

                // Skip donated items if filtering
                if (_showOnlyNeeded && isDonated)
                {
                    index++;
                    continue;
                }

                // Row with background color
                var bgTex = isDonated ? _itemDonated : (index % 2 == 0 ? _itemEven : _itemOdd);

                GUILayout.BeginHorizontal(_itemRowStyle, GUILayout.Height(32));

                // Draw background manually
                var lastRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == UnityEngine.EventType.Repaint && bgTex != null)
                {
                    GUI.DrawTexture(lastRect, bgTex);
                }

                GUILayout.Space(10);

                // Checkbox for manual toggle
                bool newDonated = GUILayout.Toggle(isDonated, "", GUILayout.Width(20));
                if (newDonated != isDonated)
                {
                    _donationManager.ToggleDonated(item.Id);
                }

                GUILayout.Space(5);

                // Item icon
                var icon = GetItemIcon(item.GameItemId);
                if (icon != null)
                {
                    var iconRect = GUILayoutUtility.GetRect(ICON_SIZE, ICON_SIZE, GUILayout.Width(ICON_SIZE), GUILayout.Height(ICON_SIZE));
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                    GUILayout.Space(8);
                }
                else
                {
                    // Placeholder space if no icon
                    GUILayout.Space(ICON_SIZE + 8);
                }

                // Item name with rarity color
                var rarityColor = _rarityColors.TryGetValue(item.Rarity, out var c) ? c : _textPrimary;
                var nameStyle = new GUIStyle(_labelStyle)
                {
                    fontSize = 12,
                    normal = { textColor = isDonated ? _successGreen : rarityColor }
                };
                if (isDonated) nameStyle.fontStyle = FontStyle.Italic;

                GUILayout.Label(item.Name, nameStyle, GUILayout.ExpandWidth(true));

                // Rarity badge
                var rarityStyle = new GUIStyle(_labelStyle)
                {
                    fontSize = 9,
                    normal = { textColor = rarityColor }
                };
                GUILayout.Label(item.Rarity.ToString(), rarityStyle, GUILayout.Width(60));

                // Status icon
                if (isDonated)
                {
                    GUILayout.Label("✓", _checkmarkStyle, GUILayout.Width(25));
                }
                else
                {
                    var neededStyle = new GUIStyle(_checkmarkStyle) { normal = { textColor = _neededRed } };
                    GUILayout.Label("○", neededStyle, GUILayout.Width(25));
                }

                GUILayout.Space(10);

                GUILayout.EndHorizontal();

                index++;
            }
        }

        private void DrawFooter()
        {
            var sections = MuseumContent.GetAllSections();
            var section = sections[_selectedSectionIndex];
            var stats = _donationManager.GetSectionStats(section);
            float percent = _donationManager.GetSectionCompletionPercent(section);

            GUILayout.BeginHorizontal();

            // Section progress bar
            var barRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));

            // Background
            GUI.DrawTexture(barRect, _progressBg);

            // Fill
            var fillRect = new Rect(barRect.x, barRect.y, barRect.width * (percent / 100f), barRect.height);
            var sectionColor = _sectionColors.TryGetValue(section.Id, out var sc) ? sc : _successGreen;
            GUI.color = sectionColor;
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Text overlay
            var progressLabel = new GUIStyle(_labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(barRect, $"{section.Name}: {stats.donated}/{stats.total} ({percent:F1}%)", progressLabel);

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Hotkey hint
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var hintStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 10,
                normal = { textColor = _textMuted }
            };
            GUILayout.Label($"Press {(_requireCtrl ? "Ctrl+" : "")}{_toggleKey} to toggle | Escape to close", hintStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}
