using System;
using System.Collections.Generic;
using SenpaisChest.Data;
using SunhavenMods.Shared;
using SunHavenMuseumUtilityTracker;
using SunHavenMuseumUtilityTracker.Data;
using UnityEngine;
using Wish;

namespace SenpaisChest.UI
{
    public class SmartChestUI : MonoBehaviour
    {
        private SmartChestManager _manager;
        private bool _isVisible;
        private Chest _currentChest;
        private SmartChestData _currentData;
        private string _chestId;

        // Window
        private Rect _windowRect = new Rect(100, 100, 420, 500);
        private Vector2 _rulesScrollPos;
        private float _contentHeight = 500f;

        // Add rule form state
        private int _selectedRuleType;
        private string _itemIdInput = "";
        private int _selectedCategory;
        private int _selectedItemType;
        private int _selectedProperty;

        // Item search state
        private string _lastSearchQuery = "";
        private List<KeyValuePair<int, string>> _searchResults = new List<KeyValuePair<int, string>>();
        private Vector2 _searchScrollPos;
        private int _selectedItemId = -1;
        private string _selectedItemName = "";

        // Dropdown options
        private static readonly string[] RuleTypeNames = { "By Item", "By Category", "By Item Type", "By Property" };
        private static readonly string[] CategoryNames = { "Equip", "Use", "Craftable", "Monster", "Furniture", "Quest" };
        private static readonly string[] ItemTypeNames = { "Normal", "Armor", "Food", "Fish", "Crop", "WateringCan", "Animal", "Pet", "Tool" };
        private static readonly string[] PropertyNames = { "isGem", "isForageable", "isAnimalProduct", "isMeal", "isFruit", "isArtisanryItem", "isPotion", "isNotDonated" };
        private static readonly string[] PropertyDisplayNames = { "Gems", "Forageables", "Animal Products", "Meals", "Fruits", "Artisanry Items", "Potions", "Museum (Not Donated)" };

        // Color palette — dark navy theme with gold accents (all fully opaque)
        private readonly Color _bgDark = new Color(0.15f, 0.16f, 0.24f, 1f);
        private readonly Color _borderGold = new Color(0.75f, 0.65f, 0.30f, 1f);
        private readonly Color _goldText = new Color(0.95f, 0.85f, 0.35f);
        private readonly Color _whiteText = new Color(0.95f, 0.95f, 0.95f);
        private readonly Color _dimText = new Color(0.6f, 0.6f, 0.7f);
        private readonly Color _greenActive = new Color(0.20f, 0.55f, 0.45f, 1f);
        private readonly Color _greenHover = new Color(0.25f, 0.65f, 0.52f, 1f);
        private readonly Color _greenBright = new Color(0.30f, 0.70f, 0.55f, 1f);
        private readonly Color _redDanger = new Color(0.75f, 0.20f, 0.20f, 1f);
        private readonly Color _redHover = new Color(0.85f, 0.28f, 0.28f, 1f);
        private readonly Color _btnInactive = new Color(0.22f, 0.24f, 0.34f, 1f);
        private readonly Color _btnHover = new Color(0.30f, 0.32f, 0.44f, 1f);
        private readonly Color _ruleBoxColor = new Color(0.18f, 0.19f, 0.28f, 1f);
        private readonly Color _fieldBg = new Color(0.12f, 0.13f, 0.22f, 1f);
        private readonly Color _museumHighlight = new Color(0.35f, 0.65f, 0.85f, 1f); // Museum item indicator
        private Texture2D _solidBg;

        // Textures
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

        // Styles
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
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
        private bool _stylesInitialized;

        public bool IsVisible => _isVisible;

        public void Initialize(SmartChestManager manager)
        {
            _manager = manager;
        }

        public void Show()
        {
            _isVisible = true;
        }

        public void Hide()
        {
            _isVisible = false;
            var chestToClose = _currentChest;
            _currentChest = null;
            _currentData = null;

            // Clear our tracking first (before EndInteract fires our prefix)
            Plugin.CurrentInteractingChest = null;

            // Save any pending changes
            SaveIfDirty();

            // Force the chest to properly end its interaction so it's no longer "in use"
            // (our EndInteract prefix may have blocked the game's close attempt earlier)
            if (chestToClose != null)
            {
                try { chestToClose.EndInteract(0); }
                catch (Exception ex) { Plugin.Log?.LogDebug($"[SmartChestUI] EndInteract on close: {ex.Message}"); }
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
                Hide();
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
            if (!_isVisible || _currentData == null)
                return;

            if (!_stylesInitialized)
                InitializeStyles();

            // Dynamic height — clamp to screen bounds
            float maxHeight = Screen.height - 40f;
            _windowRect.height = Mathf.Clamp(_contentHeight, 300f, maxHeight);

            // Keep window on screen
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);

            _windowRect = GUI.Window(
                PluginInfo.PLUGIN_GUID.GetHashCode(),
                _windowRect,
                DrawWindow,
                "",
                _windowStyle);
        }

        private void DrawWindow(int windowId)
        {
            // Draw solid background to guarantee opacity
            GUI.DrawTexture(new Rect(0, 0, _windowRect.width, _windowRect.height), _solidBg);

            // Title bar
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Smart Chest Config - {_currentData.ChestName}", _titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", _closeButtonStyle, GUILayout.Width(26), GUILayout.Height(22)))
                Hide();
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Enable toggle
            GUILayout.BeginHorizontal();
            bool newEnabled = GUILayout.Toggle(_currentData.IsEnabled, " Smart Chest Enabled", _toggleStyle);
            if (newEnabled != _currentData.IsEnabled)
            {
                _currentData.IsEnabled = newEnabled;
                _manager.MarkDirty();
                SaveIfDirty();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Rules section header
            GUILayout.Label("Item Rules:", _sectionHeaderStyle);
            GUILayout.Space(4);

            if (_currentData.Rules.Count == 0)
            {
                GUILayout.Label("  No rules configured. Add rules below.", _labelDimStyle);
            }
            else
            {
                // Adaptive scroll height: show all rules up to a max, then scroll
                float ruleItemHeight = 36f;
                float rulesHeight = Mathf.Min(_currentData.Rules.Count * ruleItemHeight, 180f);

                _rulesScrollPos = GUILayout.BeginScrollView(_rulesScrollPos, GUILayout.Height(rulesHeight));
                int removeIndex = -1;

                for (int i = 0; i < _currentData.Rules.Count; i++)
                {
                    GUILayout.BeginHorizontal(_ruleBoxStyle);
                    GUILayout.Label($"{i + 1}. {GetRuleDisplayText(_currentData.Rules[i])}", _ruleTextStyle, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("X", _removeRuleBtnStyle, GUILayout.Width(26), GUILayout.Height(22)))
                    {
                        removeIndex = i;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }

                GUILayout.EndScrollView();

                if (removeIndex >= 0)
                {
                    _currentData.Rules.RemoveAt(removeIndex);
                    _manager.MarkDirty();
                    SaveIfDirty();
                }
            }

            GUILayout.Space(10);

            // Add rule section
            GUILayout.Label("Add New Rule:", _sectionHeaderStyle);
            GUILayout.Space(6);

            // Rule type selector — 2x2 grid
            GUILayout.BeginHorizontal();
            DrawSelectorButton(0, GUILayout.Height(28));
            DrawSelectorButton(1, GUILayout.Height(28));
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            DrawSelectorButton(2, GUILayout.Height(28));
            DrawSelectorButton(3, GUILayout.Height(28));
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Type-specific input
            DrawRuleInput();

            GUILayout.Space(8);

            // Add button
            if (GUILayout.Button("Add Rule", _addButtonStyle, GUILayout.Height(30)))
            {
                AddRule();
            }

            GUILayout.Space(12);

            // Bottom buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove Smart Chest", _dangerButtonStyle, GUILayout.Height(28)))
            {
                _manager.RemoveSmartChest(_chestId);
                SaveIfDirty();
                Hide();
            }
            GUILayout.Space(8);
            if (GUILayout.Button("Close", _closeBottomButtonStyle, GUILayout.Height(28)))
            {
                Hide();
            }
            GUILayout.EndHorizontal();

            // Track actual content height for dynamic window sizing
            if (Event.current.type == UnityEngine.EventType.Repaint)
            {
                var lastRect = GUILayoutUtility.GetLastRect();
                _contentHeight = lastRect.yMax + 24f; // 24px for window padding
            }

            // Drag header area
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 28));
        }

        private void DrawSelectorButton(int index, params GUILayoutOption[] options)
        {
            var style = (index == _selectedRuleType) ? _selectorActiveStyle : _selectorStyle;
            if (GUILayout.Button(RuleTypeNames[index], style, options))
                _selectedRuleType = index;
        }

        private void DrawRuleInput()
        {
            switch (_selectedRuleType)
            {
                case 0: // ByItemId
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Search:", _labelBoldStyle, GUILayout.Width(55));
                    _itemIdInput = GUILayout.TextField(_itemIdInput, _textFieldStyle);
                    GUILayout.EndHorizontal();

                    // Run search when query changes
                    if (_itemIdInput != _lastSearchQuery)
                    {
                        _lastSearchQuery = _itemIdInput;
                        _searchResults = ItemSearch.SearchItems(_itemIdInput, 20);
                        _searchScrollPos = Vector2.zero;
                    }

                    // Show selected item
                    if (_selectedItemId > 0)
                    {
                        GUILayout.Space(2);
                        GUILayout.Label($"Selected: {ItemSearch.FormatDisplay(_selectedItemName, _selectedItemId)}", _labelStyle);
                    }

                    // Show search results
                    if (_searchResults.Count > 0)
                    {
                        GUILayout.Space(4);
                        _searchScrollPos = GUILayout.BeginScrollView(_searchScrollPos, GUILayout.Height(120));
                        foreach (var result in _searchResults)
                        {
                            bool isMuseum = IsUndonatedMuseumItem(result.Key);
                            string label = ItemSearch.FormatDisplay(result.Value, result.Key);
                            if (isMuseum)
                                label += " [Museum]";

                            GUILayout.BeginHorizontal();
                            if (isMuseum)
                            {
                                // Left border indicator for undonated museum items
                                var barRect = GUILayoutUtility.GetRect(4, 22, GUILayout.Width(4), GUILayout.Height(22));
                                GUI.color = _museumHighlight;
                                GUI.DrawTexture(barRect, Texture2D.whiteTexture);
                                GUI.color = Color.white;
                            }
                            var style = (result.Key == _selectedItemId) ? _searchResultSelectedStyle : _searchResultStyle;
                            if (GUILayout.Button(label, style, GUILayout.Height(22)))
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
                        GUILayout.Label("  No items found.", _labelDimStyle);
                    }
                    break;

                case 1: // ByCategory
                    GUILayout.Label("Category:", _labelBoldStyle);
                    DrawOptionGrid(CategoryNames, ref _selectedCategory, 3);
                    break;

                case 2: // ByItemType
                    GUILayout.Label("Item Type:", _labelBoldStyle);
                    DrawOptionGrid(ItemTypeNames, ref _selectedItemType, 3);
                    break;

                case 3: // ByProperty
                    GUILayout.Label("Property:", _labelBoldStyle);
                    DrawOptionGrid(PropertyDisplayNames, ref _selectedProperty, 2);
                    break;
            }
        }

        private void DrawOptionGrid(string[] options, ref int selected, int columns)
        {
            int rows = (options.Length + columns - 1) / columns;
            for (int row = 0; row < rows; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < columns; col++)
                {
                    int idx = row * columns + col;
                    if (idx < options.Length)
                    {
                        var style = (idx == selected) ? _selectorActiveStyle : _selectorStyle;
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
            }

            if (rule != null)
            {
                foreach (var existing in _currentData.Rules)
                {
                    if (existing.Type == rule.Type &&
                        existing.ItemId == rule.ItemId &&
                        existing.CategoryName == rule.CategoryName &&
                        existing.ItemTypeName == rule.ItemTypeName &&
                        existing.PropertyName == rule.PropertyName)
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

            _labelBoldStyle = new GUIStyle(_labelStyle)
            {
                fontStyle = FontStyle.Bold
            };

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

            _ruleTextStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 13
            };

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

            _toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = 13
            };
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
        }
    }
}
