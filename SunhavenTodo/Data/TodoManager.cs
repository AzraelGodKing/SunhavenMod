using System;
using System.Collections.Generic;
using System.Linq;

namespace SunhavenTodo.Data
{
    public class TodoManager
    {
        private TodoListData _todoData;
        private string _currentCharacter;
        private bool _isDirty;

        public event Action OnTodosChanged;
        public event Action OnDataLoaded;

        public bool IsDirty => _isDirty;
        public string CurrentCharacter => _currentCharacter;

        public void LoadForCharacter(string characterName, TodoListData data)
        {
            _currentCharacter = characterName;
            _todoData = data ?? new TodoListData(characterName);
            _isDirty = false;
            OnDataLoaded?.Invoke();
        }

        public void ClearData()
        {
            _todoData = null;
            _currentCharacter = null;
            _isDirty = false;
        }

        public TodoListData GetData()
        {
            return _todoData;
        }

        public void MarkClean()
        {
            _isDirty = false;
        }

        // CRUD Operations
        public void AddTodo(TodoItem item)
        {
            if (_todoData == null) return;

            _todoData.Items.Add(item);
            _todoData.LastUpdated = DateTime.Now;
            _isDirty = true;
            OnTodosChanged?.Invoke();
        }

        public void AddTodo(string title, string description = "", TodoPriority priority = TodoPriority.Normal, TodoCategory category = TodoCategory.General)
        {
            var item = new TodoItem(title, description, priority, category);
            AddTodo(item);
        }

        public void UpdateTodo(TodoItem item)
        {
            if (_todoData == null) return;

            var index = _todoData.Items.FindIndex(i => i.Id == item.Id);
            if (index >= 0)
            {
                _todoData.Items[index] = item;
                _todoData.LastUpdated = DateTime.Now;
                _isDirty = true;
                OnTodosChanged?.Invoke();
            }
        }

        public void RemoveTodo(string itemId)
        {
            if (_todoData == null) return;

            var removed = _todoData.Items.RemoveAll(i => i.Id == itemId) > 0;
            if (removed)
            {
                _todoData.LastUpdated = DateTime.Now;
                _isDirty = true;
                OnTodosChanged?.Invoke();
            }
        }

        public void ToggleComplete(string itemId)
        {
            if (_todoData == null) return;

            var item = _todoData.Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                item.IsCompleted = !item.IsCompleted;
                item.CompletedAt = item.IsCompleted ? DateTime.Now : (DateTime?)null;
                _todoData.LastUpdated = DateTime.Now;
                _isDirty = true;
                OnTodosChanged?.Invoke();
            }
        }

        public void ClearCompleted()
        {
            if (_todoData == null) return;

            var removed = _todoData.Items.RemoveAll(i => i.IsCompleted) > 0;
            if (removed)
            {
                _todoData.LastUpdated = DateTime.Now;
                _isDirty = true;
                OnTodosChanged?.Invoke();
            }
        }

        // Query Operations
        public IEnumerable<TodoItem> GetAllTodos()
        {
            return _todoData?.Items ?? Enumerable.Empty<TodoItem>();
        }

        public IEnumerable<TodoItem> GetTodosByCategory(TodoCategory category)
        {
            return GetAllTodos().Where(i => i.Category == category);
        }

        public IEnumerable<TodoItem> GetTodosByPriority(TodoPriority priority)
        {
            return GetAllTodos().Where(i => i.Priority == priority);
        }

        public IEnumerable<TodoItem> GetActiveTodos()
        {
            return GetAllTodos().Where(i => !i.IsCompleted);
        }

        public IEnumerable<TodoItem> GetCompletedTodos()
        {
            return GetAllTodos().Where(i => i.IsCompleted);
        }

        public IEnumerable<TodoItem> SearchTodos(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return GetAllTodos();

            var lowerQuery = query.ToLower();
            return GetAllTodos().Where(i =>
                i.Title.ToLower().Contains(lowerQuery) ||
                (i.Description != null && i.Description.ToLower().Contains(lowerQuery)));
        }

        // Statistics
        public (int total, int completed, int active) GetStats()
        {
            var todos = GetAllTodos().ToList();
            var completed = todos.Count(i => i.IsCompleted);
            return (todos.Count, completed, todos.Count - completed);
        }

        public Dictionary<TodoCategory, int> GetCountsByCategory()
        {
            var counts = new Dictionary<TodoCategory, int>();
            foreach (TodoCategory cat in Enum.GetValues(typeof(TodoCategory)))
            {
                counts[cat] = GetTodosByCategory(cat).Count(i => !i.IsCompleted);
            }
            return counts;
        }

        public float GetCompletionPercent()
        {
            var (total, completed, _) = GetStats();
            return total == 0 ? 0 : (completed / (float)total) * 100f;
        }
    }
}
