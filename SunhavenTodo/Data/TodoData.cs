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

    [Serializable]
    public class TodoListDataWrapper
    {
        public string CharacterName;
        public List<TodoItemWrapper> Items = new List<TodoItemWrapper>();
        public string LastUpdated;

        public static TodoListDataWrapper FromData(TodoListData data)
        {
            var wrapper = new TodoListDataWrapper
            {
                CharacterName = data.CharacterName,
                LastUpdated = data.LastUpdated.ToString("o")
            };

            foreach (var item in data.Items)
            {
                wrapper.Items.Add(TodoItemWrapper.FromItem(item));
            }

            return wrapper;
        }

        public TodoListData ToData()
        {
            var data = new TodoListData
            {
                CharacterName = CharacterName,
                LastUpdated = DateTime.TryParse(LastUpdated, out var dt) ? dt : DateTime.Now
            };

            foreach (var itemWrapper in Items)
            {
                data.Items.Add(itemWrapper.ToItem());
            }

            return data;
        }
    }

    [Serializable]
    public class TodoItemWrapper
    {
        public string Id;
        public string Title;
        public string Description;
        public int Priority;
        public int Category;
        public bool IsCompleted;
        public string CreatedAt;
        public string CompletedAt;

        public static TodoItemWrapper FromItem(TodoItem item)
        {
            return new TodoItemWrapper
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                Priority = (int)item.Priority,
                Category = (int)item.Category,
                IsCompleted = item.IsCompleted,
                CreatedAt = item.CreatedAt.ToString("o"),
                CompletedAt = item.CompletedAt?.ToString("o") ?? ""
            };
        }

        public TodoItem ToItem()
        {
            return new TodoItem
            {
                Id = Id,
                Title = Title,
                Description = Description,
                Priority = (TodoPriority)Priority,
                Category = (TodoCategory)Category,
                IsCompleted = IsCompleted,
                CreatedAt = DateTime.TryParse(CreatedAt, out var created) ? created : DateTime.Now,
                CompletedAt = string.IsNullOrEmpty(CompletedAt) ? null : (DateTime.TryParse(CompletedAt, out var completed) ? completed : (DateTime?)null)
            };
        }
    }
}
