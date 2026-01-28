using System;
using System.Collections.Generic;
using TheList.Data;
using TheList.Patches;
using UnityEngine;
using Wish;

namespace TheList.UI
{
    /// <summary>
    /// Main UI window for The List todo/journal mod.
    /// </summary>
    public class ListUI : MonoBehaviour
    {
        // Window dimensions
        private const float WINDOW_WIDTH = 500f;
        private const float WINDOW_HEIGHT = 600f;
        private const float ROW_HEIGHT = 50f;
        private const float BUTTON_WIDTH = 80f;

        // State
        private ListManager _listManager;
        private bool _isVisible;
        private Rect _windowRect;
        private Vector2 _scrollPosition;
        private int _windowId;

        // Hotkey
        private KeyCode _toggleKey = KeyCode.J;
        private bool _requireCtrl = false;
        private KeyCode _altToggleKey = KeyCode.F9;

        // Editing state
        private bool _isAddingTask;
        private bool _isEditingTask;
        private string _editingTaskId;
        private string _inputTitle = "";
        private string _inputNotes = "";
        private TaskCategory _inputCategory = TaskCategory.General;
        private TaskPriority _inputPriority = TaskPriority.Normal;

        // Filter state (cached from manager)
        private TaskCategory? _filterCategory = null;
        private SortMode _sortMode = SortMode.Priority;
        private bool _showCompleted = true;

        // Styles
        private bool _stylesInitialized;
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _taskRowStyle;
        private GUIStyle _taskTitleStyle;
        private GUIStyle _taskNotesStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _textAreaStyle;
        private GUIStyle _checkboxStyle;
        private GUIStyle _priorityStyle;
        private GUIStyle _categoryTagStyle;
        private GUIStyle _timestampStyle;

        // Textures
        private Texture2D _windowBackground;
        private Texture2D _headerBackground;
        private Texture2D _buttonNormal;
        private Texture2D _buttonHover;
        private Texture2D _buttonActive;
        private Texture2D _rowEven;
        private Texture2D _rowOdd;
        private Texture2D _rowCompleted;
        private Texture2D _rowHover;
        private Texture2D _inputFieldBg;
        private Texture2D _modalBackground;
        private Texture2D _scrollbarBg;
        private Texture2D _scrollbarThumb;

        // Sun Haven color palette
        private readonly Color _bgDark = new Color(0.12f, 0.13f, 0.16f, 0.98f);
        private readonly Color _bgMedium = new Color(0.16f, 0.17f, 0.21f, 0.95f);
        private readonly Color _bgLight = new Color(0.22f, 0.24f, 0.28f, 0.9f);
        private readonly Color _accentBlue = new Color(0.35f, 0.55f, 0.75f);
        private readonly Color _accentBlueDark = new Color(0.25f, 0.40f, 0.58f);
        private readonly Color _accentBlueLight = new Color(0.50f, 0.70f, 0.90f);
        private readonly Color _gold = new Color(0.95f, 0.82f, 0.35f);
        private readonly Color _goldDark = new Color(0.75f, 0.62f, 0.25f);
        private readonly Color _textPrimary = new Color(0.92f, 0.90f, 0.85f);
        private readonly Color _textSecondary = new Color(0.65f, 0.65f, 0.70f);
        private readonly Color _textMuted = new Color(0.45f, 0.45f, 0.50f);
        private readonly Color _borderColor = new Color(0.35f, 0.38f, 0.45f, 0.6f);

        // Priority colors (Sun Haven style)
        private readonly Color _priorityLow = new Color(0.45f, 0.50f, 0.55f);
        private readonly Color _priorityNormal = new Color(0.70f, 0.75f, 0.80f);
        private readonly Color _priorityHigh = new Color(0.95f, 0.80f, 0.30f);
        private readonly Color _priorityUrgent = new Color(0.95f, 0.40f, 0.35f);

        // Category colors (Sun Haven style - softer, more harmonious)
        private readonly Dictionary<TaskCategory, Color> _categoryColors = new Dictionary<TaskCategory, Color>
        {
            { TaskCategory.General, new Color(0.55f, 0.58f, 0.65f) },
            { TaskCategory.Farming, new Color(0.45f, 0.72f, 0.45f) },
            { TaskCategory.Mining, new Color(0.70f, 0.55f, 0.40f) },
            { TaskCategory.Fishing, new Color(0.45f, 0.65f, 0.85f) },
            { TaskCategory.Combat, new Color(0.85f, 0.40f, 0.40f) },
            { TaskCategory.Quests, new Color(0.95f, 0.80f, 0.35f) },
            { TaskCategory.Social, new Color(0.85f, 0.55f, 0.70f) },
            { TaskCategory.Crafting, new Color(0.65f, 0.50f, 0.80f) },
            { TaskCategory.Shopping, new Color(0.40f, 0.75f, 0.75f) },
            { TaskCategory.Other, new Color(0.50f, 0.52f, 0.55f) }
        };

        public bool IsVisible => _isVisible;

        public void Initialize(ListManager listManager)
        {
            _listManager = listManager;
            _isVisible = false;
            _windowId = GetHashCode();

            // Center window
            float x = (Screen.width - WINDOW_WIDTH) / 2f;
            float y = (Screen.height - WINDOW_HEIGHT) / 2f;
            _windowRect = new Rect(x, y, WINDOW_WIDTH, WINDOW_HEIGHT);

            // Load settings
            var settings = _listManager.GetSettings();
            _filterCategory = settings.FilterCategory;
            _sortMode = settings.SortBy;
            _showCompleted = settings.ShowCompletedTasks;

            Plugin.Log?.LogInfo("ListUI initialized");
        }

        public void SetToggleKey(KeyCode key, bool requireCtrl)
        {
            _toggleKey = key;
            _requireCtrl = requireCtrl;
        }

        public void SetAltToggleKey(KeyCode key)
        {
            _altToggleKey = key;
        }

        public void Toggle()
        {
            if (!PlayerPatches.IsListLoaded)
            {
                Plugin.Log?.LogWarning("Cannot toggle UI: list not loaded");
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
            _isAddingTask = false;
            _isEditingTask = false;
            ClearInputFields();

            // Pause game
            if (Player.Instance != null)
                Player.Instance.AddPauseObject("TheList_UI");

            Plugin.Log?.LogInfo("List UI opened");
        }

        public void Hide()
        {
            _isVisible = false;
            _isAddingTask = false;
            _isEditingTask = false;
            ClearInputFields();

            // Unpause game
            if (Player.Instance != null)
                Player.Instance.RemovePauseObject("TheList_UI");

            Plugin.Log?.LogInfo("List UI closed");
        }

        private void ClearInputFields()
        {
            _inputTitle = "";
            _inputNotes = "";
            _inputCategory = TaskCategory.General;
            _inputPriority = TaskPriority.Normal;
            _editingTaskId = null;
        }

        private void Update()
        {
            // Close on Escape
            if (_isVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                if (_isAddingTask || _isEditingTask)
                {
                    _isAddingTask = false;
                    _isEditingTask = false;
                    ClearInputFields();
                }
                else
                {
                    Hide();
                }
            }
        }

        private void OnGUI()
        {
            if (!_isVisible || _listManager == null || !PlayerPatches.IsListLoaded)
                return;

            InitializeStyles();

            // Draw main window
            _windowRect = GUI.Window(_windowId, _windowRect, DrawWindow, "", _windowStyle);

            // Draw modal if adding/editing
            if (_isAddingTask || _isEditingTask)
            {
                DrawTaskModal();
            }
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            // Create textures with Sun Haven style
            _windowBackground = MakeGradientTex(8, 64, _bgDark, new Color(_bgDark.r * 0.9f, _bgDark.g * 0.9f, _bgDark.b * 0.9f, _bgDark.a));
            _headerBackground = MakeGradientTex(8, 32, _bgLight, _bgMedium);
            _modalBackground = MakeBorderedTex(8, 8, _bgDark, _borderColor, 2);
            _buttonNormal = MakeBorderedTex(8, 8, _accentBlueDark, new Color(_accentBlue.r, _accentBlue.g, _accentBlue.b, 0.4f), 1);
            _buttonHover = MakeBorderedTex(8, 8, _accentBlue, _accentBlueLight, 1);
            _buttonActive = MakeBorderedTex(8, 8, _accentBlueLight, _gold, 1);
            _rowEven = MakeTex(1, 1, new Color(_bgMedium.r, _bgMedium.g, _bgMedium.b, 0.5f));
            _rowOdd = MakeTex(1, 1, new Color(_bgLight.r * 0.85f, _bgLight.g * 0.85f, _bgLight.b * 0.85f, 0.4f));
            _rowCompleted = MakeTex(1, 1, new Color(0.18f, 0.28f, 0.20f, 0.5f));
            _rowHover = MakeTex(1, 1, new Color(_accentBlue.r, _accentBlue.g, _accentBlue.b, 0.2f));
            _inputFieldBg = MakeBorderedTex(8, 8, new Color(0.08f, 0.09f, 0.12f, 0.95f), _borderColor, 1);
            _scrollbarBg = MakeTex(1, 1, new Color(0.1f, 0.11f, 0.14f, 0.8f));
            _scrollbarThumb = MakeTex(1, 1, _accentBlueDark);

            // Window style - Sun Haven dark panel
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = _windowBackground, textColor = _textPrimary },
                onNormal = { background = _windowBackground, textColor = _textPrimary },
                padding = new RectOffset(12, 12, 12, 12),
                border = new RectOffset(8, 8, 8, 8)
            };

            // Title style - Gold header text
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = _gold }
            };

            // Button style - Blue accent buttons
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _buttonNormal, textColor = _textPrimary },
                hover = { background = _buttonHover, textColor = Color.white },
                active = { background = _buttonActive, textColor = _gold },
                onNormal = { background = _buttonHover, textColor = _gold },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 6, 6),
                margin = new RectOffset(2, 2, 2, 2),
                border = new RectOffset(4, 4, 4, 4)
            };

            // Task row style
            _taskRowStyle = new GUIStyle
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(0, 0, 2, 2)
            };

            // Task title style
            _taskTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _textPrimary },
                wordWrap = true
            };

            // Task notes style
            _taskNotesStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = _textSecondary },
                wordWrap = true
            };

            // Label style
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = _textPrimary }
            };

            // TextField style - Dark input field
            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 14,
                normal = { background = _inputFieldBg, textColor = _textPrimary },
                focused = { background = _inputFieldBg, textColor = Color.white },
                hover = { background = _inputFieldBg, textColor = _textPrimary },
                padding = new RectOffset(8, 8, 6, 6),
                border = new RectOffset(4, 4, 4, 4)
            };

            // TextArea style
            _textAreaStyle = new GUIStyle(GUI.skin.textArea)
            {
                fontSize = 12,
                normal = { background = _inputFieldBg, textColor = _textPrimary },
                focused = { background = _inputFieldBg, textColor = Color.white },
                hover = { background = _inputFieldBg, textColor = _textPrimary },
                padding = new RectOffset(8, 8, 6, 6),
                border = new RectOffset(4, 4, 4, 4),
                wordWrap = true
            };

            // Checkbox style
            _checkboxStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = 14,
                normal = { textColor = _textPrimary },
                onNormal = { textColor = _gold }
            };

            // Priority style
            _priorityStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            // Category tag style
            _categoryTagStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 3, 3)
            };

            // Timestamp style
            _timestampStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                normal = { textColor = _textMuted },
                alignment = TextAnchor.MiddleRight
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

        /// <summary>
        /// Creates a vertical gradient texture.
        /// </summary>
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

        /// <summary>
        /// Creates a texture with a subtle border effect.
        /// </summary>
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

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // Header
            DrawHeader();

            GUILayout.Space(10);

            // Toolbar
            DrawToolbar();

            GUILayout.Space(10);

            // Task list
            DrawTaskList();

            GUILayout.Space(10);

            // Footer
            DrawFooter();

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, WINDOW_WIDTH, 40));
        }

        private void DrawHeader()
        {
            // Header background
            var headerRect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(headerRect, _headerBackground, ScaleMode.StretchToFill);

            GUILayout.BeginArea(headerRect);
            GUILayout.BeginHorizontal();

            GUILayout.Space(10);

            // Title
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label("The List", _titleStyle, GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Character name
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            string charName = PlayerPatches.LoadedCharacterName ?? "Unknown";
            var charStyle = new GUIStyle(_labelStyle) { normal = { textColor = _textSecondary }, fontSize = 11 };
            GUILayout.Label($"Character: {charName}", charStyle);
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
            closeStyle.fontStyle = FontStyle.Bold;
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

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();

            // Add task button with gold accent
            var addButtonStyle = new GUIStyle(_buttonStyle);
            addButtonStyle.normal.background = MakeBorderedTex(8, 8, _goldDark, _gold, 1);
            addButtonStyle.hover.background = MakeBorderedTex(8, 8, _gold, new Color(1f, 0.95f, 0.7f), 1);
            addButtonStyle.normal.textColor = new Color(0.1f, 0.08f, 0.02f);
            addButtonStyle.hover.textColor = new Color(0.05f, 0.04f, 0.01f);
            addButtonStyle.fontSize = 13;
            if (GUILayout.Button("+ Add Task", addButtonStyle, GUILayout.Width(110), GUILayout.Height(28)))
            {
                _isAddingTask = true;
                ClearInputFields();
            }

            GUILayout.FlexibleSpace();

            // Category filter
            GUILayout.Label("Filter:", _labelStyle, GUILayout.Width(40));
            string[] categories = new string[] { "All", "General", "Farming", "Mining", "Fishing", "Combat", "Quests", "Social", "Crafting", "Shopping", "Other" };
            int currentFilter = _filterCategory.HasValue ? (int)_filterCategory.Value + 1 : 0;
            int newFilter = GUILayout.SelectionGrid(currentFilter, categories, 4, _buttonStyle, GUILayout.Width(200));
            if (newFilter != currentFilter)
            {
                _filterCategory = newFilter == 0 ? null : (TaskCategory?)(newFilter - 1);
                _listManager.SetFilterCategory(_filterCategory);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            // Sort mode
            GUILayout.Label("Sort:", _labelStyle, GUILayout.Width(35));
            string[] sortModes = { "Priority", "Date", "Category" };
            int currentSort = (int)_sortMode;
            if (currentSort > 2) currentSort = 0;
            int newSort = GUILayout.SelectionGrid(currentSort, sortModes, 3, _buttonStyle, GUILayout.Width(180));
            if (newSort != currentSort)
            {
                _sortMode = (SortMode)newSort;
                _listManager.SetSortMode(_sortMode);
            }

            GUILayout.FlexibleSpace();

            // Show completed toggle
            bool newShowCompleted = GUILayout.Toggle(_showCompleted, " Show Completed", _checkboxStyle);
            if (newShowCompleted != _showCompleted)
            {
                _showCompleted = newShowCompleted;
                _listManager.SetShowCompletedTasks(_showCompleted);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawTaskList()
        {
            var tasks = _listManager.GetFilteredTasks();

            // Scroll view
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            if (tasks.Count == 0)
            {
                GUILayout.Label("No tasks to show. Click '+ Add Task' to create one!", _labelStyle);
            }
            else
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    DrawTaskRow(tasks[i], i % 2 == 0);
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawTaskRow(TaskItem task, bool isEven)
        {
            // Row background
            Color bgColor = task.IsCompleted ? new Color(0.15f, 0.2f, 0.15f, 0.6f) :
                           (isEven ? new Color(0.2f, 0.2f, 0.22f, 0.8f) : new Color(0.18f, 0.18f, 0.2f, 0.8f));

            var rowRect = GUILayoutUtility.GetRect(0, ROW_HEIGHT, GUILayout.ExpandWidth(true));
            GUI.color = bgColor;
            GUI.DrawTexture(rowRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(rowRect);
            GUILayout.BeginHorizontal();

            // Checkbox
            GUILayout.Space(5);
            bool newCompleted = GUILayout.Toggle(task.IsCompleted, "", _checkboxStyle, GUILayout.Width(20), GUILayout.Height(ROW_HEIGHT - 10));
            if (newCompleted != task.IsCompleted)
            {
                _listManager.ToggleTaskComplete(task.Id);
            }

            // Priority indicator
            GUILayout.Space(5);
            Color priorityColor = GetPriorityColor(task.Priority);
            GUI.color = priorityColor;
            GUILayout.Label(GetPrioritySymbol(task.Priority), _priorityStyle, GUILayout.Width(20), GUILayout.Height(ROW_HEIGHT - 10));
            GUI.color = Color.white;

            // Task content
            GUILayout.BeginVertical();
            GUILayout.Space(5);

            // Title (with strikethrough if completed)
            var titleStyle = new GUIStyle(_taskTitleStyle);
            if (task.IsCompleted)
            {
                titleStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            }
            GUILayout.Label(task.Title, titleStyle);

            // Notes preview (if exists)
            if (!string.IsNullOrEmpty(task.Notes))
            {
                string notesPreview = task.Notes.Length > 50 ? task.Notes.Substring(0, 47) + "..." : task.Notes;
                GUILayout.Label(notesPreview, _taskNotesStyle);
            }

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Category tag
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            Color catColor = _categoryColors.TryGetValue(task.Category, out var c) ? c : Color.gray;
            GUI.color = catColor;
            GUILayout.Label(task.Category.ToString(), _categoryTagStyle, GUILayout.Width(60));
            GUI.color = Color.white;

            // Timestamp
            var settings = _listManager.GetSettings();
            if (settings.ShowTimestamps)
            {
                string timestamp = task.IsCompleted && task.CompletedAt.HasValue
                    ? $"Done: {task.CompletedAt.Value:MM/dd HH:mm}"
                    : $"Created: {task.CreatedAt:MM/dd HH:mm}";
                GUILayout.Label(timestamp, _timestampStyle, GUILayout.Width(100));
            }
            GUILayout.EndVertical();

            // Edit/Delete buttons
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            if (GUILayout.Button("Edit", _buttonStyle, GUILayout.Width(50)))
            {
                StartEditTask(task);
            }
            if (GUILayout.Button("Del", _buttonStyle, GUILayout.Width(50)))
            {
                _listManager.DeleteTask(task.Id);
            }
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private Color GetPriorityColor(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Low => _priorityLow,
                TaskPriority.Normal => _priorityNormal,
                TaskPriority.High => _priorityHigh,
                TaskPriority.Urgent => _priorityUrgent,
                _ => _priorityNormal
            };
        }

        private string GetPrioritySymbol(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Low => "-",
                TaskPriority.Normal => "o",
                TaskPriority.High => "!",
                TaskPriority.Urgent => "!!",
                _ => "o"
            };
        }

        private void DrawFooter()
        {
            var (pending, completed, total) = _listManager.GetTaskCounts();

            GUILayout.BeginHorizontal();

            GUILayout.Label($"Tasks: {pending} pending, {completed} completed ({total} total)", _labelStyle);

            GUILayout.FlexibleSpace();

            if (completed > 0)
            {
                if (GUILayout.Button("Clear Completed", _buttonStyle, GUILayout.Width(120)))
                {
                    _listManager.ClearCompletedTasks();
                }
            }

            GUILayout.EndHorizontal();
        }

        private void StartEditTask(TaskItem task)
        {
            _isEditingTask = true;
            _isAddingTask = false;
            _editingTaskId = task.Id;
            _inputTitle = task.Title;
            _inputNotes = task.Notes;
            _inputCategory = task.Category;
            _inputPriority = task.Priority;
        }

        private void DrawTaskModal()
        {
            // Check for Enter key to confirm
            bool enterPressed = Event.current.type == UnityEngine.EventType.KeyDown &&
                               (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);

            // Darken background
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Modal window
            float modalWidth = 400;
            float modalHeight = 380;
            float x = (Screen.width - modalWidth) / 2f;
            float y = (Screen.height - modalHeight) / 2f;
            Rect modalRect = new Rect(x, y, modalWidth, modalHeight);

            GUI.Box(modalRect, "", _windowStyle);

            GUILayout.BeginArea(new Rect(x + 15, y + 15, modalWidth - 30, modalHeight - 30));
            GUILayout.BeginVertical();

            // Title
            string modalTitle = _isEditingTask ? "Edit Task" : "Add New Task";
            GUILayout.Label(modalTitle, _titleStyle);
            GUILayout.Space(15);

            // Title input
            GUILayout.Label("Title:", _labelStyle);
            GUI.SetNextControlName("TaskTitleField");
            _inputTitle = GUILayout.TextField(_inputTitle, _textFieldStyle, GUILayout.Height(25));

            GUILayout.Space(10);

            // Notes input
            GUILayout.Label("Notes (optional):", _labelStyle);
            _inputNotes = GUILayout.TextArea(_inputNotes, _textAreaStyle, GUILayout.Height(60));

            GUILayout.Space(10);

            // Category
            GUILayout.Label("Category:", _labelStyle);
            string[] categories = Enum.GetNames(typeof(TaskCategory));
            int catIndex = (int)_inputCategory;
            int newCatIndex = GUILayout.SelectionGrid(catIndex, categories, 5, _buttonStyle);
            _inputCategory = (TaskCategory)newCatIndex;

            GUILayout.Space(10);

            // Priority
            GUILayout.Label("Priority:", _labelStyle);
            string[] priorities = { "Low", "Normal", "High", "Urgent" };
            int priIndex = (int)_inputPriority;
            int newPriIndex = GUILayout.SelectionGrid(priIndex, priorities, 4, _buttonStyle);
            _inputPriority = (TaskPriority)newPriIndex;

            GUILayout.Space(15);

            // Hint text
            GUILayout.Label("Press Enter to confirm, Escape to cancel", _timestampStyle);

            GUILayout.Space(10);

            // Buttons
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Cancel", _buttonStyle, GUILayout.Height(35), GUILayout.Width(100)))
            {
                _isAddingTask = false;
                _isEditingTask = false;
                ClearInputFields();
            }

            GUILayout.FlexibleSpace();

            // Confirm button with gold Sun Haven style
            var confirmStyle = new GUIStyle(_buttonStyle);
            confirmStyle.normal.background = MakeBorderedTex(8, 8, _goldDark, _gold, 1);
            confirmStyle.hover.background = MakeBorderedTex(8, 8, _gold, new Color(1f, 0.95f, 0.7f), 1);
            confirmStyle.normal.textColor = new Color(0.1f, 0.08f, 0.02f);
            confirmStyle.hover.textColor = new Color(0.05f, 0.04f, 0.01f);
            confirmStyle.fontSize = 14;
            confirmStyle.fontStyle = FontStyle.Bold;

            bool confirmClicked = GUILayout.Button(_isEditingTask ? "Save Task" : "Add Task", confirmStyle, GUILayout.Height(35), GUILayout.Width(120));

            // Handle confirm via button click OR Enter key
            if (confirmClicked || enterPressed)
            {
                if (!string.IsNullOrWhiteSpace(_inputTitle))
                {
                    if (_isEditingTask && !string.IsNullOrEmpty(_editingTaskId))
                    {
                        _listManager.UpdateTask(_editingTaskId, _inputTitle, _inputNotes, _inputCategory, _inputPriority);
                    }
                    else
                    {
                        _listManager.AddTask(_inputTitle, _inputNotes, _inputCategory, _inputPriority);
                    }

                    _isAddingTask = false;
                    _isEditingTask = false;
                    ClearInputFields();

                    if (enterPressed)
                    {
                        Event.current.Use();
                    }
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // Focus the title field when modal opens
            if (Event.current.type == UnityEngine.EventType.Repaint && string.IsNullOrEmpty(_inputTitle))
            {
                GUI.FocusControl("TaskTitleField");
            }
        }
    }
}
