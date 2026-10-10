using System;
using System.Collections.Generic;
using System.Linq;
using BirthdayReminder.Data;
using SunhavenMods.Shared;

namespace BirthdayReminder.Integration
{
    /// <summary>
    /// Integration with SunhavenTodo mod, through <see cref="TodoSoftClient"/>.
    /// Auto-creates birthday gift todos when birthdays are detected,
    /// and auto-completes them when gifts are given.
    ///
    /// Only instantiated when SunhavenTodo is loaded.
    /// </summary>
    public class TodoIntegration
    {
        private readonly BirthdayManager _birthdayManager;

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
                var todo = TodoSoftClient.TryGetManager(Plugin.Log);
                if (todo == null)
                {
                    Plugin.Log?.LogWarning($"[TodoIntegration] OnGiftGiven: TodoManager is null, cannot complete todo for {npcName}");
                    return;
                }

                if (!TryResolveTodoId(npcName, out var todoId))
                {
                    Plugin.Log?.LogInfo($"[TodoIntegration] OnGiftGiven: No tracked todo for {npcName}");
                    return;
                }

                bool? completed = todo.IsCompleted(todoId);
                if (completed == null)
                {
                    Plugin.Log?.LogWarning($"[TodoIntegration] OnGiftGiven: Todo {todoId} not found for {npcName}");
                    _birthdayTodoIds.Remove(npcName);
                    return;
                }

                if (todo.SetCompleted(todoId, true))
                    Plugin.Log?.LogInfo($"[TodoIntegration] Completed birthday todo for {npcName} (direct path)");
                else
                    Plugin.Log?.LogInfo($"[TodoIntegration] Todo for {npcName} already completed");
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
                var todo = TodoSoftClient.TryGetManager(Plugin.Log);
                if (todo == null)
                {
                    Plugin.Log?.LogDebug("[TodoIntegration] OnBirthdaysUpdated: TodoManager is null");
                    return;
                }

                var todaysBirthdays = _birthdayManager.TodaysBirthdays;

                // If no birthdays today, clean up any existing birthday todos
                if (todaysBirthdays == null || todaysBirthdays.Count == 0)
                {
                    CleanupBirthdayTodos(todo);
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
                    RemoveBirthdayTodo(todo, npc);
                }

                // Process each birthday
                foreach (var birthday in todaysBirthdays)
                {
                    if (birthday.HasBeenGifted)
                    {
                        // Gift was given - complete the todo if it exists and isn't already completed
                        CompleteBirthdayTodo(todo, birthday.NPCName);
                    }
                    else
                    {
                        // No gift yet - ensure a todo exists
                        EnsureBirthdayTodo(todo, birthday);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[TodoIntegration] Error syncing birthdays: {ex.Message}");
            }
        }

        private void EnsureBirthdayTodo(TodoManagerReflection todo, BirthdayDisplayInfo birthday)
        {
            string todoKey = GiftPatches.NormalizeNpcName(birthday.NPCName);
            if (string.IsNullOrEmpty(todoKey))
                todoKey = birthday.NPCName;

            // Don't create duplicate
            if (_birthdayTodoIds.ContainsKey(todoKey)) return;

            string description = !string.IsNullOrEmpty(birthday.GiftHint)
                ? birthday.GiftHint
                : "It's their birthday today!";

            string todoId = todo.AddTodo(new TodoDraft
            {
                Title = $"Give {birthday.NPCName} a birthday gift!",
                Description = description,
                Priority = "High",
                Category = "Social"
            });

            _birthdayTodoIds[todoKey] = todoId;

            Plugin.Log?.LogInfo($"[TodoIntegration] Added birthday todo for {birthday.NPCName} (id: {todoId})");
        }

        private void CompleteBirthdayTodo(TodoManagerReflection todo, string npcName)
        {
            if (!TryResolveTodoId(npcName, out var todoId)) return;

            if (todo.SetCompleted(todoId, true))
                Plugin.Log?.LogInfo($"[TodoIntegration] Completed birthday todo for {npcName} (event path)");
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

        private void RemoveBirthdayTodo(TodoManagerReflection todo, string npcName)
        {
            if (!TryResolveTodoId(npcName, out var todoId))
                return;

            todo.RemoveTodo(todoId);
            string normalized = GiftPatches.NormalizeNpcName(npcName);
            _birthdayTodoIds.Remove(normalized);
            _birthdayTodoIds.Remove(npcName);
        }

        private void CleanupBirthdayTodos(TodoManagerReflection todo)
        {
            foreach (var kvp in _birthdayTodoIds.ToList())
            {
                todo.RemoveTodo(kvp.Value);
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
                var todo = TodoSoftClient.TryGetManager(Plugin.Log);
                if (todo != null && _birthdayTodoIds.Count > 0)
                    CleanupBirthdayTodos(todo);
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
