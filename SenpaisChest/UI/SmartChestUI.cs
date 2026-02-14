using System;
using System.Collections.Generic;
using SenpaisChest.Data;
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

        // Add rule form state
        private int _selectedRuleType;
        private string _itemIdInput = "";
        private int _selectedCategory;
        private int _selectedItemType;
        private int _selectedProperty;

        // Dropdown options
        private static readonly string[] RuleTypeNames = { "By Item ID", "By Category", "By Item Type", "By Property" };
        private static readonly string[] CategoryNames = { "Equip", "Use", "Craftable", "Monster", "Furniture", "Quest" };
        private static readonly string[] ItemTypeNames = { "Normal", "Armor", "Food", "Fish", "Crop", "WateringCan", "Animal", "Pet", "Tool" };
        private static readonly string[] PropertyNames = { "isGem", "isForageable", "isAnimalProduct", "isMeal", "isFruit", "isArtisanryItem", "isPotion" };
        private static readonly string[] PropertyDisplayNames = { "Gems", "Forageables", "Animal Products", "Meals", "Fruits", "Artisanry Items", "Potions" };

        // Styles (lazy initialized)
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _ruleBoxStyle;
        private GUIStyle _toggleStyle;
        private bool _stylesInitialized;

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
            _currentChest = null;
            _currentData = null;
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

            // Get the chest name from ChestData
            string chestName = GetChestName(chest);
            _currentData = _manager.GetOrCreateSmartChest(_chestId, chestName);

            // Reset add-rule form
            _selectedRuleType = 0;
            _itemIdInput = "";
            _selectedCategory = 0;
            _selectedItemType = 0;
            _selectedProperty = 0;

            _isVisible = true;
        }

        private string GetChestName(Chest chest)
        {
            try
            {
                var dataField = typeof(Chest).GetField("data",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dataField != null)
                {
                    var chestData = dataField.GetValue(chest) as ChestData;
                    if (chestData != null && !string.IsNullOrEmpty(chestData.name))
                        return chestData.name;
                }
            }
            catch { }
            return "Chest";
        }

        private void InitStyles()
        {
            if (_stylesInitialized)
                return;

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            _windowStyle.normal.textColor = Color.white;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _headerStyle.normal.textColor = new Color(0.9f, 0.8f, 0.3f);

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13
            };
            _labelStyle.normal.textColor = Color.white;

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            _ruleBoxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                padding = new RectOffset(8, 8, 4, 4)
            };
            _ruleBoxStyle.normal.textColor = Color.white;

            _toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = 13
            };
            _toggleStyle.normal.textColor = Color.white;

            _stylesInitialized = false; // Re-init each frame since GUI.skin changes
        }

        private void OnGUI()
        {
            if (!_isVisible || _currentData == null)
                return;

            // Reset styles each frame (GUI.skin may change between frames)
            _stylesInitialized = false;
            InitStyles();

            _windowRect = GUILayout.Window(
                PluginInfo.PLUGIN_GUID.GetHashCode(),
                _windowRect,
                DrawWindow,
                $"Smart Chest Config — {_currentData.ChestName}",
                _windowStyle);
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Space(5);

            // Enable toggle
            GUILayout.BeginHorizontal();
            bool newEnabled = GUILayout.Toggle(_currentData.IsEnabled, " Smart Chest Enabled", _toggleStyle);
            if (newEnabled != _currentData.IsEnabled)
            {
                _currentData.IsEnabled = newEnabled;
                _manager.MarkDirty();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            DrawSeparator();

            // Rules list
            GUILayout.Label("Item Rules:", _headerStyle);
            GUILayout.Space(3);

            if (_currentData.Rules.Count == 0)
            {
                GUILayout.Label("  No rules configured. Add rules below.", _labelStyle);
            }
            else
            {
                _rulesScrollPos = GUILayout.BeginScrollView(_rulesScrollPos, GUILayout.Height(180));
                int removeIndex = -1;

                for (int i = 0; i < _currentData.Rules.Count; i++)
                {
                    GUILayout.BeginHorizontal(_ruleBoxStyle);
                    GUILayout.Label($"{i + 1}. {_currentData.Rules[i].GetDisplayText()}", _labelStyle, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("X", _buttonStyle, GUILayout.Width(25), GUILayout.Height(20)))
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
                }
            }

            GUILayout.Space(5);
            DrawSeparator();

            // Add rule section
            GUILayout.Label("Add New Rule:", _headerStyle);
            GUILayout.Space(3);

            // Rule type selector
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type:", _labelStyle, GUILayout.Width(50));
            _selectedRuleType = GUILayout.SelectionGrid(_selectedRuleType, RuleTypeNames, 2, _buttonStyle);
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            // Type-specific input
            DrawRuleInput();

            GUILayout.Space(5);

            // Add button
            if (GUILayout.Button("Add Rule", _buttonStyle, GUILayout.Height(30)))
            {
                AddRule();
            }

            GUILayout.Space(5);
            DrawSeparator();

            // Bottom buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove Smart Chest", _buttonStyle, GUILayout.Height(28)))
            {
                _manager.RemoveSmartChest(_chestId);
                Hide();
            }
            if (GUILayout.Button("Close", _buttonStyle, GUILayout.Height(28)))
            {
                Hide();
            }
            GUILayout.EndHorizontal();

            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 25));
        }

        private void DrawRuleInput()
        {
            switch (_selectedRuleType)
            {
                case 0: // ByItemId
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Item ID:", _labelStyle, GUILayout.Width(60));
                    _itemIdInput = GUILayout.TextField(_itemIdInput, GUILayout.Width(100));
                    GUILayout.Label("(numeric game item ID)", _labelStyle);
                    GUILayout.EndHorizontal();
                    break;

                case 1: // ByCategory
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Category:", _labelStyle, GUILayout.Width(70));
                    _selectedCategory = GUILayout.SelectionGrid(_selectedCategory, CategoryNames, 3, _buttonStyle);
                    GUILayout.EndHorizontal();
                    break;

                case 2: // ByItemType
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Item Type:", _labelStyle, GUILayout.Width(70));
                    GUILayout.EndHorizontal();
                    _selectedItemType = GUILayout.SelectionGrid(_selectedItemType, ItemTypeNames, 3, _buttonStyle);
                    break;

                case 3: // ByProperty
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Property:", _labelStyle, GUILayout.Width(70));
                    GUILayout.EndHorizontal();
                    _selectedProperty = GUILayout.SelectionGrid(_selectedProperty, PropertyDisplayNames, 2, _buttonStyle);
                    break;
            }
        }

        private void AddRule()
        {
            SmartChestRule rule = null;

            switch (_selectedRuleType)
            {
                case 0: // ByItemId
                    if (int.TryParse(_itemIdInput, out int itemId) && itemId > 0)
                    {
                        rule = new SmartChestRule { Type = RuleType.ByItemId, ItemId = itemId };
                        _itemIdInput = "";
                    }
                    else
                    {
                        Plugin.Log?.LogWarning("Invalid item ID");
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
                // Check for duplicate rules
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
                Plugin.Log?.LogInfo($"Added rule: {rule.GetDisplayText()}");
            }
        }

        private void DrawSeparator()
        {
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }
    }
}
