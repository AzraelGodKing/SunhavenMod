using System;
using System.Collections.Generic;
using System.Reflection;
using SenpaisChest.Config;
using SenpaisChest.Data;
using SunhavenMods.Shared;
using SunHavenMuseumUtilityTracker;
using SunHavenMuseumUtilityTracker.Data;
using UnityEngine;
using Wish;

namespace SenpaisChest.UI
{
    [UnityEngine.DefaultExecutionOrder(-30000)]
    public class SmartChestUI : MonoBehaviour
    {
        private const string PAUSE_ID = "SenpaisChest_Config";

        private SmartChestManager _manager;
        private bool _isVisible;
        private Chest _currentChest;
        private SmartChestData _currentData;
        private string _chestId;

        // Window
        private Rect _windowRect = new Rect(100, 100, 420, 500);

        // Note panel shown when chest is open ("Press F9 to configure")
        private GameObject _notePanelRoot;
        private Rect _noteRect;
        private bool _noteRectDirty = true;
        private float _contentHeight = 500f;

        // Add rule form state
        private int _selectedRuleType;
        private string _itemIdInput = "";
        private int _selectedCategory;
        private int _selectedItemType;
        private int _selectedProperty;
        private int _selectedGroup;

        // Item search state
        private string _lastSearchQuery = "";
        private List<KeyValuePair<int, string>> _searchResults = new List<KeyValuePair<int, string>>();
        private Vector2 _searchScrollPos;
        private int _selectedItemId = -1;
        private string _selectedItemName = "";

        // Groups management
        private bool _groupsWindowVisible;
        private Rect _groupsWindowRect = new Rect(150, 150, 400, 450);
        private ItemGroup _editingGroup;
        private string _newGroupName = "";
        private string _groupItemSearch = "";
        private string _groupItemSearchLast = "";
        private List<KeyValuePair<int, string>> _groupSearchResults = new List<KeyValuePair<int, string>>();
        private Vector2 _groupSearchScroll;
        private Vector2 _groupListScroll;
        private Vector2 _groupItemsScroll;

        // Dropdown options
        private static readonly string[] RuleTypeNames = { "By Item", "By Category", "By Item Type", "By Property", "By Group" };
        private static readonly string[] CategoryNames = { "Equip", "Use", "Craftable", "Monster", "Furniture", "Quest" };
        private static readonly string[] ItemTypeNames = { "Normal", "Armor", "Food", "Fish", "Crop", "WateringCan", "Animal", "Pet", "Tool" };
        private static readonly string[] PropertyNames = { "isGem", "isForageable", "isAnimalProduct", "isMeal", "isFruit", "isArtisanryItem", "isPotion", "isNotDonated" };
        private static readonly string[] PropertyDisplayNames = { "Gems", "Forageables", "Animal Products", "Meals", "Fruits", "Artisanry Items", "Potions", "Museum (Not Donated)" };
        private const string SearchFieldControlName = "SmartChestItemSearch";

        // Color palette — dark navy theme with gold accents
        private readonly Color _bgDark = new Color(0.15f, 0.16f, 0.24f, 1f);
        private readonly Color _borderGold = new Color(0.75f, 0.65f, 0.30f, 1f);
        private readonly Color _goldText = new Color(0.95f, 0.85f, 0.35f, 1f);
        private readonly Color _whiteText = new Color(0.95f, 0.95f, 0.95f, 1f);
        private readonly Color _dimText = new Color(0.6f, 0.6f, 0.7f, 1f);
        private readonly Color _greenActive = new Color(0.20f, 0.55f, 0.45f, 1f);
        private readonly Color _greenHover = new Color(0.25f, 0.65f, 0.52f, 1f);
        private readonly Color _greenBright = new Color(0.30f, 0.70f, 0.55f, 1f);
        private readonly Color _redDanger = new Color(0.75f, 0.20f, 0.20f, 1f);
        private readonly Color _redHover = new Color(0.85f, 0.28f, 0.28f, 1f);
        private readonly Color _btnInactive = new Color(0.22f, 0.24f, 0.34f, 1f);
        private readonly Color _btnHover = new Color(0.30f, 0.32f, 0.44f, 1f);
        private readonly Color _ruleBoxColor = new Color(0.18f, 0.19f, 0.28f, 1f);
        private readonly Color _fieldBg = new Color(0.12f, 0.13f, 0.22f, 1f);
        private readonly Color _museumHighlight = new Color(0.35f, 0.65f, 0.85f, 1f);
        private readonly Color _parchmentLight = new Color(0.96f, 0.93f, 0.86f, 0.98f);
        private readonly Color _parchment = new Color(0.92f, 0.87f, 0.78f, 0.97f);
        private readonly Color _parchmentDark = new Color(0.85f, 0.78f, 0.65f, 0.95f);
        private readonly Color _woodDark = new Color(0.35f, 0.25f, 0.15f);
        private readonly Color _woodMedium = new Color(0.50f, 0.38f, 0.25f);
        private readonly Color _woodLight = new Color(0.65f, 0.52f, 0.38f);
        private readonly Color _goldPale = new Color(0.92f, 0.82f, 0.55f);
        private readonly Color _goldRich = new Color(0.72f, 0.55f, 0.20f);
        private readonly Color _borderWood = new Color(0.45f, 0.35f, 0.22f, 0.9f);
        private readonly Color _chestTextDark = new Color(0.08f, 0.06f, 0.05f, 1f);
        private readonly Color _chestTextDim = new Color(0.14f, 0.11f, 0.09f, 1f);
        private readonly Color _crimsonTitle = new Color(0.52f, 0.18f, 0.12f, 1f);
        private readonly Color _crimsonTitleLight = new Color(0.58f, 0.22f, 0.16f, 1f);
        private readonly Color _innerBorderCrimson = new Color(0.42f, 0.18f, 0.14f, 1f);

        // Textures
        private Texture2D _solidBg;
        private Texture2D _windowBg;
        private Texture2D _ruleBg;
        private Texture2D _btnInactiveTex;
        private Texture2D _btnHoverTex;
        private Texture2D _btnActiveTex;
        private Texture2D _btnActiveHoverTex;
        private Texture2D _redBtnTex;
        private Texture2D _redBtnHoverTex;
        private Texture2D _greenBtnTex;
        private Texture2D _greenBtnHoverTex;
        private Texture2D _closeBtnTex;
        private Texture2D _closeBtnHoverTex;
        private Texture2D _fieldBgTex;
        private Texture2D _chestParchmentBg;
        private Texture2D _chestWoodBorderTex;
        private Texture2D _chestRuleBoxTex;
        private Texture2D _chestSelectorTex;
        private Texture2D _chestSelectorHoverTex;
        private Texture2D _chestSelectorActiveTex;
        private Texture2D _chestAddBtnTex;
        private Texture2D _chestAddBtnHoverTex;
        private Texture2D _chestWoodBtnTex;
        private Texture2D _chestWoodBtnHoverTex;
        private Texture2D _chestDangerBtnTex;
        private Texture2D _chestDangerBtnHoverTex;
        private Texture2D _chestFieldBgTex;
        private Texture2D _chestGoldLineTex;
        private Texture2D _noteBannerBg;
        private Texture2D _noteBannerBorderTex;
        private Texture2D _configTitleBarTex;
        private Texture2D _configContentBg;
        private Texture2D _configGoldSeparatorTex;
        private Texture2D _configSectionHeaderTex;
        private Texture2D _configSearchFieldTex;
        private Texture2D _configRemoveBtnTex;
        private Texture2D _configRemoveBtnHoverTex;
        private Texture2D _configCloseBtnTex;
        private Texture2D _configCloseBtnHoverTex;
        private Texture2D _configWindowBg;
        private Texture2D _configInnerBorderTex;

        // Styles
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _chestTitleStyle;
        private GUIStyle _chestLabelStyle;
        private GUIStyle _chestLabelBoldStyle;
        private GUIStyle _chestLabelDimStyle;
        private GUIStyle _chestSectionHeaderStyle;
        private GUIStyle _chestToggleStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _labelBoldStyle;
        private GUIStyle _labelDimStyle;
        private GUIStyle _ruleBoxStyle;
        private GUIStyle _ruleTextStyle;
        private GUIStyle _removeRuleBtnStyle;
        private GUIStyle _closeButtonStyle;
        private GUIStyle _toggleStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _selectorStyle;
        private GUIStyle _selectorActiveStyle;
        private GUIStyle _addButtonStyle;
        private GUIStyle _dangerButtonStyle;
        private GUIStyle _closeBottomButtonStyle;
        private GUIStyle _searchResultStyle;
        private GUIStyle _searchResultSelectedStyle;
        private GUIStyle _chestRuleBoxStyle;
        private GUIStyle _chestRuleTextStyle;
        private GUIStyle _chestSelectorStyle;
        private GUIStyle _chestSelectorActiveStyle;
        private GUIStyle _chestAddButtonStyle;
        private GUIStyle _chestDangerButtonStyle;
        private GUIStyle _chestCloseButtonStyle;
        private GUIStyle _chestSearchFieldStyle;
        private GUIStyle _chestRemoveRuleBtnStyle;
        private GUIStyle _chestSearchResultStyle;
        private GUIStyle _chestSearchResultSelectedStyle;
        private GUIStyle _noteBannerStyle;
        private GUIStyle _configTitleStyle;
        private GUIStyle _configSectionHeaderBoxStyle;
        private GUIStyle _configSearchFieldStyle;
        private GUIStyle _configRemoveButtonStyle;
        private GUIStyle _configCloseBottomStyle;
        private GUIStyle _configWindowStyle;
        private bool _stylesInitialized;

        public bool IsVisible => _isVisible;
        /// <summary>True when config is drawn inside the chest UI panel (we should not block closing the chest).</summary>
        public bool IsEmbedded => false;

        public void Initialize(SmartChestManager manager)
        {
            _manager = manager;
        }

        public void Show()
        {
            _isVisible = true;
        }

        /// <summary>Full cleanup when chest closes. Clears note and config, closes chest.</summary>
        public void Hide()
        {
            _isVisible = false;
            BlockGameInput(false);
            var chestToClose = _currentChest;
            _currentChest = null;
            _currentData = null;
            _notePanelRoot = null;
            _noteRectDirty = true;

            Plugin.CurrentInteractingChest = null;
            SaveIfDirty();

            if (chestToClose != null)
            {
                try { chestToClose.EndInteract(0); }
                catch (Exception ex) { Plugin.Log?.LogDebug($"[SmartChestUI] EndInteract on close: {ex.Message}"); }
            }
        }

        /// <summary>Closes the config window only. Chest stays open and note remains visible.</summary>
        public void HideConfig()
        {
            SaveIfDirty(); // Ensure rules are saved before closing
            _isVisible = false;
            _groupsWindowVisible = false;
            BlockGameInput(false);
        }

        /// <summary>Block/unblock game input so only Senpai's Chest UI receives it (e.g. Backspace won't close chest).</summary>
        private void BlockGameInput(bool block)
        {
            try
            {
                if (Player.Instance != null)
                {
                    if (block)
                        Player.Instance.AddPauseObject(PAUSE_ID);
                    else
                        Player.Instance.RemovePauseObject(PAUSE_ID);
                }
                var playerInputType = Type.GetType("PlayerInput, Assembly-CSharp");
                if (playerInputType != null)
                {
                    var method = playerInputType.GetMethod(block ? "DisableInput" : "EnableInput",
                        BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                    method?.Invoke(null, new object[] { PAUSE_ID });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[SmartChestUI] BlockGameInput failed: {ex.Message}");
            }
        }

        public void Toggle()
        {
            if (_isVisible)
                Hide();
            else
                Show();
        }

        public void ToggleForChest(Chest chest)
        {
            if (_isVisible && _currentChest == chest)
            {
                HideConfig();
                return;
            }

            _currentChest = chest;
            _chestId = SmartChestManager.GetChestId(chest);

            if (string.IsNullOrEmpty(_chestId))
            {
                Plugin.Log?.LogWarning("Cannot configure chest: no valid ID");
                return;
            }

            string chestName = GetChestName(chest);
            _currentData = _manager.GetOrCreateSmartChest(_chestId, chestName);

            // Reset add-rule form
            _selectedRuleType = 0;
            _itemIdInput = "";
            _lastSearchQuery = "";
            _searchResults.Clear();
            _selectedItemId = -1;
            _selectedItemName = "";
            _searchScrollPos = Vector2.zero;
            _selectedCategory = 0;
            _selectedItemType = 0;
            _selectedProperty = 0;

            _isVisible = true;
            BlockGameInput(true);
            PositionWindowNextToChestPanel();
        }

        /// <summary>
        /// Shows the note ("Press F9 to configure") in the given panel when chest is open. Config opens only via F9.
        /// </summary>
        public void ShowNoteForChest(Chest chest, GameObject panelRoot)
        {
            _currentChest = chest;
            _chestId = SmartChestManager.GetChestId(chest);
            if (string.IsNullOrEmpty(_chestId))
            {
                Plugin.Log?.LogWarning("[SmartChestUI] Cannot show note: no valid chest ID");
                return;
            }
            // Do not call GetOrCreateSmartChest here; chest is only registered when user opens config (F9)
            _currentData = null;
            _notePanelRoot = panelRoot;
            _noteRectDirty = true;
            _isVisible = false; // Config not shown; only note
            Plugin.Log?.LogDebug($"[SmartChestUI] ShowNoteForChest: chest={chest != null}, panelRoot={panelRoot != null}");
        }

        /// <summary>
        /// Positions the config window at the bottom of the screen (same area as embedded panel)
        /// when the chest UI is open, so config is always in the same place.
        /// </summary>
        private void PositionWindowNextToChestPanel()
        {
            if (_currentChest == null) return;

            // Place floating window at bottom of screen to match embedded panel position
            const float bottomMargin = 8f;
            const float windowHeight = 400f;
            float y = bottomMargin;
            float h = Mathf.Min(windowHeight, Screen.height - bottomMargin);
            _windowRect.width = 420f;
            _windowRect.height = h;
            _windowRect.x = Mathf.Clamp((Screen.width - _windowRect.width) * 0.5f, 0, Screen.width - _windowRect.width);
            _windowRect.y = y; // GUI coords: y=0 is bottom, so this is just above bottom margin
        }

        /// <summary>
        /// Delegates to SmartChestManager.GetChestName which caches the Chest.data field.
        /// </summary>
        private string GetChestName(Chest chest)
        {
            return SmartChestManager.GetChestName(chest);
        }

        private void OnGUI()
        {
            if (!_stylesInitialized)
                InitializeStyles();

            // Note panel: "Press F9 to configure rules" when chest is open and config not visible
            if (_notePanelRoot != null && !_isVisible)
            {
                if (!_notePanelRoot.activeInHierarchy)
                {
                    _notePanelRoot = null;
                    return;
                }
                if (_noteRectDirty || Event.current.type == UnityEngine.EventType.Layout)
                {
                    _noteRect = GetScreenRectFromTransform(_notePanelRoot.transform);
                    _noteRectDirty = false;
                }
                Rect drawRect = _noteRect;
                if (drawRect.width <= 0 || drawRect.height <= 0)
                {
                    drawRect = new Rect((Screen.width - 340f) * 0.5f, 8f, 340f, 34f);
                }
                drawRect = ClampRectToScreen(drawRect);

                GUI.BeginGroup(drawRect);
                DrawNoteContent(drawRect.width, drawRect.height);
                GUI.EndGroup();
                return;
            }

            if (!_isVisible || _currentData == null)
                return;

            // Floating config window (opened via F9)
            float maxHeight = Screen.height - 40f;
            _windowRect.height = Mathf.Clamp(_contentHeight, 300f, maxHeight);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);

            _windowRect = GUI.Window(
                PluginInfo.PLUGIN_GUID.GetHashCode(),
                _windowRect,
                DrawWindow,
                "",
                _configWindowStyle);

            if (_groupsWindowVisible)
            {
                _groupsWindowRect = GUI.Window(
                    PluginInfo.PLUGIN_GUID.GetHashCode() + 1,
                    _groupsWindowRect,
                    DrawGroupsWindow,
                    "Manage Groups",
                    _configWindowStyle);
            }
        }

        /// <summary>
        /// Converts a RectTransform to screen-space Rect (Unity GUI: origin bottom-left).
        /// </summary>
        private static Rect GetScreenRectFromTransform(Transform transform)
        {
            var rt = transform as RectTransform;
            if (rt == null) return new Rect(0, 0, 400, 400);
            var canvas = rt.GetComponentInParent<Canvas>();
            
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            
            Vector2 min, max;
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // For ScreenSpaceOverlay, GetWorldCorners returns screen coordinates
                // corners[0] = bottom-left, corners[1] = top-left, corners[2] = top-right, corners[3] = bottom-right
                min = new Vector2(corners[0].x, corners[0].y); // bottom-left
                max = new Vector2(corners[2].x, corners[2].y); // top-right
            }
            else
            {
                // For Camera or WorldSpace, convert world to screen
                var cam = canvas != null ? canvas.worldCamera : Camera.main;
                min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            }
            
            // Convert to GUI coordinates (bottom-left origin)
            // Unity GUI uses bottom-left origin, so Y needs to be flipped
            float x = min.x;
            float y = Screen.height - max.y; // Flip Y: GUI origin is bottom-left
            float width = max.x - min.x;
            float height = max.y - min.y;
            
            Rect result = new Rect(x, y, width, height);
            return result;
        }

        /// <summary>
        /// Clamps a rect to stay fully within the visible screen (Unity GUI: bottom-left origin).
        /// </summary>
        private static Rect ClampRectToScreen(Rect rect)
        {
            float w = Mathf.Min(rect.width, Screen.width);
            float h = Mathf.Min(rect.height, Screen.height);
            float x = Mathf.Clamp(rect.x, 0, Screen.width - w);
            float y = Mathf.Clamp(rect.y, 0, Screen.height - h);
            return new Rect(x, y, w, h);
        }

        private void DrawWindow(int windowId)
        {
            DrawConfigContent(new Rect(0, 0, _windowRect.width, _windowRect.height), withWindowChrome: true);
        }

        private void DrawConfigContent(Rect rect, bool withWindowChrome)
        {
            if (Event.current.type == UnityEngine.EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Event.current.Use();
                    HideConfig();
                    return;
                }
            }

            bool compact = !withWindowChrome;
            bool useParchmentTheme = withWindowChrome;

            if (withWindowChrome)
            {
                const float titleBarHeight = 36f;
                const float outerBorder = 5f;
                const float innerBorderW = 1f;

                if (_configInnerBorderTex != null)
                {
                    float innerX = outerBorder;
                    float innerY = outerBorder;
                    float innerW = rect.width - outerBorder * 2;
                    float innerH = rect.height - outerBorder * 2;
                    GUI.DrawTexture(new Rect(innerX, innerY, innerW, innerBorderW), _configInnerBorderTex);
                    GUI.DrawTexture(new Rect(innerX, innerY + innerH - innerBorderW, innerW, innerBorderW), _configInnerBorderTex);
                    GUI.DrawTexture(new Rect(innerX, innerY, innerBorderW, innerH), _configInnerBorderTex);
                    GUI.DrawTexture(new Rect(innerX + innerW - innerBorderW, innerY, innerBorderW, innerH), _configInnerBorderTex);
                }

                if (_configTitleBarTex != null)
                    GUI.DrawTexture(new Rect(0, 0, rect.width, titleBarHeight), _configTitleBarTex);
                GUILayout.BeginArea(new Rect(0, 0, rect.width, titleBarHeight));
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Senpai's Chest Config", _configTitleStyle, GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", _closeButtonStyle, GUILayout.Width(28), GUILayout.Height(24)))
                    HideConfig();
                GUILayout.Space(4);
                GUILayout.EndHorizontal();
                GUILayout.EndArea();

                GUILayout.Space(titleBarHeight + 2);
                if (_configGoldSeparatorTex != null)
                {
                    var sepRect = GUILayoutUtility.GetRect(rect.width - 24, 2);
                    GUI.DrawTexture(sepRect, _configGoldSeparatorTex);
                }
                GUILayout.Space(8);
            }

            if (withWindowChrome)
                GUILayout.Space(4);
            const float colSpacing = 8f;
            float colWidth = compact ? (rect.width - colSpacing) / 2f : 0f;
            if (compact)
            {
                GUILayout.BeginHorizontal(GUILayout.Width(rect.width));
                GUILayout.BeginVertical(GUILayout.Width(colWidth));
            }

            GUILayout.BeginHorizontal();
            bool newEnabled = GUILayout.Toggle(_currentData.IsEnabled, " Smart Chest Enabled", useParchmentTheme ? _chestToggleStyle : _toggleStyle);
            if (newEnabled != _currentData.IsEnabled)
            {
                _currentData.IsEnabled = newEnabled;
                _manager.MarkDirty();
                SaveIfDirty();
            }
            GUILayout.EndHorizontal();

            if (useParchmentTheme && _configGoldSeparatorTex != null)
            {
                var sepR = GUILayoutUtility.GetRect(rect.width - 24, 2);
                GUI.DrawTexture(sepR, _configGoldSeparatorTex);
                GUILayout.Space(6);
            }
            else
                GUILayout.Space(compact ? 4 : 8);

            var sectionHeaderStyle = useParchmentTheme ? _configSectionHeaderBoxStyle : (compact ? _chestSectionHeaderStyle : _sectionHeaderStyle);
            GUILayout.Label("Item Rules", sectionHeaderStyle);
            GUILayout.Space(compact ? 2 : 4);

            if (useParchmentTheme && _configGoldSeparatorTex != null)
            {
                var sepR2 = GUILayoutUtility.GetRect(rect.width - 24, 2);
                GUI.DrawTexture(sepR2, _configGoldSeparatorTex);
                GUILayout.Space(4);
            }

            if (_currentData.Rules.Count == 0)
            {
                GUILayout.Label("  No rules configured. Add rules below.", useParchmentTheme ? _chestLabelDimStyle : (compact ? _chestLabelDimStyle : _labelDimStyle));
            }
            else
            {
                float ruleItemHeight = compact ? 24f : 36f;
                float maxRulesHeight = compact ? 100f : 180f;
                float rulesHeight = Mathf.Min(_currentData.Rules.Count * ruleItemHeight, maxRulesHeight);
                GUILayout.BeginVertical(GUILayout.Height(rulesHeight));
                int removeIndex = -1;
                for (int i = 0; i < _currentData.Rules.Count; i++)
                {
                    var ruleBoxStyle = useParchmentTheme ? _chestRuleBoxStyle : (compact ? _chestRuleBoxStyle : _ruleBoxStyle);
                    var ruleTextStyle = useParchmentTheme ? _chestRuleTextStyle : (compact ? _chestRuleTextStyle : _ruleTextStyle);
                    var removeBtnStyle = useParchmentTheme ? _chestRemoveRuleBtnStyle : (compact ? _chestRemoveRuleBtnStyle : _removeRuleBtnStyle);
                    GUILayout.BeginHorizontal(ruleBoxStyle);
                    GUILayout.Label($"{i + 1}. {GetRuleDisplayText(_currentData.Rules[i])}", ruleTextStyle, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("X", removeBtnStyle, GUILayout.Width(26), GUILayout.Height(22)))
                        removeIndex = i;
                    GUILayout.EndHorizontal();
                    GUILayout.Space(compact ? 1 : 2);
                }
                GUILayout.EndVertical();
                if (removeIndex >= 0)
                {
                    _currentData.Rules.RemoveAt(removeIndex);
                    _manager.MarkDirty();
                    SaveIfDirty();
                }
            }

            if (useParchmentTheme && _configGoldSeparatorTex != null)
            {
                var sepR3 = GUILayoutUtility.GetRect(rect.width - 24, 2);
                GUI.DrawTexture(sepR3, _configGoldSeparatorTex);
                GUILayout.Space(8);
            }
            else
                GUILayout.Space(compact ? 4 : 10);

            GUILayout.Label("Add New Rule", sectionHeaderStyle);
            GUILayout.Space(compact ? 2 : 6);

            if (useParchmentTheme && _configGoldSeparatorTex != null)
            {
                var sepR4 = GUILayoutUtility.GetRect(rect.width - 24, 2);
                GUI.DrawTexture(sepR4, _configGoldSeparatorTex);
                GUILayout.Space(4);
            }

            GUILayout.BeginHorizontal();
            DrawSelectorButton(0, useParchmentTheme || compact, GUILayout.Height((useParchmentTheme || compact) ? 24 : 28));
            DrawSelectorButton(1, useParchmentTheme || compact, GUILayout.Height((useParchmentTheme || compact) ? 24 : 28));
            DrawSelectorButton(2, useParchmentTheme || compact, GUILayout.Height((useParchmentTheme || compact) ? 24 : 28));
            GUILayout.EndHorizontal();
            GUILayout.Space(compact ? 1 : 2);
            GUILayout.BeginHorizontal();
            DrawSelectorButton(3, useParchmentTheme || compact, GUILayout.Height((useParchmentTheme || compact) ? 24 : 28));
            DrawSelectorButton(4, useParchmentTheme || compact, GUILayout.Height((useParchmentTheme || compact) ? 24 : 28));
            if (GUILayout.Button("Manage Groups", useParchmentTheme ? _configCloseBottomStyle : _closeBottomButtonStyle, GUILayout.Height((useParchmentTheme || compact) ? 22 : 26)))
                _groupsWindowVisible = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(compact ? 4 : 8);

            if (useParchmentTheme && _configGoldSeparatorTex != null)
            {
                var sepR5 = GUILayoutUtility.GetRect(rect.width - 24, 2);
                GUI.DrawTexture(sepR5, _configGoldSeparatorTex);
                GUILayout.Space(4);
            }

            DrawRuleInput(useParchmentTheme || compact, useWhiteSearchField: useParchmentTheme);
            GUILayout.Space(compact ? 4 : 8);

            if (useParchmentTheme && _configGoldSeparatorTex != null)
            {
                var sepR6 = GUILayoutUtility.GetRect(rect.width - 24, 2);
                GUI.DrawTexture(sepR6, _configGoldSeparatorTex);
                GUILayout.Space(6);
            }

            var addBtnStyle = useParchmentTheme ? _chestAddButtonStyle : (compact ? _chestAddButtonStyle : _addButtonStyle);
            if (GUILayout.Button("Add Rule", addBtnStyle, GUILayout.Height((useParchmentTheme || compact) ? 24 : 30)))
                AddRule();

            if (compact)
            {
                GUILayout.EndVertical();
                GUILayout.Space(colSpacing);
                GUILayout.BeginVertical(GUILayout.Width(colWidth));
                DrawEmbeddedRightColumn(compact);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            else
            {
                if (useParchmentTheme && _configGoldSeparatorTex != null)
                {
                    var sepR7 = GUILayoutUtility.GetRect(rect.width - 24, 2);
                    GUI.DrawTexture(sepR7, _configGoldSeparatorTex);
                    GUILayout.Space(8);
                }
                else
                    GUILayout.Space(12);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Remove Smart Chest", useParchmentTheme ? _configRemoveButtonStyle : _dangerButtonStyle, GUILayout.Height(28)))
                {
                    _manager.RemoveSmartChest(_chestId);
                    SaveIfDirty();
                    HideConfig();
                }
                GUILayout.Space(8);
                if (GUILayout.Button("Close", useParchmentTheme ? _configCloseBottomStyle : _closeBottomButtonStyle, GUILayout.Height(28)))
                    HideConfig();
                GUILayout.EndHorizontal();
            }

            if (withWindowChrome)
            {
                if (Event.current.type == UnityEngine.EventType.Repaint)
                {
                    var lastRect = GUILayoutUtility.GetLastRect();
                    _contentHeight = lastRect.yMax + 24f;
                }
                GUI.DragWindow(new Rect(0, 0, rect.width, 28));
            }

            if (Event.current.type == UnityEngine.EventType.KeyDown && Event.current.keyCode == KeyCode.Backspace)
                Event.current.Use();
        }

        private void DrawEmbeddedRightColumn(bool compact)
        {
            GUILayout.Space(4);
            GUILayout.Label("Summary", _chestSectionHeaderStyle, GUILayout.ExpandWidth(true));
            int ruleCount = _currentData?.Rules?.Count ?? 0;
            string status = _currentData?.IsEnabled == true ? "Active" : "Disabled";
            GUILayout.BeginVertical(_chestRuleBoxStyle);
            GUILayout.Label($"Rules: {ruleCount} | Status: {status}", _chestLabelStyle, GUILayout.ExpandWidth(true));
            if (ruleCount > 0)
            {
                GUILayout.Label("Accepts:", _chestLabelBoldStyle, GUILayout.ExpandWidth(true));
                for (int i = 0; i < ruleCount; i++)
                    GUILayout.Label($"  • {GetRuleDisplayText(_currentData.Rules[i])}", _chestLabelDimStyle, GUILayout.ExpandWidth(true));
            }
            GUILayout.EndVertical();
            GUILayout.Space(4);
            GUILayout.Label("Tips", _chestSectionHeaderStyle, GUILayout.ExpandWidth(true));
            GUILayout.BeginVertical(_chestRuleBoxStyle);
            GUILayout.Label("• Rules filter auto-sort. Multiple = AND.", _chestRuleTextStyle, GUILayout.ExpandWidth(true));
            GUILayout.Label("• Transfer SAME/ALL applies rules.", _chestRuleTextStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndVertical();
            GUILayout.Space(8);
            if (GUILayout.Button("Remove Smart Chest", _chestDangerButtonStyle, GUILayout.Height(20), GUILayout.ExpandWidth(true)))
            {
                _manager.RemoveSmartChest(_chestId);
                SaveIfDirty();
                HideConfig();
            }
            GUILayout.Space(2);
            if (GUILayout.Button("Close", _chestCloseButtonStyle, GUILayout.Height(20), GUILayout.ExpandWidth(true)))
                HideConfig();
        }

        private void DrawGroupsWindow(int windowId)
        {
            if (!_stylesInitialized) InitializeStyles();
            var labelBold = _chestLabelBoldStyle;
            var label = _chestLabelStyle;
            var labelDim = _chestLabelDimStyle;

            GUILayout.Label("Create or edit groups. Use 'By Group' when adding rules to attach a group to a chest.", labelDim, GUILayout.ExpandWidth(true));
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            _newGroupName = GUILayout.TextField(_newGroupName ?? "", _configSearchFieldStyle ?? _chestSearchFieldStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("New Group", _chestAddButtonStyle, GUILayout.Width(90)))
            {
                var name = (_newGroupName ?? "").Trim();
                if (name.Length > 0 && _manager.GetGroup(name) == null)
                {
                    _manager.GetOrCreateGroup(name);
                    _newGroupName = "";
                    _editingGroup = _manager.GetGroup(name);
                    _manager.MarkDirty();
                    SaveIfDirty();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            var groups = _manager.GetGroups();
            if (groups.Count > 0)
            {
                GUILayout.Label("Groups:", labelBold);
                _groupListScroll = GUILayout.BeginScrollView(_groupListScroll, GUILayout.Height(100));
                for (int i = 0; i < groups.Count; i++)
                {
                    var g = groups[i];
                    GUILayout.BeginHorizontal();
                    bool isEditing = _editingGroup == g;
                    var btnStyle = isEditing ? _chestSelectorActiveStyle : _chestSelectorStyle;
                    if (GUILayout.Button($"{g.Name} ({g.ItemIds?.Count ?? 0})", btnStyle, GUILayout.ExpandWidth(true)))
                        _editingGroup = g;
                    if (GUILayout.Button("X", _chestRemoveRuleBtnStyle, GUILayout.Width(26), GUILayout.Height(22)))
                    {
                        _manager.RemoveGroup(g.Name);
                        if (_editingGroup == g) _editingGroup = null;
                        _manager.MarkDirty();
                        SaveIfDirty();
                        }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                GUILayout.Space(6);
            }

            if (_editingGroup != null && _manager.GetGroup(_editingGroup.Name) != null)
            {
                GUILayout.Label($"Editing: {_editingGroup.Name}", labelBold);
                if (_editingGroup.ItemIds != null && _editingGroup.ItemIds.Count > 0)
                {
                    _groupItemsScroll = GUILayout.BeginScrollView(_groupItemsScroll, GUILayout.Height(80));
                    for (int i = _editingGroup.ItemIds.Count - 1; i >= 0; i--)
                    {
                        int id = _editingGroup.ItemIds[i];
                        var itemName = ItemSearch.GetItemName(id) ?? $"#{id}";
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(itemName, label, GUILayout.ExpandWidth(true));
                        if (GUILayout.Button("X", _chestRemoveRuleBtnStyle, GUILayout.Width(24), GUILayout.Height(20)))
                        {
                            _editingGroup.ItemIds.RemoveAt(i);
                            _manager.MarkDirty();
                            SaveIfDirty();
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();
                }
                else
                {
                    GUILayout.Label("  No items. Search below to add.", labelDim);
                }
                GUILayout.Space(4);
                GUILayout.Label("Add item (search):", labelBold);
                if (_groupItemSearch != _groupItemSearchLast)
                {
                    _groupItemSearchLast = _groupItemSearch ?? "";
                    _groupSearchResults = string.IsNullOrEmpty(_groupItemSearchLast) || _groupItemSearchLast.Length < 2
                        ? new List<KeyValuePair<int, string>>()
                        : ItemSearch.SearchItems(_groupItemSearchLast, 15);
                }
                _groupItemSearch = GUILayout.TextField(_groupItemSearch ?? "", _configSearchFieldStyle ?? _chestSearchFieldStyle);
                if (_groupSearchResults.Count > 0)
                {
                    _groupSearchScroll = GUILayout.BeginScrollView(_groupSearchScroll, GUILayout.Height(100));
                    foreach (var kv in _groupSearchResults)
                    {
                        if (GUILayout.Button(ItemSearch.FormatDisplay(kv.Value, kv.Key), _chestSearchResultStyle, GUILayout.Height(22)))
                        {
                            if (_editingGroup.ItemIds == null) _editingGroup.ItemIds = new List<int>();
                            if (!_editingGroup.ItemIds.Contains(kv.Key))
                            {
                                _editingGroup.ItemIds.Add(kv.Key);
                                _manager.MarkDirty();
                                SaveIfDirty();
                            }
                            _groupItemSearch = "";
                            _groupItemSearchLast = "";
                            _groupSearchResults.Clear();
                            break;
                        }
                    }
                    GUILayout.EndScrollView();
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", _chestCloseButtonStyle, GUILayout.Height(28)))
            {
                _groupsWindowVisible = false;
                _editingGroup = null;
                _newGroupName = "";
            }
            GUI.DragWindow(new Rect(0, 0, 400, 24));
        }

        private void DrawNoteContent(float width, float height)
        {
            if (!_stylesInitialized) InitializeStyles();
            string keyStr = SmartChestConfig.StaticRequireCtrl ? $"Ctrl+{SmartChestConfig.StaticToggleKey}" : SmartChestConfig.StaticToggleKey.ToString();
            string text = $"Press [{keyStr}] to configure smart chest rules";
            if (_noteBannerBg != null)
                GUI.DrawTexture(new Rect(0, 0, width, height), _noteBannerBg);
            const float bw = 1f;
            if (_noteBannerBorderTex != null)
            {
                GUI.DrawTexture(new Rect(0, 0, width, bw), _noteBannerBorderTex);
                GUI.DrawTexture(new Rect(0, height - bw, width, bw), _noteBannerBorderTex);
                GUI.DrawTexture(new Rect(0, 0, bw, height), _noteBannerBorderTex);
                GUI.DrawTexture(new Rect(width - bw, 0, bw, height), _noteBannerBorderTex);
            }
            GUILayout.BeginArea(new Rect(0, 0, width, height));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(text, _noteBannerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        private void DrawSelectorButton(int index, bool compact, params GUILayoutOption[] options)
        {
            var style = compact
                ? ((index == _selectedRuleType) ? _chestSelectorActiveStyle : _chestSelectorStyle)
                : ((index == _selectedRuleType) ? _selectorActiveStyle : _selectorStyle);
            if (GUILayout.Button(RuleTypeNames[index], style, options))
                _selectedRuleType = index;
        }

        private void DrawRuleInput(bool useChestStyles, bool useWhiteSearchField = false)
        {
            var labelBold = useChestStyles ? _chestLabelBoldStyle : _labelBoldStyle;
            var label = useChestStyles ? _chestLabelStyle : _labelStyle;
            var labelDim = useChestStyles ? _chestLabelDimStyle : _labelDimStyle;
            var searchFieldStyle = useWhiteSearchField ? _configSearchFieldStyle : (useChestStyles ? _chestSearchFieldStyle : _textFieldStyle);

            switch (_selectedRuleType)
            {
                case 0: // ByItemId
                    if (_itemIdInput != _lastSearchQuery)
                    {
                        _lastSearchQuery = _itemIdInput;
                        _searchResults = ItemSearch.SearchItems(_itemIdInput, 20);
                        _searchScrollPos = Vector2.zero;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Search:", labelBold, GUILayout.Width(55));
                    GUI.SetNextControlName(SearchFieldControlName);
                    _itemIdInput = GUILayout.TextField(_itemIdInput, searchFieldStyle);
                    GUILayout.EndHorizontal();

                    if (_selectedItemId > 0)
                    {
                        GUILayout.Space(2);
                        GUILayout.Label($"Selected: {ItemSearch.FormatDisplay(_selectedItemName, _selectedItemId)}", label);
                    }

                    if (_searchResults.Count > 0)
                    {
                        GUILayout.Space(4);
                        _searchScrollPos = GUILayout.BeginScrollView(_searchScrollPos, GUILayout.Height(120));
                        foreach (var result in _searchResults)
                        {
                            bool isMuseum = IsUndonatedMuseumItem(result.Key);
                            string resultLabel = ItemSearch.FormatDisplay(result.Value, result.Key);
                            if (isMuseum)
                                resultLabel += " [Museum]";

                            GUILayout.BeginHorizontal();
                            if (isMuseum)
                            {
                                var barRect = GUILayoutUtility.GetRect(4, 22, GUILayout.Width(4), GUILayout.Height(22));
                                GUI.color = _museumHighlight;
                                GUI.DrawTexture(barRect, Texture2D.whiteTexture);
                                GUI.color = Color.white;
                            }
                            var resultStyle = useChestStyles
                                ? ((result.Key == _selectedItemId) ? _chestSearchResultSelectedStyle : _chestSearchResultStyle)
                                : ((result.Key == _selectedItemId) ? _searchResultSelectedStyle : _searchResultStyle);
                            if (GUILayout.Button(resultLabel, resultStyle, GUILayout.Height(22)))
                            {
                                _selectedItemId = result.Key;
                                _selectedItemName = result.Value;
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndScrollView();
                    }
                    else if (_itemIdInput.Length >= 2)
                    {
                        GUILayout.Space(2);
                        GUILayout.Label("  No items found.", labelDim);
                    }

                    break;

                case 1: // ByCategory
                    GUILayout.Label("Category:", labelBold);
                    DrawOptionGrid(CategoryNames, ref _selectedCategory, 3, useChestStyles);
                    break;

                case 2: // ByItemType
                    GUILayout.Label("Item Type:", labelBold);
                    DrawOptionGrid(ItemTypeNames, ref _selectedItemType, 3, useChestStyles);
                    break;

                case 3: // ByProperty
                    GUILayout.Label("Property:", labelBold);
                    DrawOptionGrid(PropertyDisplayNames, ref _selectedProperty, useChestStyles ? 3 : 2, useChestStyles);
                    break;

                case 4: // ByGroup
                    var groups = _manager.GetGroups();
                    GUILayout.Label("Group:", labelBold);
                    if (groups.Count == 0)
                    {
                        GUILayout.Label("  No groups yet. Click Manage Groups to create one.", labelDim);
                    }
                    else
                    {
                        for (int i = 0; i < groups.Count; i++)
                        {
                            var g = groups[i];
                            var grpStyle = useChestStyles
                                ? (i == _selectedGroup ? _chestSearchResultSelectedStyle : _chestSearchResultStyle)
                                : (i == _selectedGroup ? _searchResultSelectedStyle : _searchResultStyle);
                            string grpLabel = $"{g.Name} ({g.ItemIds?.Count ?? 0} items)";
                            if (GUILayout.Button(grpLabel, grpStyle, GUILayout.Height(24)))
                                _selectedGroup = i;
                        }
                    }
                    break;
            }
        }

        private void DrawOptionGrid(string[] options, ref int selected, int columns, bool useChestStyles = false)
        {
            var selStyle = useChestStyles ? _chestSelectorStyle : _selectorStyle;
            var selActiveStyle = useChestStyles ? _chestSelectorActiveStyle : _selectorActiveStyle;
            int rows = (options.Length + columns - 1) / columns;
            for (int row = 0; row < rows; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < columns; col++)
                {
                    int idx = row * columns + col;
                    if (idx < options.Length)
                    {
                        var style = (idx == selected) ? selActiveStyle : selStyle;
                        if (GUILayout.Button(options[idx], style, GUILayout.Height(24)))
                            selected = idx;
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
        }

        private string GetRuleDisplayText(SmartChestRule rule)
        {
            if (rule.Type == RuleType.ByItemId)
            {
                var name = ItemSearch.GetItemName(rule.ItemId);
                string baseText = !string.IsNullOrEmpty(name)
                    ? $"{name} ({rule.ItemId})"
                    : $"Item ID: {rule.ItemId}";
                if (IsUndonatedMuseumItem(rule.ItemId))
                    baseText += " [Museum]";
                return baseText;
            }
            return rule.GetDisplayText();
        }

        /// <summary>
        /// Returns true if the item is a museum item that hasn't been donated yet.
        /// Requires S.M.U.T. to be loaded. Returns false if S.M.U.T. is not present.
        /// </summary>
        private bool IsUndonatedMuseumItem(int gameItemId)
        {
            try
            {
                var donationManager = SunHavenMuseumUtilityTracker.Plugin.GetDonationManager();
                if (donationManager == null || !donationManager.IsLoaded)
                    return false;

                if (MuseumContent.FindByGameItemId(gameItemId) == null)
                    return false;

                return !donationManager.HasDonatedByGameId(gameItemId);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[SmartChestUI] IsUndonatedMuseumItem({gameItemId}): {ex.Message}");
                return false;
            }
        }

        private void AddRule()
        {
            SmartChestRule rule = null;

            switch (_selectedRuleType)
            {
                case 0: // ByItemId
                    if (_selectedItemId > 0)
                    {
                        rule = new SmartChestRule { Type = RuleType.ByItemId, ItemId = _selectedItemId };
                        _itemIdInput = "";
                        _lastSearchQuery = "";
                        _searchResults.Clear();
                        _selectedItemId = -1;
                        _selectedItemName = "";
                    }
                    else
                    {
                        Plugin.Log?.LogWarning("Select an item from the search results first");
                    }
                    break;

                case 1: // ByCategory
                    if (_selectedCategory >= 0 && _selectedCategory < CategoryNames.Length)
                    {
                        rule = new SmartChestRule { Type = RuleType.ByCategory, CategoryName = CategoryNames[_selectedCategory] };
                    }
                    break;

                case 2: // ByItemType
                    if (_selectedItemType >= 0 && _selectedItemType < ItemTypeNames.Length)
                    {
                        rule = new SmartChestRule { Type = RuleType.ByItemType, ItemTypeName = ItemTypeNames[_selectedItemType] };
                    }
                    break;

                case 3: // ByProperty
                    if (_selectedProperty >= 0 && _selectedProperty < PropertyNames.Length)
                    {
                        rule = new SmartChestRule { Type = RuleType.ByProperty, PropertyName = PropertyNames[_selectedProperty] };
                    }
                    break;

                case 4: // ByGroup
                    var groups = _manager.GetGroups();
                    if (_selectedGroup >= 0 && _selectedGroup < groups.Count)
                    {
                        var g = groups[_selectedGroup];
                        if (g != null && !string.IsNullOrEmpty(g.Name))
                            rule = new SmartChestRule { Type = RuleType.ByGroup, GroupName = g.Name };
                    }
                    else
                    {
                        Plugin.Log?.LogWarning("Select a group, or create one in Manage Groups first");
                    }
                    break;
            }

            if (rule != null)
            {
                foreach (var existing in _currentData.Rules)
                {
                    if (existing.Type == rule.Type &&
                        existing.ItemId == rule.ItemId &&
                        existing.CategoryName == rule.CategoryName &&
                        existing.ItemTypeName == rule.ItemTypeName &&
                        existing.PropertyName == rule.PropertyName &&
                        existing.GroupName == rule.GroupName)
                    {
                        Plugin.Log?.LogInfo("Rule already exists, skipping duplicate");
                        return;
                    }
                }

                _currentData.Rules.Add(rule);
                _manager.MarkDirty();
                SaveIfDirty();
                Plugin.Log?.LogInfo($"Added rule: {rule.GetDisplayText()}");
            }
        }

        private void SaveIfDirty()
        {
            var saveSystem = Plugin.GetSaveSystem();
            if (saveSystem == null)
            {
                Plugin.Log?.LogWarning("[UI] SaveIfDirty: SaveSystem is null!");
                return;
            }
            saveSystem.Save();
        }

        #region Style Initialization

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            CreateTextures();
            CreateStyles();

            _stylesInitialized = true;
        }

        private void CreateTextures()
        {
            _solidBg = MakeTex(4, 4, _bgDark);
            _windowBg = MakeBorderedTex(16, 16, _bgDark, _borderGold, 2);
            _ruleBg = MakeTex(1, 1, _ruleBoxColor);
            _btnInactiveTex = MakeTex(1, 1, _btnInactive);
            _btnHoverTex = MakeTex(1, 1, _btnHover);
            _btnActiveTex = MakeTex(1, 1, _greenActive);
            _btnActiveHoverTex = MakeTex(1, 1, _greenHover);
            _redBtnTex = MakeTex(1, 1, _redDanger);
            _redBtnHoverTex = MakeTex(1, 1, _redHover);
            _greenBtnTex = MakeTex(1, 1, _greenActive);
            _greenBtnHoverTex = MakeTex(1, 1, _greenBright);
            _closeBtnTex = MakeTex(1, 1, _redDanger);
            _closeBtnHoverTex = MakeTex(1, 1, _redHover);
            _fieldBgTex = MakeTex(1, 1, _fieldBg);
            _chestParchmentBg = MakeParchmentTexture(32, 64, _parchment, _parchmentLight, _borderWood, 3);
            _chestWoodBorderTex = MakeTex(1, 1, _borderWood);
            _chestRuleBoxTex = MakeRoundedRect(16, 16, _parchmentDark, _borderWood, 2);
            _chestSelectorTex = MakeRoundedRect(8, 8, _parchmentDark, _borderWood, 2);
            _chestSelectorHoverTex = MakeRoundedRect(8, 8, _woodLight, _borderWood, 2);
            _chestSelectorActiveTex = MakeRoundedRect(8, 8, _goldPale, _goldRich, 2);
            _chestAddBtnTex = MakeRoundedRect(8, 8, new Color(0.30f, 0.55f, 0.45f), new Color(0.20f, 0.40f, 0.35f), 2);
            _chestAddBtnHoverTex = MakeRoundedRect(8, 8, new Color(0.38f, 0.65f, 0.52f), new Color(0.25f, 0.48f, 0.42f), 2);
            _chestWoodBtnTex = MakeRoundedRect(8, 8, _woodMedium, _woodDark, 2);
            _chestWoodBtnHoverTex = MakeRoundedRect(8, 8, _woodLight, _woodMedium, 2);
            _chestDangerBtnTex = MakeRoundedRect(8, 8, _redDanger, new Color(0.55f, 0.15f, 0.15f), 2);
            _chestDangerBtnHoverTex = MakeRoundedRect(8, 8, _redHover, new Color(0.65f, 0.18f, 0.18f), 2);
            _chestFieldBgTex = MakeRoundedRect(6, 6, _parchmentLight, _borderWood, 1);
            _chestGoldLineTex = MakeGradientTex(64, 3, _goldPale, _goldRich);
            _noteBannerBg = MakeTex(4, 4, new Color(0.45f, 0.10f, 0.08f, 1f));
            _noteBannerBorderTex = MakeTex(1, 1, _goldRich);

            _configTitleBarTex = MakeGradientTex(64, 32, _crimsonTitleLight, _crimsonTitle);
            _configContentBg = MakeParchmentTexture(64, 128, _parchment, _parchmentLight, _borderWood, 0);
            _configGoldSeparatorTex = MakeGradientTex(64, 2, _goldPale, _goldRich);
            _configSectionHeaderTex = MakeRoundedRect(16, 16, _parchmentDark, _goldRich, 2);
            _configSearchFieldTex = MakeRoundedRect(8, 8, Color.white, new Color(0.25f, 0.2f, 0.18f), 1);
            _configRemoveBtnTex = MakeRoundedRect(8, 8, _crimsonTitle, new Color(0.4f, 0.12f, 0.08f), 1);
            _configRemoveBtnHoverTex = MakeRoundedRect(8, 8, _crimsonTitleLight, new Color(0.45f, 0.15f, 0.10f), 1);
            _configCloseBtnTex = MakeRoundedRect(8, 8, _woodMedium, _woodDark, 1);
            _configCloseBtnHoverTex = MakeRoundedRect(8, 8, _woodLight, _woodMedium, 1);
            _configWindowBg = MakeBorderedTex(64, 64, _parchmentLight, _goldRich, 5);
            _configInnerBorderTex = MakeTex(1, 1, _innerBorderCrimson);
        }

        private void CreateStyles()
        {
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(14, 14, 12, 12),
                border = new RectOffset(2, 2, 2, 2)
            };
            _windowStyle.normal.background = _windowBg;
            _windowStyle.normal.textColor = _whiteText;
            _windowStyle.onNormal.background = _windowBg;
            _windowStyle.onNormal.textColor = _whiteText;

            _titleStyle = new GUIStyle
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _whiteText },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(2, 2, 2, 2)
            };

            _sectionHeaderStyle = new GUIStyle
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _goldText },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 2, 2)
            };

            _labelStyle = new GUIStyle
            {
                fontSize = 13,
                normal = { textColor = _whiteText },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(2, 2, 2, 2),
                wordWrap = true
            };

            _labelBoldStyle = new GUIStyle(_labelStyle) { fontStyle = FontStyle.Bold };

            _labelDimStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                normal = { textColor = _dimText }
            };

            _ruleBoxStyle = new GUIStyle
            {
                normal = { background = _ruleBg },
                padding = new RectOffset(10, 8, 6, 6),
                margin = new RectOffset(4, 4, 2, 2)
            };

            _ruleTextStyle = new GUIStyle(_labelStyle) { fontSize = 13 };

            _removeRuleBtnStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { background = _redBtnTex, textColor = _whiteText },
                hover = { background = _redBtnHoverTex, textColor = _whiteText },
                active = { background = _redBtnHoverTex, textColor = _whiteText },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(2, 2, 2, 2)
            };

            _closeButtonStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { background = _closeBtnTex, textColor = _whiteText },
                hover = { background = _closeBtnHoverTex, textColor = _whiteText },
                active = { background = _closeBtnHoverTex, textColor = _whiteText },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(2, 2, 1, 1)
            };

            _toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 13 };
            _toggleStyle.normal.textColor = _whiteText;
            _toggleStyle.onNormal.textColor = _whiteText;
            _toggleStyle.hover.textColor = _whiteText;
            _toggleStyle.onHover.textColor = _whiteText;

            _textFieldStyle = new GUIStyle
            {
                fontSize = 13,
                normal = { background = _fieldBgTex, textColor = _whiteText },
                focused = { background = _fieldBgTex, textColor = _whiteText },
                padding = new RectOffset(8, 8, 5, 5),
                border = new RectOffset(2, 2, 2, 2)
            };

            _selectorStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { background = _btnInactiveTex, textColor = _whiteText },
                hover = { background = _btnHoverTex, textColor = _whiteText },
                active = { background = _btnActiveTex, textColor = _whiteText },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(2, 2, 1, 1)
            };

            _selectorActiveStyle = new GUIStyle(_selectorStyle)
            {
                normal = { background = _btnActiveTex, textColor = _whiteText },
                hover = { background = _btnActiveHoverTex, textColor = _whiteText }
            };

            _addButtonStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { background = _greenBtnTex, textColor = _whiteText },
                hover = { background = _greenBtnHoverTex, textColor = _whiteText },
                active = { background = _greenBtnHoverTex, textColor = _whiteText },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 5, 5)
            };

            _dangerButtonStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { background = _redBtnTex, textColor = _whiteText },
                hover = { background = _redBtnHoverTex, textColor = _whiteText },
                active = { background = _redBtnHoverTex, textColor = _whiteText },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 4, 4)
            };

            _closeBottomButtonStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { background = _btnInactiveTex, textColor = _whiteText },
                hover = { background = _btnHoverTex, textColor = _whiteText },
                active = { background = _btnHoverTex, textColor = _whiteText },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 4, 4)
            };

            _searchResultStyle = new GUIStyle
            {
                fontSize = 12,
                normal = { background = _ruleBg, textColor = _whiteText },
                hover = { background = _btnHoverTex, textColor = _whiteText },
                active = { background = _btnActiveTex, textColor = _whiteText },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 3, 3),
                margin = new RectOffset(0, 0, 1, 1)
            };

            _searchResultSelectedStyle = new GUIStyle(_searchResultStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { background = _btnActiveTex, textColor = _whiteText },
                hover = { background = _btnActiveHoverTex, textColor = _whiteText }
            };

            _chestToggleStyle = new GUIStyle(_toggleStyle) { fontSize = 14 };
            _chestToggleStyle.normal.textColor = _chestTextDark;
            _chestToggleStyle.onNormal.textColor = _chestTextDark;
            _chestToggleStyle.hover.textColor = _chestTextDark;
            _chestToggleStyle.onHover.textColor = _chestTextDark;

            _chestTitleStyle = new GUIStyle(_titleStyle)
            {
                normal = { textColor = _woodDark }, hover = { textColor = _woodDark }, active = { textColor = _woodDark },
                focused = { textColor = _woodDark }, onNormal = { textColor = _woodDark }, onHover = { textColor = _woodDark },
                onActive = { textColor = _woodDark }, onFocused = { textColor = _woodDark }, fontSize = 17
            };

            _chestSectionHeaderStyle = new GUIStyle(_sectionHeaderStyle)
            {
                normal = { textColor = _woodDark }, hover = { textColor = _woodDark }, active = { textColor = _woodDark },
                focused = { textColor = _woodDark }, onNormal = { textColor = _woodDark }, onHover = { textColor = _woodDark },
                onActive = { textColor = _woodDark }, onFocused = { textColor = _woodDark }, fontSize = 16
            };

            _chestLabelStyle = new GUIStyle(_labelStyle) { normal = { textColor = _chestTextDark }, fontSize = 14 };
            _chestLabelBoldStyle = new GUIStyle(_chestLabelStyle) { fontStyle = FontStyle.Bold };
            _chestLabelDimStyle = new GUIStyle(_chestLabelStyle)
            {
                fontSize = 13, fontStyle = FontStyle.Italic, normal = { textColor = _chestTextDim }
            };

            _chestRuleBoxStyle = new GUIStyle { normal = { background = _chestRuleBoxTex }, padding = new RectOffset(10, 8, 6, 6), margin = new RectOffset(4, 4, 2, 2) };
            _chestRuleTextStyle = new GUIStyle(_labelStyle) { fontSize = 13, normal = { textColor = _woodDark }, hover = { textColor = _woodDark }, active = { textColor = _woodDark } };
            _chestSelectorStyle = new GUIStyle
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                normal = { background = _chestSelectorTex, textColor = _woodDark },
                hover = { background = _chestSelectorHoverTex, textColor = _woodDark },
                active = { background = _chestSelectorActiveTex, textColor = _woodDark },
                alignment = TextAnchor.MiddleCenter, padding = new RectOffset(8, 8, 5, 5), margin = new RectOffset(2, 2, 1, 1)
            };
            _chestSelectorActiveStyle = new GUIStyle(_chestSelectorStyle)
            {
                normal = { background = _chestSelectorActiveTex, textColor = _woodDark },
                hover = { background = _chestSelectorActiveTex, textColor = _woodDark }
            };
            _chestAddButtonStyle = new GUIStyle
            {
                fontSize = 14, fontStyle = FontStyle.Bold,
                normal = { background = _chestAddBtnTex, textColor = _parchmentLight },
                hover = { background = _chestAddBtnHoverTex, textColor = _parchmentLight },
                active = { background = _chestAddBtnHoverTex, textColor = _parchmentLight },
                alignment = TextAnchor.MiddleCenter, padding = new RectOffset(10, 10, 5, 5)
            };
            _chestDangerButtonStyle = new GUIStyle
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                normal = { background = _chestDangerBtnTex, textColor = _whiteText },
                hover = { background = _chestDangerBtnHoverTex, textColor = _whiteText },
                active = { background = _chestDangerBtnHoverTex, textColor = _whiteText },
                alignment = TextAnchor.MiddleCenter, padding = new RectOffset(10, 10, 4, 4)
            };
            _chestCloseButtonStyle = new GUIStyle
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                normal = { background = _chestWoodBtnTex, textColor = _parchmentLight },
                hover = { background = _chestWoodBtnHoverTex, textColor = _parchmentLight },
                active = { background = _chestWoodBtnHoverTex, textColor = _parchmentLight },
                alignment = TextAnchor.MiddleCenter, padding = new RectOffset(10, 10, 4, 4)
            };
            _chestSearchFieldStyle = new GUIStyle
            {
                fontSize = 13,
                normal = { background = _chestFieldBgTex, textColor = _woodDark },
                focused = { background = _chestFieldBgTex, textColor = _woodDark },
                hover = { background = _chestFieldBgTex, textColor = _woodDark },
                padding = new RectOffset(8, 8, 5, 5), border = new RectOffset(2, 2, 2, 2)
            };
            _chestRemoveRuleBtnStyle = new GUIStyle
            {
                fontSize = 13, fontStyle = FontStyle.Bold,
                normal = { background = _chestDangerBtnTex, textColor = _whiteText },
                hover = { background = _chestDangerBtnHoverTex, textColor = _whiteText },
                active = { background = _chestDangerBtnHoverTex, textColor = _whiteText },
                alignment = TextAnchor.MiddleCenter, padding = new RectOffset(2, 2, 2, 2)
            };
            _chestSearchResultStyle = new GUIStyle
            {
                fontSize = 12,
                normal = { background = _chestRuleBoxTex, textColor = _woodDark },
                hover = { background = _chestSelectorHoverTex, textColor = _woodDark },
                active = { background = _chestSelectorActiveTex, textColor = _woodDark },
                alignment = TextAnchor.MiddleLeft, padding = new RectOffset(8, 8, 3, 3), margin = new RectOffset(0, 0, 1, 1)
            };
            _chestSearchResultSelectedStyle = new GUIStyle(_chestSearchResultStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { background = _chestSelectorActiveTex, textColor = _woodDark },
                hover = { background = _chestSelectorActiveTex, textColor = _woodDark }
            };

            _noteBannerStyle = new GUIStyle
            {
                fontSize = 14, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.92f, 0.85f) },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 4, 8, 4)
            };

            _configTitleStyle = new GUIStyle
            {
                fontSize = 16, fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 4, 4)
            };

            _configSectionHeaderBoxStyle = new GUIStyle
            {
                fontSize = 14, fontStyle = FontStyle.Bold,
                normal = { background = _configSectionHeaderTex, textColor = _chestTextDark },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(12, 8, 6, 6),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _configSearchFieldStyle = new GUIStyle
            {
                fontSize = 13,
                normal = { background = _configSearchFieldTex, textColor = _chestTextDark },
                focused = { background = _configSearchFieldTex, textColor = _chestTextDark },
                padding = new RectOffset(8, 8, 5, 5),
                border = new RectOffset(2, 2, 2, 2)
            };

            _configRemoveButtonStyle = new GUIStyle
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                normal = { background = _configRemoveBtnTex, textColor = Color.white },
                hover = { background = _configRemoveBtnHoverTex, textColor = Color.white },
                active = { background = _configRemoveBtnHoverTex, textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 6, 6)
            };

            _configCloseBottomStyle = new GUIStyle
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                normal = { background = _configCloseBtnTex, textColor = Color.white },
                hover = { background = _configCloseBtnHoverTex, textColor = Color.white },
                active = { background = _configCloseBtnHoverTex, textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 6, 6)
            };

            _configWindowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(5, 5, 5, 5),
                border = new RectOffset(5, 5, 5, 5)
            };
            _configWindowStyle.normal.background = _configWindowBg;
            _configWindowStyle.onNormal.background = _configWindowBg;
        }

        #endregion

        #region Texture Generation

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

        private Texture2D MakeBorderedTex(int width, int height, Color fillColor, Color borderColor, int borderWidth)
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

        private Texture2D MakeRoundedRect(int width, int height, Color fillColor, Color borderColor, int borderWidth)
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

        private Texture2D MakeParchmentTexture(int width, int height, Color baseColor, Color lightColor, Color borderColor, int borderWidth)
        {
            var tex = new Texture2D(width, height);
            var pixels = new Color[width * height];
            var rand = new System.Random(42);
            for (int y = 0; y < height; y++)
            {
                float gradientT = (float)y / (height - 1);
                Color rowBase = Color.Lerp(lightColor, baseColor, gradientT * 0.3f);
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = x < borderWidth || x >= width - borderWidth ||
                                   y < borderWidth || y >= height - borderWidth;
                    if (isBorder)
                        pixels[y * width + x] = borderColor;
                    else
                    {
                        float noise = (float)rand.NextDouble() * 0.03f - 0.015f;
                        pixels[y * width + x] = new Color(
                            Mathf.Clamp01(rowBase.r + noise),
                            Mathf.Clamp01(rowBase.g + noise),
                            Mathf.Clamp01(rowBase.b + noise),
                            rowBase.a);
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        #endregion

        private void OnDestroy()
        {
            if (_solidBg != null) Destroy(_solidBg);
            if (_windowBg != null) Destroy(_windowBg);
            if (_ruleBg != null) Destroy(_ruleBg);
            if (_btnInactiveTex != null) Destroy(_btnInactiveTex);
            if (_btnHoverTex != null) Destroy(_btnHoverTex);
            if (_btnActiveTex != null) Destroy(_btnActiveTex);
            if (_btnActiveHoverTex != null) Destroy(_btnActiveHoverTex);
            if (_redBtnTex != null) Destroy(_redBtnTex);
            if (_redBtnHoverTex != null) Destroy(_redBtnHoverTex);
            if (_greenBtnTex != null) Destroy(_greenBtnTex);
            if (_greenBtnHoverTex != null) Destroy(_greenBtnHoverTex);
            if (_closeBtnTex != null) Destroy(_closeBtnTex);
            if (_closeBtnHoverTex != null) Destroy(_closeBtnHoverTex);
            if (_fieldBgTex != null) Destroy(_fieldBgTex);
            if (_chestParchmentBg != null) Destroy(_chestParchmentBg);
            if (_chestWoodBorderTex != null) Destroy(_chestWoodBorderTex);
            if (_chestRuleBoxTex != null) Destroy(_chestRuleBoxTex);
            if (_chestSelectorTex != null) Destroy(_chestSelectorTex);
            if (_chestSelectorHoverTex != null) Destroy(_chestSelectorHoverTex);
            if (_chestSelectorActiveTex != null) Destroy(_chestSelectorActiveTex);
            if (_chestAddBtnTex != null) Destroy(_chestAddBtnTex);
            if (_chestAddBtnHoverTex != null) Destroy(_chestAddBtnHoverTex);
            if (_chestWoodBtnTex != null) Destroy(_chestWoodBtnTex);
            if (_chestWoodBtnHoverTex != null) Destroy(_chestWoodBtnHoverTex);
            if (_chestDangerBtnTex != null) Destroy(_chestDangerBtnTex);
            if (_chestDangerBtnHoverTex != null) Destroy(_chestDangerBtnHoverTex);
            if (_chestFieldBgTex != null) Destroy(_chestFieldBgTex);
            if (_chestGoldLineTex != null) Destroy(_chestGoldLineTex);
            if (_noteBannerBg != null) Destroy(_noteBannerBg);
            if (_noteBannerBorderTex != null) Destroy(_noteBannerBorderTex);
            if (_configTitleBarTex != null) Destroy(_configTitleBarTex);
            if (_configContentBg != null) Destroy(_configContentBg);
            if (_configGoldSeparatorTex != null) Destroy(_configGoldSeparatorTex);
            if (_configSectionHeaderTex != null) Destroy(_configSectionHeaderTex);
            if (_configSearchFieldTex != null) Destroy(_configSearchFieldTex);
            if (_configRemoveBtnTex != null) Destroy(_configRemoveBtnTex);
            if (_configRemoveBtnHoverTex != null) Destroy(_configRemoveBtnHoverTex);
            if (_configCloseBtnTex != null) Destroy(_configCloseBtnTex);
            if (_configCloseBtnHoverTex != null) Destroy(_configCloseBtnHoverTex);
            if (_configWindowBg != null) Destroy(_configWindowBg);
            if (_configInnerBorderTex != null) Destroy(_configInnerBorderTex);
        }
    }
}
