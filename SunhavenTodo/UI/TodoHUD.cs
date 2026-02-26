using System;
using System.Collections.Generic;
using System.Linq;
using SunhavenTodo.Data;
using UnityEngine;

namespace SunhavenTodo.UI
{
    /// <summary>
    /// Movable HUD panel that displays the top 5 most urgent todo items.
    /// View-only interface - players can drag it anywhere on screen.
    /// </summary>
    public class TodoHUD : MonoBehaviour
    {
        // Window settings
        private const int WINDOW_ID = 98766;
        private const float WINDOW_WIDTH = 280f;
        private const float MIN_HEIGHT = 100f;
        private const float MAX_HEIGHT = 300f;
        private const float HEADER_HEIGHT = 28f;
        private const float ITEM_HEIGHT = 24f;
        private const int MAX_ITEMS = 5;

        // Manager reference
        private TodoManager _manager;

        // Display state
        private bool _isEnabled = true;
        private Rect _windowRect;

        // Position persistence callback
        public Action<float, float> OnPositionChanged;

        // Cached data
        private List<TodoItem> _cachedItems = new List<TodoItem>();
        private float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 1f;

        // Sun Haven themed colors (matching TodoUI)
        private readonly Color _parchmentLight = new Color(0.96f, 0.93f, 0.86f, 0.95f);
        private readonly Color _parchment = new Color(0.92f, 0.87f, 0.78f, 0.95f);
        private readonly Color _parchmentDark = new Color(0.85f, 0.78f, 0.65f, 0.95f);
        private readonly Color _woodDark = new Color(0.35f, 0.25f, 0.15f);
        private readonly Color _woodMedium = new Color(0.50f, 0.38f, 0.25f);
        private readonly Color _goldRich = new Color(0.85f, 0.68f, 0.20f);
        private readonly Color _goldBright = new Color(0.95f, 0.80f, 0.30f);
        private readonly Color _textDark = new Color(0.25f, 0.20f, 0.15f);
        private readonly Color _borderDark = new Color(0.40f, 0.30f, 0.20f, 0.8f);

        // Priority colors
        private readonly Dictionary<TodoPriority, Color> _priorityColors = new Dictionary<TodoPriority, Color>
        {
            { TodoPriority.Low, new Color(0.50f, 0.60f, 0.70f) },
            { TodoPriority.Normal, new Color(0.45f, 0.55f, 0.45f) },
            { TodoPriority.High, new Color(0.85f, 0.65f, 0.25f) },
            { TodoPriority.Urgent, new Color(0.80f, 0.30f, 0.25f) }
        };

        // Category colors
        private readonly Dictionary<TodoCategory, Color> _categoryColors = new Dictionary<TodoCategory, Color>
        {
            { TodoCategory.General, new Color(0.55f, 0.50f, 0.45f) },
            { TodoCategory.Farming, new Color(0.45f, 0.65f, 0.35f) },
            { TodoCategory.Mining, new Color(0.50f, 0.45f, 0.55f) },
            { TodoCategory.Fishing, new Color(0.40f, 0.60f, 0.75f) },
            { TodoCategory.Combat, new Color(0.75f, 0.35f, 0.35f) },
            { TodoCategory.Crafting, new Color(0.65f, 0.55f, 0.40f) },
            { TodoCategory.Social, new Color(0.70f, 0.50f, 0.60f) },
            { TodoCategory.Quests, new Color(0.80f, 0.70f, 0.30f) },
            { TodoCategory.Collection, new Color(0.55f, 0.70f, 0.65f) }
        };

        // Styles
        private bool _stylesInitialized;
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _itemStyle;
        private GUIStyle _priorityStyle;
        private GUIStyle _categoryStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _emptyStyle;

        // Textures
        private Texture2D _windowBackground;
        private Texture2D _headerBackground;
        private Texture2D _itemEven;
        private Texture2D _itemOdd;

        public void Initialize(TodoManager manager)
        {
            _manager = manager;

            // Default position: top-right corner with some padding
            _windowRect = new Rect(
                Screen.width - WINDOW_WIDTH - 20,
                100,
                WINDOW_WIDTH,
                MIN_HEIGHT
            );

            if (_manager != null)
            {
                _manager.OnTodosChanged += RefreshCache;
                _manager.OnDataLoaded += RefreshCache;
            }
        }

        public void SetPosition(float x, float y)
        {
            _windowRect.x = Mathf.Clamp(x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(y, 0, Screen.height - _windowRect.height);
        }

        public (float x, float y) GetPosition()
        {
            return (_windowRect.x, _windowRect.y);
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        public bool IsEnabled => _isEnabled;

        public void Toggle()
        {
            _isEnabled = !_isEnabled;
            Plugin.Log?.LogInfo($"TodoHUD toggled: {(_isEnabled ? "ON" : "OFF")}");
        }

        private void Update()
        {
            if (!_isEnabled || _manager == null) return;

            // Periodically refresh cache
            if (Time.unscaledTime - _lastUpdateTime > UPDATE_INTERVAL)
            {
                RefreshCache();
                _lastUpdateTime = Time.unscaledTime;
            }
        }

        private void RefreshCache()
        {
            if (_manager == null) return;

            _cachedItems = _manager.GetActiveTodos()
                .OrderByDescending(t => (int)t.Priority)
                .ThenByDescending(t => t.CreatedAt)
                .Take(MAX_ITEMS)
                .ToList();
        }

        private void OnGUI()
        {
            // Don't show if disabled or no manager
            if (!_isEnabled || _manager == null) return;

            // Don't show if main TodoUI is open
            var todoUI = Plugin.GetTodoUI();
            if (todoUI != null && todoUI.IsVisible) return;

            // Don't show if no data loaded
            if (string.IsNullOrEmpty(_manager.CurrentCharacter)) return;

            InitializeStyles();

            // Calculate dynamic height based on content
            float contentHeight = HEADER_HEIGHT + 8; // Header + padding
            if (_cachedItems.Count == 0)
            {
                contentHeight += 40; // Empty message
            }
            else
            {
                contentHeight += _cachedItems.Count * ITEM_HEIGHT + 4;
            }
            _windowRect.height = Mathf.Clamp(contentHeight, MIN_HEIGHT, MAX_HEIGHT);

            // Store position before drawing
            var prevRect = _windowRect;

            // Draw the window
            GUI.depth = -500; // Below main UI but above game
            _windowRect = GUI.Window(WINDOW_ID, _windowRect, DrawWindow, "", _windowStyle);

            // Clamp to screen bounds
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);

            // Notify if position changed (for persistence)
            if (Math.Abs(_windowRect.x - prevRect.x) > 0.1f || Math.Abs(_windowRect.y - prevRect.y) > 0.1f)
            {
                OnPositionChanged?.Invoke(_windowRect.x, _windowRect.y);
            }
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            DrawHeader();
            DrawItems();

            GUILayout.EndVertical();

            // Make the entire header area draggable
            GUI.DragWindow(new Rect(0, 0, WINDOW_WIDTH, HEADER_HEIGHT));
        }

        private void DrawHeader()
        {
            // Header background
            var headerRect = new Rect(0, 0, WINDOW_WIDTH, HEADER_HEIGHT);
            if (_headerBackground != null)
            {
                GUI.DrawTexture(headerRect, _headerBackground);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(8);

            // Title with item count
            var activeCount = _manager?.GetActiveTodos().Count() ?? 0;
            GUILayout.Label($"Todo ({activeCount})", _headerStyle, GUILayout.Height(HEADER_HEIGHT));

            GUILayout.FlexibleSpace();

            // Drag hint
            var hintStyle = new GUIStyle(_headerStyle)
            {
                fontSize = 9,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(_textDark.r, _textDark.g, _textDark.b, 0.5f) }
            };
            var shortcut = Plugin.GetOpenListShortcutDisplay();
            GUILayout.Label($"{shortcut} to open", hintStyle, GUILayout.Height(HEADER_HEIGHT));

            GUILayout.FlexibleSpace();

            // Drag hint
            GUILayout.Label("drag to move", hintStyle, GUILayout.Height(HEADER_HEIGHT));

            GUILayout.Space(8);
            GUILayout.EndHorizontal();
        }

        private void DrawItems()
        {
            GUILayout.Space(4);

            if (_cachedItems.Count == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("No active tasks", _emptyStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                for (int i = 0; i < _cachedItems.Count; i++)
                {
                    DrawItem(_cachedItems[i], i);
                }
            }

            GUILayout.Space(4);
        }

        private void DrawItem(TodoItem item, int index)
        {
            var bgTex = index % 2 == 0 ? _itemEven : _itemOdd;

            // Use row style with background so content stays in layout flow (avoids blank title in some setups)
            var rowStyle = new GUIStyle(_itemStyle)
            {
                normal = { background = bgTex },
                padding = new RectOffset(6, 6, 2, 2)
            };

            GUILayout.BeginHorizontal(rowStyle, GUILayout.Height(ITEM_HEIGHT));
            GUILayout.Space(6);

            // Priority indicator
            var priorityColor = _priorityColors.TryGetValue(item.Priority, out var pc) ? pc : _textDark;
            var priorityStyle = new GUIStyle(_priorityStyle) { normal = { textColor = priorityColor } };
            GUILayout.Label(GetPriorityIcon(item.Priority), priorityStyle, GUILayout.Width(16));

            // Category indicator
            var categoryColor = _categoryColors.TryGetValue(item.Category, out var cc) ? cc : _textDark;
            var categoryStyle = new GUIStyle(_categoryStyle) { normal = { textColor = categoryColor } };
            GUILayout.Label($"[{GetCategoryShort(item.Category)}]", categoryStyle, GUILayout.Width(36));

            GUILayout.Space(4);

            // Title (truncated if too long; guard against null/empty)
            var title = string.IsNullOrWhiteSpace(item.Title) ? "(No title)" : item.Title;
            var displayTitle = title.Length > 25 ? title.Substring(0, 22) + "..." : title;
            GUILayout.Label(displayTitle, _titleStyle, GUILayout.ExpandWidth(true));

            GUILayout.Space(6);
            GUILayout.EndHorizontal();
        }

        private string GetPriorityIcon(TodoPriority priority)
        {
            switch (priority)
            {
                case TodoPriority.Low: return "-";
                case TodoPriority.Normal: return "o";
                case TodoPriority.High: return "!";
                case TodoPriority.Urgent: return "!!";
                default: return "o";
            }
        }

        private string GetCategoryShort(TodoCategory category)
        {
            switch (category)
            {
                case TodoCategory.General: return "GEN";
                case TodoCategory.Farming: return "FRM";
                case TodoCategory.Mining: return "MIN";
                case TodoCategory.Fishing: return "FSH";
                case TodoCategory.Combat: return "CMB";
                case TodoCategory.Crafting: return "CRF";
                case TodoCategory.Social: return "SOC";
                case TodoCategory.Quests: return "QST";
                case TodoCategory.Collection: return "COL";
                default: return "GEN";
            }
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
            _windowBackground = MakeParchmentTexture(16, 64, _parchment, _parchmentLight, _borderDark, 2);
            _headerBackground = MakeGradientTex(8, (int)HEADER_HEIGHT, _parchmentDark, _parchment);
            _itemEven = MakeTex(1, 1, new Color(_parchmentLight.r, _parchmentLight.g, _parchmentLight.b, 0.3f));
            _itemOdd = MakeTex(1, 1, new Color(_parchment.r, _parchment.g, _parchment.b, 0.3f));
        }

        private void CreateStyles()
        {
            _windowStyle = new GUIStyle
            {
                normal = { background = _windowBackground, textColor = _textDark },
                padding = new RectOffset(4, 4, 4, 4),
                border = new RectOffset(4, 4, 4, 4)
            };

            _headerStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _woodDark },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(2, 2, 2, 2)
            };

            _itemStyle = new GUIStyle
            {
                fontSize = 11,
                normal = { textColor = _textDark },
                padding = new RectOffset(2, 2, 2, 2)
            };

            _priorityStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = _textDark }
            };

            _categoryStyle = new GUIStyle
            {
                fontSize = 8,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = _textDark }
            };

            _titleStyle = new GUIStyle
            {
                fontSize = 11,
                normal = { textColor = _textDark },
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };

            _emptyStyle = new GUIStyle
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(_textDark.r, _textDark.g, _textDark.b, 0.6f) },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 10, 10)
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
                Color rowColor = Color.Lerp(topColor, bottomColor, t);
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = rowColor;
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
                    {
                        pixels[y * width + x] = borderColor;
                    }
                    else
                    {
                        float noise = (float)rand.NextDouble() * 0.02f - 0.01f;
                        pixels[y * width + x] = new Color(
                            Mathf.Clamp01(rowBase.r + noise),
                            Mathf.Clamp01(rowBase.g + noise),
                            Mathf.Clamp01(rowBase.b + noise),
                            rowBase.a
                        );
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
            if (_manager != null)
            {
                _manager.OnTodosChanged -= RefreshCache;
                _manager.OnDataLoaded -= RefreshCache;
            }
        }
    }
}
