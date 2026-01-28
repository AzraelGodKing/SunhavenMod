using System;
using System.Collections.Generic;

namespace TheList.Data
{
    /// <summary>
    /// Represents a single task/todo item in the list.
    /// </summary>
    [Serializable]
    public class TaskItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
        public TaskCategory Category { get; set; }
        public TaskPriority Priority { get; set; }
        public bool IsCompleted { get; set; }
        public long CreatedAtTicks { get; set; }
        public long? CompletedAtTicks { get; set; }
        public int SortOrder { get; set; }

        public DateTime CreatedAt
        {
            get => new DateTime(CreatedAtTicks);
            set => CreatedAtTicks = value.Ticks;
        }

        public DateTime? CompletedAt
        {
            get => CompletedAtTicks.HasValue ? new DateTime(CompletedAtTicks.Value) : (DateTime?)null;
            set => CompletedAtTicks = value?.Ticks;
        }

        public TaskItem()
        {
            Id = Guid.NewGuid().ToString();
            Title = "";
            Notes = "";
            Category = TaskCategory.General;
            Priority = TaskPriority.Normal;
            IsCompleted = false;
            CreatedAt = DateTime.Now;
            CompletedAt = null;
            SortOrder = 0;
        }

        public TaskItem Clone()
        {
            return new TaskItem
            {
                Id = this.Id,
                Title = this.Title,
                Notes = this.Notes,
                Category = this.Category,
                Priority = this.Priority,
                IsCompleted = this.IsCompleted,
                CreatedAtTicks = this.CreatedAtTicks,
                CompletedAtTicks = this.CompletedAtTicks,
                SortOrder = this.SortOrder
            };
        }
    }

    /// <summary>
    /// Task categories for organization.
    /// </summary>
    public enum TaskCategory
    {
        General,
        Farming,
        Mining,
        Fishing,
        Combat,
        Quests,
        Social,
        Crafting,
        Shopping,
        Other
    }

    /// <summary>
    /// Task priority levels.
    /// </summary>
    public enum TaskPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }

    /// <summary>
    /// Sort modes for task list display.
    /// </summary>
    public enum SortMode
    {
        Priority,
        CreatedDate,
        Category,
        Manual
    }

    /// <summary>
    /// User preferences for list display.
    /// </summary>
    [Serializable]
    public class ListSettings
    {
        public bool ShowCompletedTasks { get; set; } = true;
        public bool ShowTimestamps { get; set; } = true;
        public int FilterCategoryIndex { get; set; } = -1; // -1 = All
        public SortMode SortBy { get; set; } = SortMode.Priority;

        public TaskCategory? FilterCategory
        {
            get => FilterCategoryIndex >= 0 ? (TaskCategory)FilterCategoryIndex : (TaskCategory?)null;
            set => FilterCategoryIndex = value.HasValue ? (int)value.Value : -1;
        }
    }

    /// <summary>
    /// Main data container for a player's todo list.
    /// </summary>
    [Serializable]
    public class ListData
    {
        public int Version { get; set; } = 1;
        public string PlayerName { get; set; }
        public long LastSavedTicks { get; set; }
        public List<TaskItem> Tasks { get; set; }
        public ListSettings Settings { get; set; }

        public DateTime LastSaved
        {
            get => new DateTime(LastSavedTicks);
            set => LastSavedTicks = value.Ticks;
        }

        public ListData()
        {
            Version = 1;
            PlayerName = "";
            LastSaved = DateTime.Now;
            Tasks = new List<TaskItem>();
            Settings = new ListSettings();
        }

        public ListData Clone()
        {
            var clone = new ListData
            {
                Version = this.Version,
                PlayerName = this.PlayerName,
                LastSavedTicks = this.LastSavedTicks,
                Tasks = new List<TaskItem>(),
                Settings = new ListSettings
                {
                    ShowCompletedTasks = this.Settings.ShowCompletedTasks,
                    ShowTimestamps = this.Settings.ShowTimestamps,
                    FilterCategoryIndex = this.Settings.FilterCategoryIndex,
                    SortBy = this.Settings.SortBy
                }
            };

            foreach (var task in this.Tasks)
            {
                clone.Tasks.Add(task.Clone());
            }

            return clone;
        }
    }

    /// <summary>
    /// Wrapper for JSON serialization (Unity JsonUtility compatibility).
    /// </summary>
    [Serializable]
    public class ListDataWrapper
    {
        public int Version;
        public string PlayerName;
        public long LastSavedTicks;

        // Settings
        public bool ShowCompletedTasks;
        public bool ShowTimestamps;
        public int FilterCategoryIndex;
        public int SortByIndex;

        // Tasks as parallel arrays (Unity JsonUtility doesn't support List<T> well)
        public string[] TaskIds;
        public string[] TaskTitles;
        public string[] TaskNotes;
        public int[] TaskCategories;
        public int[] TaskPriorities;
        public bool[] TaskCompleted;
        public long[] TaskCreatedTicks;
        public long[] TaskCompletedTicks;
        public bool[] TaskHasCompletedTime;
        public int[] TaskSortOrders;

        public static ListDataWrapper FromListData(ListData data)
        {
            var wrapper = new ListDataWrapper
            {
                Version = data.Version,
                PlayerName = data.PlayerName ?? "",
                LastSavedTicks = data.LastSavedTicks,
                ShowCompletedTasks = data.Settings.ShowCompletedTasks,
                ShowTimestamps = data.Settings.ShowTimestamps,
                FilterCategoryIndex = data.Settings.FilterCategoryIndex,
                SortByIndex = (int)data.Settings.SortBy
            };

            int count = data.Tasks.Count;
            wrapper.TaskIds = new string[count];
            wrapper.TaskTitles = new string[count];
            wrapper.TaskNotes = new string[count];
            wrapper.TaskCategories = new int[count];
            wrapper.TaskPriorities = new int[count];
            wrapper.TaskCompleted = new bool[count];
            wrapper.TaskCreatedTicks = new long[count];
            wrapper.TaskCompletedTicks = new long[count];
            wrapper.TaskHasCompletedTime = new bool[count];
            wrapper.TaskSortOrders = new int[count];

            for (int i = 0; i < count; i++)
            {
                var task = data.Tasks[i];
                wrapper.TaskIds[i] = task.Id ?? "";
                wrapper.TaskTitles[i] = task.Title ?? "";
                wrapper.TaskNotes[i] = task.Notes ?? "";
                wrapper.TaskCategories[i] = (int)task.Category;
                wrapper.TaskPriorities[i] = (int)task.Priority;
                wrapper.TaskCompleted[i] = task.IsCompleted;
                wrapper.TaskCreatedTicks[i] = task.CreatedAtTicks;
                wrapper.TaskCompletedTicks[i] = task.CompletedAtTicks ?? 0;
                wrapper.TaskHasCompletedTime[i] = task.CompletedAtTicks.HasValue;
                wrapper.TaskSortOrders[i] = task.SortOrder;
            }

            return wrapper;
        }

        public ListData ToListData()
        {
            var data = new ListData
            {
                Version = this.Version,
                PlayerName = this.PlayerName,
                LastSavedTicks = this.LastSavedTicks,
                Settings = new ListSettings
                {
                    ShowCompletedTasks = this.ShowCompletedTasks,
                    ShowTimestamps = this.ShowTimestamps,
                    FilterCategoryIndex = this.FilterCategoryIndex,
                    SortBy = (SortMode)this.SortByIndex
                }
            };

            if (TaskIds != null)
            {
                for (int i = 0; i < TaskIds.Length; i++)
                {
                    var task = new TaskItem
                    {
                        Id = TaskIds[i],
                        Title = TaskTitles[i],
                        Notes = TaskNotes[i],
                        Category = (TaskCategory)TaskCategories[i],
                        Priority = (TaskPriority)TaskPriorities[i],
                        IsCompleted = TaskCompleted[i],
                        CreatedAtTicks = TaskCreatedTicks[i],
                        CompletedAtTicks = TaskHasCompletedTime[i] ? TaskCompletedTicks[i] : (long?)null,
                        SortOrder = TaskSortOrders[i]
                    };
                    data.Tasks.Add(task);
                }
            }

            return data;
        }
    }
}
