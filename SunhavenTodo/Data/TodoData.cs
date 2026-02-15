using System;
using System.Collections.Generic;

namespace SunhavenTodo.Data
{
    public enum TodoPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }

    public enum TodoCategory
    {
        General,
        Farming,
        Mining,
        Fishing,
        Combat,
        Crafting,
        Social,
        Quests,
        Collection
    }

    [Serializable]
    public class TodoItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TodoPriority Priority { get; set; }
        public TodoCategory Category { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public TodoItem()
        {
            Id = Guid.NewGuid().ToString();
            Priority = TodoPriority.Normal;
            Category = TodoCategory.General;
            CreatedAt = DateTime.Now;
            IsCompleted = false;
        }

        public TodoItem(string title, string description = "", TodoPriority priority = TodoPriority.Normal, TodoCategory category = TodoCategory.General)
            : this()
        {
            Title = title;
            Description = description;
            Priority = priority;
            Category = category;
        }
    }

    [Serializable]
    public class TodoListData
    {
        public string CharacterName { get; set; }
        public List<TodoItem> Items { get; set; }
        public DateTime LastUpdated { get; set; }

        public TodoListData()
        {
            Items = new List<TodoItem>();
            LastUpdated = DateTime.Now;
        }

        public TodoListData(string characterName) : this()
        {
            CharacterName = characterName;
        }
    }

}
