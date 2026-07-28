using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BirthdayReminder.Data;
using SunhavenTodo;
using SunhavenTodo.Data;

namespace BirthdayReminder.Integration
{
    /// <summary>
    /// Integration with SunhavenTodo mod.
    /// Auto-creates birthday gift todos when birthdays are detected,
    /// and auto-completes them when gifts are given.
    ///
    /// This class is only instantiated when SunhavenTodo is confirmed loaded,
    /// so the JIT compiler won't resolve SunhavenTodo types unless the mod is present.
    /// </summary>
    public class TodoIntegration
    {
        private readonly BirthdayManager _birthdayManager;
        private static MethodInfo _todoGetByIdMethod;
        private static bool _todoGetByIdLookedUp;

        // Track which NPC birthday todos we've created (NPC name → todo item ID)
        private readonly Dictionary<string, string> _birthdayTodoIds = new Dictionary<string, string>();

        public TodoIntegration(BirthdayManager birthdayManager)
        {
            _birthdayManager = birthdayManager;
            _birthdayManager.OnBirthdaysUpdated += OnBirthdaysUpdated;

            Plugin.Log?.LogInfo("[TodoIntegration] Initialized - birthday todos will sync with Sun Haven Todo");
        }

        /// <summary>
        /// Called directly when a gift is given to an NPC on their birthday.
        /// This is a more direct path than waiting for OnBirthdaysUpdated.
        /// </summary>
        public void OnGiftGiven(string npcName)
        {
            try
            {
                var todoManager = SunhavenTodo.Plugin.GetTodoManager();
                if (todoManager == null)
                {
                    Plugin.Log?.LogWarning($"[TodoIntegration] OnGiftGiven: TodoManager is null, cannot complete todo for {npcName}");
                    return;
                }

                if (!TryResolveTodoId(npcName, out var todoId))
                {
                    Plugin.Log?.LogInfo($"[TodoIntegration] OnGiftGiven: No tracked todo for {npcName}");
                    return;
                }

                var todo = FindTodoById(todoManager, todoId);
                if (todo == null)
                {
                    Plugin.Log?.LogWarning($"[TodoIntegration] OnGiftGiven: Todo {todoId} not found for {npcName}");
                    _birthdayTodoIds.Remove(npcName);
                    return;
                }

                if (!todo.IsCompleted)
                {
                    todoManager.ToggleComplete(todoId);
                    Plugin.Log?.LogInfo($"[TodoIntegration] Completed birthday todo for {npcName} (direct path)");
                }
                else
                {
                    Plugin.Log?.LogInfo($"[TodoIntegration] Todo for {npcName} already completed");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[TodoIntegration] Error completing todo on gift: {ex.Message}");
            }
        }

        private void OnBirthdaysUpdated()
        {
            try
            {
                var todoManager = SunhavenTodo.Plugin.GetTodoManager();
                if (todoManager == null)
                {
                    Plugin.Log?.LogDebug("[TodoIntegration] OnBirthdaysUpdated: TodoManager is null");
                    return;
                }

                var todaysBirthdays = _birthdayManager.TodaysBirthdays;

                // If no birthdays today, clean up any existing birthday todos
                if (todaysBirthdays == null || todaysBirthdays.Count == 0)
                {
                    CleanupBirthdayTodos(todoManager);
                    return;
                }

                // Build set of current birthday NPC names (normalized keys)
                var currentNPCs = new HashSet<string>(
                    todaysBirthdays.Select(b => GiftPatches.NormalizeNpcName(b.NPCName)),
                    StringComparer.OrdinalIgnoreCase);

                // Remove todos for NPCs no longer in the birthday list
                var toRemove = _birthdayTodoIds.Keys.Where(k => !currentNPCs.Contains(k)).ToList();
                foreach (var npc in toRemove)
                {
                    RemoveBirthdayTodo(todoManager, npc);
                }

                // Process each birthday
                foreach (var birthday in todaysBirthdays)
                {
                    if (birthday.HasBeenGifted)
                    {
                        // Gift was given - complete the todo if it exists and isn't already completed
                        CompleteBirthdayTodo(todoManager, birthday.NPCName);
                    }
                    else
                    {
                        // No gift yet - ensure a todo exists
                        EnsureBirthdayTodo(todoManager, birthday);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[TodoIntegration] Error syncing birthdays: {ex.Message}");
            }
        }

        private void EnsureBirthdayTodo(TodoManager todoManager, BirthdayDisplayInfo birthday)
        {
            string todoKey = GiftPatches.NormalizeNpcName(birthday.NPCName);
            if (string.IsNullOrEmpty(todoKey))
                todoKey = birthday.NPCName;

            // Don't create duplicate
            if (_birthdayTodoIds.ContainsKey(todoKey)) return;

            string title = $"Give {birthday.NPCName} a birthday gift!";
            string description = !string.IsNullOrEmpty(birthday.GiftHint)
                ? birthday.GiftHint
                : "It's their birthday today!";

            var todoItem = new TodoItem(title, description, TodoPriority.High, TodoCategory.Social);
            todoManager.AddTodo(todoItem);

            _birthdayTodoIds[todoKey] = todoItem.Id;

            Plugin.Log?.LogInfo($"[TodoIntegration] Added birthday todo for {birthday.NPCName} (id: {todoItem.Id})");
        }

        private void CompleteBirthdayTodo(TodoManager todoManager, string npcName)
        {
            if (!TryResolveTodoId(npcName, out var todoId)) return;

            var todo = FindTodoById(todoManager, todoId);
            if (todo != null && !todo.IsCompleted)
            {
                todoManager.ToggleComplete(todoId);
                Plugin.Log?.LogInfo($"[TodoIntegration] Completed birthday todo for {npcName} (event path)");
            }
        }

        private static TodoItem FindTodoById(TodoManager todoManager, string todoId)
        {
            if (todoManager == null || string.IsNullOrEmpty(todoId))
                return null;

            // Prefer O(1) TodoManager.GetTodoById when available.
            if (!_todoGetByIdLookedUp)
            {
                _todoGetByIdMethod = typeof(TodoManager).GetMethod("GetTodoById", BindingFlags.Public | BindingFlags.Instance);
                _todoGetByIdLookedUp = true;
            }

            if (_todoGetByIdMethod != null)
            {
                try
                {
                    return _todoGetByIdMethod.Invoke(todoManager, new object[] { todoId }) as TodoItem;
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogDebug($"[TodoIntegration] GetTodoById reflection fallback used: {ex.Message}");
                    // Fall back to linear scan if reflective call fails.
                }
            }

            return todoManager.GetAllTodos().FirstOrDefault(t => t.Id == todoId);
        }

        private bool TryResolveTodoId(string npcName, out string todoId)
        {
            todoId = null;
            if (string.IsNullOrEmpty(npcName))
                return false;

            if (_birthdayTodoIds.TryGetValue(npcName, out todoId))
                return true;

            string normalized = GiftPatches.NormalizeNpcName(npcName);
            if (!string.IsNullOrEmpty(normalized) && _birthdayTodoIds.TryGetValue(normalized, out todoId))
                return true;

            foreach (var kvp in _birthdayTodoIds)
            {
                if (string.Equals(GiftPatches.NormalizeNpcName(kvp.Key), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    todoId = kvp.Value;
                    return true;
                }
            }

            return false;
        }

        private void RemoveBirthdayTodo(TodoManager todoManager, string npcName)
        {
            if (!TryResolveTodoId(npcName, out var todoId))
                return;

            todoManager.RemoveTodo(todoId);
            string normalized = GiftPatches.NormalizeNpcName(npcName);
            _birthdayTodoIds.Remove(normalized);
            _birthdayTodoIds.Remove(npcName);
        }

        private void CleanupBirthdayTodos(TodoManager todoManager)
        {
            foreach (var kvp in _birthdayTodoIds.ToList())
            {
                todoManager.RemoveTodo(kvp.Value);
            }
            _birthdayTodoIds.Clear();
        }

        /// <summary>
        /// Reset tracking when character changes or new day starts.
        /// Called from Plugin.OnCharacterChanged.
        /// Removes any tracked birthday todos so the next sync does not orphan/duplicate them.
        /// </summary>
        public void Reset()
        {
            try
            {
                var todoManager = SunhavenTodo.Plugin.GetTodoManager();
                if (todoManager != null && _birthdayTodoIds.Count > 0)
                    CleanupBirthdayTodos(todoManager);
                else
                    _birthdayTodoIds.Clear();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[TodoIntegration] Reset cleanup failed: {ex.Message}");
                _birthdayTodoIds.Clear();
            }
        }

        public void Dispose()
        {
            if (_birthdayManager != null)
                _birthdayManager.OnBirthdaysUpdated -= OnBirthdaysUpdated;
        }
    }
}
