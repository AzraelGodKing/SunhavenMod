using System;
using System.Collections.Generic;
using System.Linq;
using SunHavenMuseumUtilityTracker;
using SunHavenMuseumUtilityTracker.Data;
using SunhavenTodo;
using SunhavenTodo.Data;
using Wish;

namespace SenpaisChest.Integration
{
    /// <summary>
    /// Integration between Senpai's Chest, S.M.U.T., and Sun Haven Todo.
    /// When chest scans detect items that haven't been donated to the museum,
    /// auto-creates todo tasks reminding the player to donate them.
    /// Auto-completes todos when items are donated.
    ///
    /// Only instantiated when both SMUT and SunhavenTodo are confirmed loaded.
    /// </summary>
    public class MuseumTodoIntegration
    {
        // Track which museum items already have todos (game item ID → todo item ID)
        private readonly Dictionary<int, string> _museumTodoIds = new Dictionary<int, string>();

        // Throttle: don't scan every chest cycle, only every N scans
        private int _scanCounter;
        private const int SCAN_INTERVAL = 3; // Check every 3rd chest scan

        public MuseumTodoIntegration()
        {
            // Subscribe to SMUT's donation changes to auto-complete todos
            var donationManager = SunHavenMuseumUtilityTracker.Plugin.GetDonationManager();
            if (donationManager != null)
            {
                donationManager.OnDonationsChanged += OnDonationsChanged;
            }

            Plugin.Log?.LogInfo("[MuseumTodoIntegration] Initialized - museum item todos will sync with S.M.U.T. and Todo");
        }

        /// <summary>
        /// Called after each Senpai's Chest scan cycle.
        /// Checks all chest inventories for undonated museum items.
        /// </summary>
        public void OnScanComplete()
        {
            _scanCounter++;
            if (_scanCounter < SCAN_INTERVAL)
                return;
            _scanCounter = 0;

            try
            {
                var donationManager = SunHavenMuseumUtilityTracker.Plugin.GetDonationManager();
                var todoManager = SunhavenTodo.Plugin.GetTodoManager();

                if (donationManager == null || !donationManager.IsLoaded) return;
                if (todoManager == null) return;

                var inventories = ChestManager.inventories;
                if (inventories == null || inventories.Count == 0) return;

                foreach (var inventory in inventories)
                {
                    if (inventory == null) continue;

                    var items = inventory.Items;
                    if (items == null) continue;

                    int slotCount = Math.Min(inventory.maxSlots, items.Count);
                    for (int i = 0; i < slotCount; i++)
                    {
                        var slotData = items[i];
                        if (slotData.id <= 0 || slotData.amount <= 0)
                            continue;

                        CheckMuseumItem(slotData.id, donationManager, todoManager);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[MuseumTodoIntegration] Error during scan: {ex.Message}");
            }
        }

        private void CheckMuseumItem(int gameItemId, DonationManager donationManager, TodoManager todoManager)
        {
            // Already tracking this item
            if (_museumTodoIds.ContainsKey(gameItemId))
                return;

            // Not a museum item
            var museumItem = MuseumContent.FindByGameItemId(gameItemId);
            if (museumItem == null)
                return;

            // Already donated
            if (donationManager.HasDonatedByGameId(gameItemId))
                return;

            string destinationHall = GetDestinationHallForItem(gameItemId);

            // Create a todo for this museum item
            string title = $"Donate {museumItem.Name} -> {destinationHall}";
            string description = $"Found in a chest. Needed in {destinationHall}.";

            var todoItem = new TodoItem(title, description, TodoPriority.High, TodoCategory.Collection);
            SetTodoMetadata(todoItem, gameItemId, destinationHall);
            todoManager.AddTodo(todoItem);

            _museumTodoIds[gameItemId] = todoItem.Id;

            Plugin.Log?.LogInfo($"[MuseumTodoIntegration] Created todo for museum item: {museumItem.Name} -> {destinationHall} (ID: {gameItemId})");
        }

        /// <summary>
        /// Called when SMUT reports a donation change.
        /// Auto-completes todos for items that have been donated.
        /// </summary>
        private void OnDonationsChanged()
        {
            try
            {
                var donationManager = SunHavenMuseumUtilityTracker.Plugin.GetDonationManager();
                var todoManager = SunhavenTodo.Plugin.GetTodoManager();

                if (donationManager == null || todoManager == null) return;

                var toComplete = new List<int>();

                foreach (var kvp in _museumTodoIds)
                {
                    if (donationManager.HasDonatedByGameId(kvp.Key))
                    {
                        toComplete.Add(kvp.Key);
                    }
                }

                foreach (var gameItemId in toComplete)
                {
                    if (_museumTodoIds.TryGetValue(gameItemId, out var todoId))
                    {
                        var todo = todoManager.GetAllTodos().FirstOrDefault(t => t.Id == todoId);
                        if (todo != null && !todo.IsCompleted)
                        {
                            todoManager.ToggleComplete(todoId);

                            var museumItem = MuseumContent.FindByGameItemId(gameItemId);
                            Plugin.Log?.LogInfo($"[MuseumTodoIntegration] Completed todo for donated item: {museumItem?.Name ?? gameItemId.ToString()}");
                        }
                        _museumTodoIds.Remove(gameItemId);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[MuseumTodoIntegration] Error processing donation change: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset tracking when character changes.
        /// </summary>
        public void Reset()
        {
            _museumTodoIds.Clear();
            _scanCounter = 0;
        }

        private static string GetDestinationHallForItem(int gameItemId)
        {
            var halls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var section in MuseumContent.GetAllSections())
            {
                foreach (var bundle in section.Bundles)
                {
                    foreach (var item in bundle.Items)
                    {
                        if (item.GameItemId == gameItemId)
                            halls.Add(section.Name);
                    }
                }
            }

            if (halls.Count == 0)
                return "the museum";

            return string.Join(" / ", halls);
        }

        private static void SetTodoMetadata(TodoItem todoItem, int gameItemId, string destinationHall)
        {
            if (todoItem == null) return;

            try
            {
                var todoType = todoItem.GetType();
                var iconProp = todoType.GetProperty("IconItemId");
                var destinationProp = todoType.GetProperty("MuseumDestination");

                if (iconProp != null && iconProp.CanWrite)
                    iconProp.SetValue(todoItem, gameItemId);

                if (destinationProp != null && destinationProp.CanWrite)
                    destinationProp.SetValue(todoItem, destinationHall ?? "");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[MuseumTodoIntegration] Failed to set todo metadata: {ex.Message}");
            }
        }
    }
}
