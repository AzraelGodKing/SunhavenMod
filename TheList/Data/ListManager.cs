using System;
using System.Collections.Generic;
using System.Linq;

namespace TheList.Data
{
    /// <summary>
    /// Manages all task operations and provides the API for UI and other systems.
    /// </summary>
    public class ListManager
    {
        private ListData _listData;
        private bool _isDirty;

        /// <summary>
        /// Event fired when any task changes (add, edit, delete, complete).
        /// </summary>
        public event Action OnTasksChanged;

        /// <summary>
        /// Event fired when list data is loaded.
        /// </summary>
        public event Action OnListLoaded;

        /// <summary>
        /// Event fired when settings change.
        /// </summary>
        public event Action OnSettingsChanged;

        public ListManager()
        {
            _listData = new ListData();
            _isDirty = false;
        }

        #region Data Management

        /// <summary>
        /// Load list data (called by save system).
        /// </summary>
        public void LoadListData(ListData data)
        {
            _listData = data ?? new ListData();
            _isDirty = false;
            Plugin.Log?.LogInfo($"List data loaded for player: {_listData.PlayerName}");
            OnListLoaded?.Invoke();
        }

        /// <summary>
        /// Get current list data for saving.
        /// </summary>
        public ListData GetListData()
        {
            _listData.LastSaved = DateTime.Now;
            return _listData;
        }

        /// <summary>
        /// Check if list has unsaved changes.
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Mark list as saved.
        /// </summary>
        public void MarkClean()
        {
            _isDirty = false;
        }

        /// <summary>
        /// Set the player name for this list.
        /// </summary>
        public void SetPlayerName(string playerName)
        {
            _listData.PlayerName = playerName;
            _isDirty = true;
        }

        /// <summary>
        /// Get the player name for this list.
        /// </summary>
        public string GetPlayerName()
        {
            return _listData.PlayerName;
        }

        #endregion

        #region Task CRUD Operations

        /// <summary>
        /// Add a new task to the list.
        /// </summary>
        public TaskItem AddTask(string title, string notes = "", TaskCategory category = TaskCategory.General, TaskPriority priority = TaskPriority.Normal)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                Plugin.Log?.LogWarning("Cannot add task with empty title");
                return null;
            }

            var task = new TaskItem
            {
                Title = title.Trim(),
                Notes = notes?.Trim() ?? "",
                Category = category,
                Priority = priority,
                SortOrder = _listData.Tasks.Count
            };

            _listData.Tasks.Add(task);
            _isDirty = true;

            Plugin.Log?.LogInfo($"Added task: {task.Title}");
            OnTasksChanged?.Invoke();

            return task;
        }

        /// <summary>
        /// Update an existing task.
        /// </summary>
        public bool UpdateTask(string taskId, string title = null, string notes = null, TaskCategory? category = null, TaskPriority? priority = null)
        {
            var task = GetTaskById(taskId);
            if (task == null)
            {
                Plugin.Log?.LogWarning($"Task not found: {taskId}");
                return false;
            }

            if (title != null)
                task.Title = title.Trim();
            if (notes != null)
                task.Notes = notes.Trim();
            if (category.HasValue)
                task.Category = category.Value;
            if (priority.HasValue)
                task.Priority = priority.Value;

            _isDirty = true;
            OnTasksChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Delete a task from the list.
        /// </summary>
        public bool DeleteTask(string taskId)
        {
            var task = GetTaskById(taskId);
            if (task == null)
            {
                Plugin.Log?.LogWarning($"Task not found for deletion: {taskId}");
                return false;
            }

            _listData.Tasks.Remove(task);
            _isDirty = true;

            Plugin.Log?.LogInfo($"Deleted task: {task.Title}");
            OnTasksChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Toggle task completion status.
        /// </summary>
        public bool ToggleTaskComplete(string taskId)
        {
            var task = GetTaskById(taskId);
            if (task == null)
            {
                Plugin.Log?.LogWarning($"Task not found: {taskId}");
                return false;
            }

            task.IsCompleted = !task.IsCompleted;
            task.CompletedAt = task.IsCompleted ? DateTime.Now : (DateTime?)null;
            _isDirty = true;

            Plugin.Log?.LogInfo($"Task '{task.Title}' marked as {(task.IsCompleted ? "completed" : "pending")}");
            OnTasksChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Set task completion status explicitly.
        /// </summary>
        public bool SetTaskComplete(string taskId, bool isCompleted)
        {
            var task = GetTaskById(taskId);
            if (task == null) return false;

            if (task.IsCompleted != isCompleted)
            {
                task.IsCompleted = isCompleted;
                task.CompletedAt = isCompleted ? DateTime.Now : (DateTime?)null;
                _isDirty = true;
                OnTasksChanged?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// Get a task by its ID.
        /// </summary>
        public TaskItem GetTaskById(string taskId)
        {
            return _listData.Tasks.FirstOrDefault(t => t.Id == taskId);
        }

        #endregion

        #region Task Queries

        /// <summary>
        /// Get all tasks.
        /// </summary>
        public IReadOnlyList<TaskItem> GetAllTasks()
        {
            return _listData.Tasks.AsReadOnly();
        }

        /// <summary>
        /// Get tasks filtered and sorted according to current settings.
        /// </summary>
        public List<TaskItem> GetFilteredTasks()
        {
            var settings = _listData.Settings;
            var tasks = _listData.Tasks.AsEnumerable();

            // Filter by completion
            if (!settings.ShowCompletedTasks)
            {
                tasks = tasks.Where(t => !t.IsCompleted);
            }

            // Filter by category
            if (settings.FilterCategory.HasValue)
            {
                tasks = tasks.Where(t => t.Category == settings.FilterCategory.Value);
            }

            // Sort
            tasks = settings.SortBy switch
            {
                SortMode.Priority => tasks.OrderByDescending(t => t.Priority)
                                          .ThenBy(t => t.IsCompleted)
                                          .ThenBy(t => t.CreatedAt),
                SortMode.CreatedDate => tasks.OrderByDescending(t => t.CreatedAt),
                SortMode.Category => tasks.OrderBy(t => t.Category)
                                          .ThenByDescending(t => t.Priority),
                SortMode.Manual => tasks.OrderBy(t => t.SortOrder),
                _ => tasks
            };

            return tasks.ToList();
        }

        /// <summary>
        /// Get pending (incomplete) tasks.
        /// </summary>
        public List<TaskItem> GetPendingTasks()
        {
            return _listData.Tasks.Where(t => !t.IsCompleted).ToList();
        }

        /// <summary>
        /// Get completed tasks.
        /// </summary>
        public List<TaskItem> GetCompletedTasks()
        {
            return _listData.Tasks.Where(t => t.IsCompleted).ToList();
        }

        /// <summary>
        /// Get tasks by category.
        /// </summary>
        public List<TaskItem> GetTasksByCategory(TaskCategory category)
        {
            return _listData.Tasks.Where(t => t.Category == category).ToList();
        }

        /// <summary>
        /// Get the highest priority pending task.
        /// </summary>
        public TaskItem GetTopPriorityTask()
        {
            return _listData.Tasks
                .Where(t => !t.IsCompleted)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .FirstOrDefault();
        }

        /// <summary>
        /// Get counts for display.
        /// </summary>
        public (int pending, int completed, int total) GetTaskCounts()
        {
            int completed = _listData.Tasks.Count(t => t.IsCompleted);
            int total = _listData.Tasks.Count;
            return (total - completed, completed, total);
        }

        #endregion

        #region Settings

        /// <summary>
        /// Get current settings.
        /// </summary>
        public ListSettings GetSettings()
        {
            return _listData.Settings;
        }

        /// <summary>
        /// Update show completed tasks setting.
        /// </summary>
        public void SetShowCompletedTasks(bool show)
        {
            if (_listData.Settings.ShowCompletedTasks != show)
            {
                _listData.Settings.ShowCompletedTasks = show;
                _isDirty = true;
                OnSettingsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Update show timestamps setting.
        /// </summary>
        public void SetShowTimestamps(bool show)
        {
            if (_listData.Settings.ShowTimestamps != show)
            {
                _listData.Settings.ShowTimestamps = show;
                _isDirty = true;
                OnSettingsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Update filter category.
        /// </summary>
        public void SetFilterCategory(TaskCategory? category)
        {
            if (_listData.Settings.FilterCategory != category)
            {
                _listData.Settings.FilterCategory = category;
                _isDirty = true;
                OnSettingsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Update sort mode.
        /// </summary>
        public void SetSortMode(SortMode mode)
        {
            if (_listData.Settings.SortBy != mode)
            {
                _listData.Settings.SortBy = mode;
                _isDirty = true;
                OnSettingsChanged?.Invoke();
            }
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Clear all completed tasks.
        /// </summary>
        public int ClearCompletedTasks()
        {
            int removed = _listData.Tasks.RemoveAll(t => t.IsCompleted);
            if (removed > 0)
            {
                _isDirty = true;
                Plugin.Log?.LogInfo($"Cleared {removed} completed tasks");
                OnTasksChanged?.Invoke();
            }
            return removed;
        }

        /// <summary>
        /// Clear all tasks.
        /// </summary>
        public void ClearAllTasks()
        {
            if (_listData.Tasks.Count > 0)
            {
                _listData.Tasks.Clear();
                _isDirty = true;
                Plugin.Log?.LogInfo("Cleared all tasks");
                OnTasksChanged?.Invoke();
            }
        }

        /// <summary>
        /// Move task to a new position (for manual sorting).
        /// </summary>
        public void MoveTask(string taskId, int newIndex)
        {
            var task = GetTaskById(taskId);
            if (task == null) return;

            int oldIndex = _listData.Tasks.IndexOf(task);
            if (oldIndex == newIndex) return;

            _listData.Tasks.RemoveAt(oldIndex);
            _listData.Tasks.Insert(Math.Min(newIndex, _listData.Tasks.Count), task);

            // Update sort orders
            for (int i = 0; i < _listData.Tasks.Count; i++)
            {
                _listData.Tasks[i].SortOrder = i;
            }

            _isDirty = true;
            OnTasksChanged?.Invoke();
        }

        #endregion
    }
}
