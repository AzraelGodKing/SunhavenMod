using System;
using System.Collections.Generic;
using System.Linq;
using HavensAlmanac.Data;
using SunhavenTodo;
using SunhavenTodo.Data;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    public class TodoDataProvider : IModDataProvider
    {
        public string ModName => "Todo";
        public string ModIcon => "\u270E";

        private int _activeCount;
        private int _totalCount;
        private int _completedCount;
        private float _completionPercent;
        private List<TodoItem> _highPriorityItems = new List<TodoItem>();
        private string _hudSummary = "Loading...";
        private bool _isReady;

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;
        public bool HasBriefingContent => _isReady && _activeCount > 0;

        public void Refresh()
        {
            try
            {
                var manager = SunhavenTodo.Plugin.GetTodoManager();
                if (manager == null) { _isReady = false; return; }

                var stats = manager.GetStats();
                _totalCount = stats.total;
                _completedCount = stats.completed;
                _activeCount = stats.active;
                _completionPercent = manager.GetCompletionPercent();

                _highPriorityItems = manager.GetActiveTodos()
                    .Where(t => t.Priority >= TodoPriority.High)
                    .OrderByDescending(t => (int)t.Priority)
                    .Take(5)
                    .ToList();

                _hudSummary = _totalCount == 0
                    ? "No tasks"
                    : $"{_activeCount} active / {_completionPercent:F0}%";
                _isReady = true;
            }
            catch (Exception ex)
            {
                _hudSummary = "Error";
                _isReady = false;
                HavensAlmanac.Plugin.Log?.LogWarning($"[TodoProvider] Refresh error: {ex.Message}");
            }
        }

        public void DrawDashboardSection()
        {
            GUILayout.Label($"Total: {_totalCount}  |  Completed: {_completedCount}  |  Active: {_activeCount}");

            // Progress bar
            var barRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
            GUI.Box(barRect, "");
            if (_totalCount > 0)
            {
                var fillRect = new Rect(barRect.x + 1, barRect.y + 1, (barRect.width - 2) * (_completionPercent / 100f), barRect.height - 2);
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            }
            GUILayout.Label($"{_completionPercent:F0}% complete");

            if (_highPriorityItems.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("High Priority Tasks:", GUI.skin.label);
                foreach (var item in _highPriorityItems)
                {
                    string priority = item.Priority == TodoPriority.Urgent ? "[!] " : "[*] ";
                    GUILayout.Label($"  {priority}{item.Title}");
                }
            }
        }

        public bool DrawBriefingSection()
        {
            if (_activeCount == 0) return false;

            int highCount = _highPriorityItems.Count;
            GUILayout.Label($"You have {_activeCount} active task{(_activeCount != 1 ? "s" : "")}.");
            if (highCount > 0)
                GUILayout.Label($"  {highCount} {(highCount != 1 ? "are" : "is")} high priority!");
            return true;
        }
    }
}
