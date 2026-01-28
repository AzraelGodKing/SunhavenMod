using System;
using TheList.Data;
using TheList.Patches;
using UnityEngine;

namespace TheList.UI
{
    /// <summary>
    /// Mini HUD that shows pending task count and top priority task.
    /// </summary>
    public class ListHUD : MonoBehaviour
    {
        public enum HUDPosition
        {
            TopLeft,
            TopCenter,
            TopRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }

        // Dimensions
        private const float HUD_WIDTH = 250f;
        private const float HUD_HEIGHT = 50f;
        private const float PADDING = 10f;

        // State
        private ListManager _listManager;
        private bool _isEnabled = true;
        private HUDPosition _position = HUDPosition.TopRight;

        // Cached data
        private int _pendingCount;
        private string _topTaskTitle;
        private TaskPriority _topTaskPriority;
        private float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 1f;

        // Styles
        private bool _stylesInitialized;
        private GUIStyle _hudBoxStyle;
        private GUIStyle _countStyle;
        private GUIStyle _taskStyle;
        private GUIStyle _priorityStyle;
        private Texture2D _hudBackground;

        // Sun Haven color palette
        private readonly Color _bgDark = new Color(0.12f, 0.13f, 0.16f, 0.92f);
        private readonly Color _accentBlue = new Color(0.35f, 0.55f, 0.75f);
        private readonly Color _gold = new Color(0.95f, 0.82f, 0.35f);
        private readonly Color _textPrimary = new Color(0.92f, 0.90f, 0.85f);
        private readonly Color _textSecondary = new Color(0.65f, 0.65f, 0.70f);
        private readonly Color _borderColor = new Color(0.35f, 0.38f, 0.45f, 0.6f);

        // Priority colors (Sun Haven style)
        private readonly Color _priorityLow = new Color(0.45f, 0.50f, 0.55f);
        private readonly Color _priorityNormal = new Color(0.70f, 0.75f, 0.80f);
        private readonly Color _priorityHigh = new Color(0.95f, 0.80f, 0.30f);
        private readonly Color _priorityUrgent = new Color(0.95f, 0.40f, 0.35f);

        public void Initialize(ListManager listManager)
        {
            _listManager = listManager;
            _lastUpdateTime = 0;

            // Subscribe to changes
            _listManager.OnTasksChanged += UpdateCachedData;
            _listManager.OnListLoaded += UpdateCachedData;

            Plugin.Log?.LogInfo("ListHUD initialized");
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        public void SetPosition(HUDPosition position)
        {
            _position = position;
        }

        private void Update()
        {
            // Update cached data periodically
            if (_isEnabled && Time.time - _lastUpdateTime > UPDATE_INTERVAL)
            {
                UpdateCachedData();
            }
        }

        private void UpdateCachedData()
        {
            if (_listManager == null) return;

            var (pending, _, _) = _listManager.GetTaskCounts();
            _pendingCount = pending;

            var topTask = _listManager.GetTopPriorityTask();
            if (topTask != null)
            {
                _topTaskTitle = topTask.Title;
                _topTaskPriority = topTask.Priority;

                // Truncate title if too long
                if (_topTaskTitle.Length > 25)
                {
                    _topTaskTitle = _topTaskTitle.Substring(0, 22) + "...";
                }
            }
            else
            {
                _topTaskTitle = null;
            }

            _lastUpdateTime = Time.time;
        }

        private void OnDestroy()
        {
            if (_listManager != null)
            {
                _listManager.OnTasksChanged -= UpdateCachedData;
                _listManager.OnListLoaded -= UpdateCachedData;
            }
        }

        private void OnGUI()
        {
            if (!_isEnabled || _listManager == null || !PlayerPatches.IsListLoaded)
                return;

            // Hide if main window is open
            if (Plugin.GetListUI()?.IsVisible == true)
                return;

            InitializeStyles();

            Rect hudRect = GetHUDRect();

            // Draw background
            GUI.Box(hudRect, "", _hudBoxStyle);

            // Draw content
            GUILayout.BeginArea(hudRect);
            GUILayout.BeginHorizontal();

            GUILayout.Space(PADDING);

            // Pending count
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            string countText = _pendingCount.ToString();
            GUILayout.Label(countText, _countStyle);

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.Space(5);

            // Label and top task
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.Label(_pendingCount == 1 ? "task" : "tasks", _taskStyle);

            if (_topTaskTitle != null)
            {
                var priorityStyle = new GUIStyle(_priorityStyle);
                priorityStyle.normal.textColor = GetPriorityColor(_topTaskPriority);
                GUILayout.Label(_topTaskTitle, priorityStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Click hint
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            var hintStyle = new GUIStyle(_taskStyle) { normal = { textColor = _accentBlue } };
            GUILayout.Label("[J]", hintStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.Space(PADDING);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Handle click to open
            if (Event.current.type == EventType.MouseDown && hudRect.Contains(Event.current.mousePosition))
            {
                Plugin.ToggleUI();
                Event.current.Use();
            }
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _hudBackground = MakeBorderedTex(8, 8, _bgDark, _borderColor, 1);

            _hudBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _hudBackground },
                border = new RectOffset(4, 4, 4, 4)
            };

            _countStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _gold },
                alignment = TextAnchor.MiddleCenter
            };

            _taskStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = _textSecondary },
                alignment = TextAnchor.MiddleLeft
            };

            _priorityStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleLeft
            };

            _stylesInitialized = true;
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

        private Rect GetHUDRect()
        {
            float x, y;

            switch (_position)
            {
                case HUDPosition.TopLeft:
                    x = PADDING;
                    y = PADDING;
                    break;
                case HUDPosition.TopCenter:
                    x = (Screen.width - HUD_WIDTH) / 2f;
                    y = PADDING;
                    break;
                case HUDPosition.TopRight:
                    x = Screen.width - HUD_WIDTH - PADDING;
                    y = PADDING;
                    break;
                case HUDPosition.BottomLeft:
                    x = PADDING;
                    y = Screen.height - HUD_HEIGHT - PADDING;
                    break;
                case HUDPosition.BottomCenter:
                    x = (Screen.width - HUD_WIDTH) / 2f;
                    y = Screen.height - HUD_HEIGHT - PADDING;
                    break;
                case HUDPosition.BottomRight:
                    x = Screen.width - HUD_WIDTH - PADDING;
                    y = Screen.height - HUD_HEIGHT - PADDING;
                    break;
                default:
                    x = Screen.width - HUD_WIDTH - PADDING;
                    y = PADDING;
                    break;
            }

            return new Rect(x, y, HUD_WIDTH, HUD_HEIGHT);
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
    }
}
