using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SunhavenMods.Shared
{
    /// <summary>A todo to create through <see cref="TodoManagerReflection.AddTodo"/>.</summary>
    public sealed class TodoDraft
    {
        public string Title { get; set; }
        public string Description { get; set; } = "";
        /// <summary>Name of a <c>SunhavenTodo.Data.TodoPriority</c> value.</summary>
        public string Priority { get; set; } = "Normal";
        /// <summary>Name of a <c>SunhavenTodo.Data.TodoCategory</c> value.</summary>
        public string Category { get; set; } = "General";
        /// <summary>Game item id for the row icon; values &lt;= 0 keep Todo's default.</summary>
        public int IconItemId { get; set; }
        public string MuseumDestination { get; set; }
    }

    /// <summary>Read-only copy of a todo's fields, detached from Todo's types.</summary>
    public sealed class TodoSnapshot
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public bool IsCompleted { get; }
        public string Priority { get; }

        public TodoSnapshot(string id, string title, string description, bool isCompleted, string priority)
        {
            Id = id;
            Title = title ?? "";
            Description = description ?? "";
            IsCompleted = isCompleted;
            Priority = priority ?? "";
        }
    }

    /// <summary>
    /// The Sun Haven Todo API that suite mods use, bound by reflection to a live
    /// <c>SunhavenTodo.Data.TodoManager</c> (AZR-255). Every member name the suite depends on is
    /// resolved here, and <c>SunhavenTodo/Tests</c> binds it to the real TodoManager so a rename
    /// fails CI instead of silently disabling integrations at runtime.
    /// No BepInEx or Unity dependencies.
    /// </summary>
    public sealed class TodoManagerReflection
    {
        public const string TodoItemTypeName = "SunhavenTodo.Data.TodoItem";
        public const string TodoPriorityTypeName = "SunhavenTodo.Data.TodoPriority";
        public const string TodoCategoryTypeName = "SunhavenTodo.Data.TodoCategory";

        private readonly Type _itemType;
        private readonly Type _priorityType;
        private readonly Type _categoryType;
        private readonly MethodInfo _addTodo;
        private readonly MethodInfo _removeTodo;
        private readonly MethodInfo _toggleComplete;
        private readonly MethodInfo _getTodoById;
        private readonly MethodInfo _getAllTodos;
        private readonly MethodInfo _getActiveTodos;
        private readonly MethodInfo _getData;
        private readonly PropertyInfo _currentCharacter;
        private readonly PropertyInfo _id;
        private readonly PropertyInfo _title;
        private readonly PropertyInfo _description;
        private readonly PropertyInfo _isCompleted;
        private readonly PropertyInfo _priority;
        private readonly PropertyInfo _category;
        private readonly PropertyInfo _iconItemId;
        private readonly PropertyInfo _museumDestination;

        public object Manager { get; }

        private TodoManagerReflection(object manager, Type itemType, Type priorityType, Type categoryType)
        {
            Manager = manager;
            _itemType = itemType;
            _priorityType = priorityType;
            _categoryType = categoryType;

            Type managerType = manager.GetType();
            _addTodo = Method(managerType, "AddTodo", itemType);
            _removeTodo = Method(managerType, "RemoveTodo", typeof(string));
            _toggleComplete = Method(managerType, "ToggleComplete", typeof(string));
            _getTodoById = Method(managerType, "GetTodoById", typeof(string));
            _getAllTodos = Method(managerType, "GetAllTodos");
            _getActiveTodos = Method(managerType, "GetActiveTodos");
            _getData = Method(managerType, "GetData");
            _currentCharacter = managerType.GetProperty("CurrentCharacter", BindingFlags.Public | BindingFlags.Instance);

            _id = Property(itemType, "Id");
            _title = Property(itemType, "Title");
            _description = Property(itemType, "Description");
            _isCompleted = Property(itemType, "IsCompleted");
            _priority = Property(itemType, "Priority");
            _category = Property(itemType, "Category");
            _iconItemId = Property(itemType, "IconItemId");
            _museumDestination = Property(itemType, "MuseumDestination");
        }

        /// <summary>
        /// Binds to <paramref name="manager"/>. Returns null and sets <paramref name="error"/>
        /// when a type or member the suite needs is missing.
        /// </summary>
        public static TodoManagerReflection TryBind(object manager, out string error)
        {
            error = null;
            if (manager == null)
            {
                error = "TodoManager is null";
                return null;
            }

            Assembly asm = manager.GetType().Assembly;
            Type itemType = asm.GetType(TodoItemTypeName);
            Type priorityType = asm.GetType(TodoPriorityTypeName);
            Type categoryType = asm.GetType(TodoCategoryTypeName);
            if (itemType == null || priorityType == null || categoryType == null)
            {
                error = "Todo data types not found";
                return null;
            }

            var bound = new TodoManagerReflection(manager, itemType, priorityType, categoryType);
            string missing = bound.FirstMissingMember();
            if (missing != null)
            {
                error = "Todo member not found: " + missing;
                return null;
            }

            return bound;
        }

        private string FirstMissingMember()
        {
            if (_addTodo == null) return "TodoManager.AddTodo(TodoItem)";
            if (_removeTodo == null) return "TodoManager.RemoveTodo(string)";
            if (_toggleComplete == null) return "TodoManager.ToggleComplete(string)";
            if (_getTodoById == null) return "TodoManager.GetTodoById(string)";
            if (_getAllTodos == null) return "TodoManager.GetAllTodos()";
            if (_getActiveTodos == null) return "TodoManager.GetActiveTodos()";
            if (_getData == null) return "TodoManager.GetData()";
            if (_currentCharacter == null) return "TodoManager.CurrentCharacter";
            if (_id == null) return "TodoItem.Id";
            if (_title == null) return "TodoItem.Title";
            if (_description == null) return "TodoItem.Description";
            if (_isCompleted == null) return "TodoItem.IsCompleted";
            if (_priority == null) return "TodoItem.Priority";
            if (_category == null) return "TodoItem.Category";
            if (_iconItemId == null) return "TodoItem.IconItemId";
            if (_museumDestination == null) return "TodoItem.MuseumDestination";
            if (_itemType.GetConstructor(Type.EmptyTypes) == null) return "TodoItem()";
            return null;
        }

        /// <summary>Character whose list is loaded, or null.</summary>
        public string CurrentCharacter => _currentCharacter.GetValue(Manager, null) as string;

        /// <summary>True when a character's list is loaded; Todo drops adds until then.</summary>
        public bool HasData => !string.IsNullOrEmpty(CurrentCharacter) && _getData.Invoke(Manager, null) != null;

        /// <summary>Adds a todo and returns its id. Throws on an unknown priority or category name.</summary>
        public string AddTodo(TodoDraft draft)
        {
            if (draft == null || string.IsNullOrWhiteSpace(draft.Title))
                throw new ArgumentException("Todo title is required", nameof(draft));

            object item = Activator.CreateInstance(_itemType);
            _title.SetValue(item, draft.Title, null);
            _description.SetValue(item, draft.Description ?? "", null);
            _priority.SetValue(item, Enum.Parse(_priorityType, draft.Priority ?? "Normal"), null);
            _category.SetValue(item, Enum.Parse(_categoryType, draft.Category ?? "General"), null);
            if (draft.IconItemId > 0)
                _iconItemId.SetValue(item, draft.IconItemId, null);
            if (draft.MuseumDestination != null)
                _museumDestination.SetValue(item, draft.MuseumDestination, null);

            _addTodo.Invoke(Manager, new[] { item });
            return _id.GetValue(item, null) as string;
        }

        /// <summary>Removes a todo. Returns false when no todo has that id.</summary>
        public bool RemoveTodo(string id)
        {
            if (string.IsNullOrEmpty(id) || GetItem(id) == null)
                return false;
            _removeTodo.Invoke(Manager, new object[] { id });
            return true;
        }

        /// <summary>Completion state, or null when no todo has that id.</summary>
        public bool? IsCompleted(string id)
        {
            object item = GetItem(id);
            if (item == null)
                return null;
            return _isCompleted.GetValue(item, null) is bool b && b;
        }

        /// <summary>Sets completion. Returns true only when the state changed.</summary>
        public bool SetCompleted(string id, bool completed)
        {
            bool? current = IsCompleted(id);
            if (current == null || current.Value == completed)
                return false;
            _toggleComplete.Invoke(Manager, new object[] { id });
            return true;
        }

        public TodoSnapshot GetById(string id)
        {
            object item = GetItem(id);
            return item == null ? null : Snapshot(item);
        }

        /// <summary>Copies every todo, or only active ones. Safe to mutate the list while iterating the result.</summary>
        public List<TodoSnapshot> GetTodos(bool activeOnly)
        {
            var result = new List<TodoSnapshot>();
            MethodInfo source = activeOnly ? _getActiveTodos : _getAllTodos;
            if (source.Invoke(Manager, null) is IEnumerable items)
            {
                foreach (object item in items)
                {
                    if (item != null)
                        result.Add(Snapshot(item));
                }
            }
            return result;
        }

        private object GetItem(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            return _getTodoById.Invoke(Manager, new object[] { id });
        }

        private TodoSnapshot Snapshot(object item)
        {
            return new TodoSnapshot(
                _id.GetValue(item, null) as string,
                _title.GetValue(item, null) as string,
                _description.GetValue(item, null) as string,
                _isCompleted.GetValue(item, null) is bool b && b,
                _priority.GetValue(item, null)?.ToString());
        }

        private static MethodInfo Method(Type type, string name, params Type[] parameters)
        {
            return type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, parameters, null);
        }

        private static PropertyInfo Property(Type type, string name)
        {
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        }
    }
}
