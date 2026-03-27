using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HavenDevTools.API;
using HavenDevTools.Config;
using HavenDevTools.Services;
using SunhavenMods.Shared;
using UnityEngine;
using Wish;

namespace HavenDevTools.Integrations
{
    /// <summary>
    /// Azrael's Mods section - direct integration for SenpaisChest, TheVault, S.M.U.T.,
    /// HavensBirthright, Birthday Reminder, Sunhaven Todo, and Haven's Almanac.
    /// </summary>
    public static class AzraelsModsPanel
    {
        private static int _selectedSubTab;
        private static Vector2 _scrollPosition;
        private static Vector2 _bundleScrollPosition;
        private static Vector2 _raceScrollPosition;
        private static int _selectedSectionIndex;
        private static int _selectedBundleIndex;
        private static int _selectedRaceIndex;

        // Cached reflection to avoid per-frame lag (FindType/GetMethod scan assemblies)
        private static Type _cachedSenpaisChestPlugin;
        private static MethodInfo _cachedSenpaisChestGetManager;
        private static Type _cachedBirthdayReminderPlugin;
        private static MethodInfo _cachedBirthdayReminderGetManager;
        private static Type _cachedSunhavenTodoPlugin;
        private static MethodInfo _cachedSunhavenTodoGetManager;
        private static Type _cachedHavensAlmanacPlugin;
        private static MethodInfo _cachedHavensAlmanacGetAggregator;

        private static readonly string[] _subTabNames = new[]
        {
            "Senpai's Chest", "The Vault", "S.M.U.T.", "Birthright",
            "Birthday", "Todo", "Almanac"
        };

        public static void Draw(GUIStyle boxStyle, GUIStyle buttonStyle, GUIStyle labelStyle, GUIStyle sectionHeaderStyle)
        {
            var installedTabs = new List<string>();
            var installedIndices = new List<int>();
            int idx = 0;
            if (Plugin.HasSenpaisChest) { installedTabs.Add("Senpai's Chest"); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasTheVault) { installedTabs.Add("The Vault"); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasSMUT) { installedTabs.Add("S.M.U.T."); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasHavensBirthright) { installedTabs.Add("Birthright"); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasBirthdayReminder) { installedTabs.Add("Birthday"); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasSunhavenTodo) { installedTabs.Add("Todo"); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasHavensAlmanac) { installedTabs.Add("Almanac"); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasTrinketFortune) { installedTabs.Add("Trinket Fortune"); installedIndices.Add(idx); }

            if (installedTabs.Count == 0)
            {
                GUILayout.Label("No Azrael's mods detected. Install SenpaisChest, TheVault, S.M.U.T., Birthright, Birthday Reminder, Todo, Almanac, or Trinket Fortune.", labelStyle);
                return;
            }

            // Map selected sub-tab index to actual mod index
            int actualIndex = _selectedSubTab >= installedIndices.Count ? 0 : installedIndices[_selectedSubTab];
            _selectedSubTab = Mathf.Clamp(_selectedSubTab, 0, installedTabs.Count - 1);
            actualIndex = installedIndices[_selectedSubTab];

            _selectedSubTab = GUILayout.Toolbar(_selectedSubTab, installedTabs.ToArray(), buttonStyle);
            GUILayout.Space(8);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            switch (actualIndex)
            {
                case 0: DrawSenpaisChest(boxStyle, buttonStyle, labelStyle, sectionHeaderStyle); break;
                case 1: DrawTheVault(boxStyle, buttonStyle, labelStyle, sectionHeaderStyle); break;
                case 2: DrawSMUT(boxStyle, buttonStyle, labelStyle, sectionHeaderStyle); break;
                case 3: DrawBirthright(boxStyle, buttonStyle, labelStyle, sectionHeaderStyle); break;
                case 4: DrawBirthdayReminder(boxStyle, buttonStyle, labelStyle, sectionHeaderStyle); break;
                case 5: DrawTodo(boxStyle, buttonStyle, labelStyle, sectionHeaderStyle); break;
                case 6: DrawAlmanac(boxStyle, buttonStyle, labelStyle, sectionHeaderStyle); break;
                case 7: DrawTrinketFortune(boxStyle, buttonStyle, labelStyle, sectionHeaderStyle); break;
            }

            GUILayout.EndScrollView();
        }

        private static void DrawSenpaisChest(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label("Senpai's Chest", sectionHeader);

            try
            {
                if (_cachedSenpaisChestPlugin == null)
                    _cachedSenpaisChestPlugin = ReflectionHelper.FindType("Plugin", "SenpaisChest") ?? ReflectionHelper.FindType("Plugin", "SenpaiChest");
                if (_cachedSenpaisChestPlugin == null) { GUILayout.Label("SenpaisChest.Plugin not found", label); GUILayout.EndVertical(); return; }

                if (_cachedSenpaisChestGetManager == null)
                    _cachedSenpaisChestGetManager = _cachedSenpaisChestPlugin.GetMethod("GetManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (_cachedSenpaisChestGetManager == null) { GUILayout.Label("GetManager not found", label); GUILayout.EndVertical(); return; }

                var manager = _cachedSenpaisChestGetManager.Invoke(null, null);
                if (manager == null) { GUILayout.Label("Manager not loaded", label); GUILayout.EndVertical(); return; }

                // Smart chests
                var getSaveData = manager.GetType().GetMethod("GetSaveData");
                if (getSaveData != null)
                {
                    var saveData = getSaveData.Invoke(manager, null);
                    if (saveData != null)
                    {
                        var chestsProp = saveData.GetType().GetProperty("Chests");
                        if (chestsProp != null)
                        {
                            var chests = chestsProp.GetValue(saveData) as System.Collections.IList;
                            if (chests != null)
                            {
                                GUILayout.Label($"Smart Chests: {chests.Count}", label);
                                foreach (var chest in chests)
                                {
                                    var nameProp = chest?.GetType().GetProperty("ChestName");
                                    var idProp = chest?.GetType().GetProperty("ChestId");
                                    var enabledProp = chest?.GetType().GetProperty("IsEnabled");
                                    string name = nameProp?.GetValue(chest)?.ToString() ?? "?";
                                    string id = idProp?.GetValue(chest)?.ToString() ?? "?";
                                    bool enabled = (bool)(enabledProp?.GetValue(chest) ?? false);
                                    GUILayout.Label($"  {(enabled ? "[ON]" : "[OFF]")} {name} ({id})", label);
                                }
                            }
                        }
                    }
                }

                // Groups
                var getGroups = manager.GetType().GetMethod("GetGroups");
                if (getGroups != null)
                {
                    var groups = getGroups.Invoke(manager, null) as System.Collections.IList;
                    if (groups != null && groups.Count > 0)
                    {
                        GUILayout.Space(5);
                        GUILayout.Label($"Groups: {groups.Count}", label);
                        foreach (var g in groups)
                        {
                            var nameProp = g?.GetType().GetProperty("Name");
                            string gn = nameProp?.GetValue(g)?.ToString() ?? "?";
                            GUILayout.Label($"  - {gn}", label);
                        }
                    }
                }

                GUILayout.Space(8);
                if (GUILayout.Button("Trigger Manual Scan", button))
                {
                    var executeScan = manager.GetType().GetMethod("ExecuteScan", new[] { typeof(int), typeof(bool) });
                    executeScan?.Invoke(manager, new object[] { 999, false });
                    Plugin.Log?.LogInfo("[AzraelsMods] Triggered SenpaisChest manual scan");
                }
            }
            catch (Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", label);
            }

            GUILayout.EndVertical();
        }

        private static void DrawTheVault(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label("The Vault", sectionHeader);

            var tracker = Plugin.GetCurrencyTracker();
            if (tracker == null) { GUILayout.Label("Currency tracker not available", label); GUILayout.EndVertical(); return; }

            var summary = tracker.GetSummary();
            GUILayout.Label("Vault Currencies:", label);
            if (summary.VaultCurrencies.Count == 0)
                GUILayout.Label("  (vault empty or not loaded)", label);
            else
            {
                foreach (var kvp in summary.VaultCurrencies)
                    GUILayout.Label($"  {kvp.Key}: {kvp.Value}", label);
            }

            GUILayout.Space(8);
            if (ModConfig.TheVaultFullVaultInspector != null)
            {
                bool inspector = ModConfig.TheVaultFullVaultInspector.Value;
                bool newInspector = GUILayout.Toggle(inspector, " Full vault inspector (zeros + Debug tab + full HUD)", label);
                if (newInspector != inspector)
                    ModConfig.TheVaultFullVaultInspector.Value = newInspector;
                GUILayout.Label("Moved from The Vault settings; persists in com.azraelgodking.havendevtools.cfg ([The Vault] section).", label);
            }

            GUILayout.EndVertical();
        }

        private static void DrawSMUT(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label("S.M.U.T. - Museum Bundle Inspector", sectionHeader);

            var inspector = Plugin.GetBundleInspector();
            if (inspector == null) { GUILayout.Label("Bundle inspector not available", label); GUILayout.EndVertical(); return; }

            var stats = inspector.GetDonationStats();
            if (stats.IsLoaded)
            {
                GUILayout.Label($"Character: {stats.CharacterName}", label);
                GUILayout.Label($"Progress: {stats.TotalDonated}/{stats.TotalItems} ({stats.CompletionPercent:F1}%)", label);
            }

            var sections = inspector.GetAllSections();
            if (sections.Count == 0) { GUILayout.EndVertical(); return; }

            string[] sectionNames = sections.Select(s => s.Name).ToArray();
            if (_selectedSectionIndex >= sections.Count) _selectedSectionIndex = 0;
            GUILayout.Label("Section:", label);
            _selectedSectionIndex = GUILayout.SelectionGrid(_selectedSectionIndex, sectionNames, 3, button);

            var selectedSection = sections[_selectedSectionIndex];
            if (selectedSection.Bundles.Count > 0)
            {
                string[] bundleNames = selectedSection.Bundles.Select(b => b.Name).ToArray();
                if (_selectedBundleIndex >= selectedSection.Bundles.Count) _selectedBundleIndex = 0;
                GUILayout.Label("Bundle:", label);
                _selectedBundleIndex = GUILayout.SelectionGrid(_selectedBundleIndex, bundleNames, 2, button);

                var selectedBundle = selectedSection.Bundles[_selectedBundleIndex];
                GUILayout.Label($"Items in {selectedBundle.Name}:", sectionHeader);
                _bundleScrollPosition = GUILayout.BeginScrollView(_bundleScrollPosition, GUILayout.Height(120));
                foreach (var item in selectedBundle.Items)
                {
                    bool donated = inspector.HasDonated(item.Id);
                    string status = donated ? "[X]" : "[ ]";
                    string qty = item.Quantity > 1 ? $" x{item.Quantity}" : "";
                    bool canSpawn = item.GameItemId > 0;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{status} {item.Name} (ID: {item.GameItemId})", label, GUILayout.Width(280));
                    GUI.enabled = canSpawn;
                    if (GUILayout.Button(canSpawn ? $"Spawn{qty}" : "—", button, GUILayout.Width(70)))
                    {
                        if (canSpawn) Plugin.GetItemInspector()?.SpawnItem(item.GameItemId, item.Quantity);
                    }
                    GUI.enabled = true;
                    if (!canSpawn) GUILayout.Label("(Unity)", label, GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

        private static void DrawBirthright(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label("Haven's Birthright - Race Modifier Tracker", sectionHeader);

            var tracker = Plugin.GetRaceModifierTracker();
            if (tracker == null) { GUILayout.Label("Race tracker not available", label); GUILayout.EndVertical(); return; }

            GUILayout.Label($"Current Race: {tracker.GetCurrentRace()}", label);
            var activeBonuses = tracker.GetActiveRaceBonuses();
            if (activeBonuses.Count > 0)
            {
                GUILayout.Label("Active Bonuses:", label);
                foreach (var b in activeBonuses)
                {
                    GUILayout.Label($"  {b.Type}: {b.GetFormattedValue()}", label);
                }
            }

            var races = tracker.GetAllRaces();
            if (races.Count > 0)
            {
                GUILayout.Space(5);
                string[] raceNames = races.ToArray();
                if (_selectedRaceIndex >= races.Count) _selectedRaceIndex = 0;
                _selectedRaceIndex = GUILayout.SelectionGrid(_selectedRaceIndex, raceNames, 4, button);
                var bonuses = tracker.GetBonusesForRace(races[_selectedRaceIndex]);
                _raceScrollPosition = GUILayout.BeginScrollView(_raceScrollPosition, GUILayout.Height(100));
                foreach (var b in bonuses)
                    GUILayout.Label($"  {b.Type}: {b.GetFormattedValue()}", label);
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

        private static void DrawBirthdayReminder(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label("Birthday Reminder", sectionHeader);

            try
            {
                if (_cachedBirthdayReminderPlugin == null)
                    _cachedBirthdayReminderPlugin = ReflectionHelper.FindType("Plugin", "BirthdayReminder");
                if (_cachedBirthdayReminderPlugin == null) { GUILayout.Label("BirthdayReminder.Plugin not found", label); GUILayout.EndVertical(); return; }

                if (_cachedBirthdayReminderGetManager == null)
                    _cachedBirthdayReminderGetManager = _cachedBirthdayReminderPlugin.GetMethod("GetManager", BindingFlags.Public | BindingFlags.Static);
                if (_cachedBirthdayReminderGetManager == null) { GUILayout.Label("GetManager not found", label); GUILayout.EndVertical(); return; }

                var manager = _cachedBirthdayReminderGetManager.Invoke(null, null);
                if (manager == null) { GUILayout.Label("Manager not loaded", label); GUILayout.EndVertical(); return; }

                var hasBirthdays = manager.GetType().GetProperty("HasBirthdays")?.GetValue(manager);
                var hasUngifted = manager.GetType().GetProperty("HasUngiftedBirthdays")?.GetValue(manager);
                var todays = manager.GetType().GetProperty("TodaysBirthdays")?.GetValue(manager) as System.Collections.IList;

                GUILayout.Label($"Has birthdays today: {hasBirthdays}", label);
                GUILayout.Label($"Has ungifted: {hasUngifted}", label);
                if (todays != null)
                {
                    GUILayout.Label($"Today's birthdays: {todays.Count}", label);
                    foreach (var b in todays)
                    {
                        var nameProp = b?.GetType().GetProperty("NpcName") ?? b?.GetType().GetProperty("Name");
                        var giftedProp = b?.GetType().GetProperty("HasBeenGifted");
                        string name = nameProp?.GetValue(b)?.ToString() ?? "?";
                        bool gifted = (bool)(giftedProp?.GetValue(b) ?? false);
                        GUILayout.Label($"  - {name} {(gifted ? "[Gifted]" : "")}", label);
                    }
                }

                GUILayout.Space(8);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Check Birthdays", button))
                {
                    ReflectionHelper.InvokeStaticMethod(_cachedBirthdayReminderPlugin, "CheckBirthdays");
                }
                if (GUILayout.Button("Manual Refresh", button))
                {
                    manager.GetType().GetMethod("ManualRefresh")?.Invoke(manager, null);
                }
                if (GUILayout.Button("Send All Test Notifications", button))
                {
                    ReflectionHelper.InvokeStaticMethod(_cachedBirthdayReminderPlugin, "SendAllBirthdayNotifications");
                }
                GUILayout.EndHorizontal();
            }
            catch (Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", label);
            }

            GUILayout.EndVertical();
        }

        private static void DrawTodo(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label("Sunhaven Todo", sectionHeader);

            try
            {
                if (_cachedSunhavenTodoPlugin == null)
                    _cachedSunhavenTodoPlugin = ReflectionHelper.FindType("Plugin", "SunhavenTodo");
                if (_cachedSunhavenTodoPlugin == null) { GUILayout.Label("SunhavenTodo.Plugin not found", label); GUILayout.EndVertical(); return; }

                if (_cachedSunhavenTodoGetManager == null)
                    _cachedSunhavenTodoGetManager = _cachedSunhavenTodoPlugin.GetMethod("GetTodoManager", BindingFlags.Public | BindingFlags.Static);
                if (_cachedSunhavenTodoGetManager == null) { GUILayout.Label("GetTodoManager not found", label); GUILayout.EndVertical(); return; }

                var manager = _cachedSunhavenTodoGetManager.Invoke(null, null);
                if (manager == null) { GUILayout.Label("Manager not loaded", label); GUILayout.EndVertical(); return; }

                var getData = manager.GetType().GetMethod("GetData");
                var data = getData?.Invoke(manager, null);
                int count = 0;
                string charName = "";
                if (data != null)
                {
                    var itemsProp = data.GetType().GetProperty("Items");
                    var items = itemsProp?.GetValue(data) as System.Collections.IList;
                    count = items?.Count ?? 0;
                }
                var currentChar = manager.GetType().GetProperty("CurrentCharacter")?.GetValue(manager);
                charName = currentChar?.ToString() ?? "?";

                var shortcutDisplay = _cachedSunhavenTodoPlugin.GetMethod("GetOpenListShortcutDisplay", BindingFlags.Public | BindingFlags.Static);
                string shortcut = shortcutDisplay?.Invoke(null, null)?.ToString() ?? "Ctrl+T";

                GUILayout.Label($"Tasks: {count}", label);
                GUILayout.Label($"Character: {charName}", label);
                GUILayout.Label($"Shortcut: {shortcut}", label);

                GUILayout.Space(8);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Toggle UI", button))
                    ReflectionHelper.InvokeStaticMethod(_cachedSunhavenTodoPlugin, "ToggleUI");
                if (GUILayout.Button("Toggle HUD", button))
                    ReflectionHelper.InvokeStaticMethod(_cachedSunhavenTodoPlugin, "ToggleHUD");
                if (GUILayout.Button("Save Data", button))
                    ReflectionHelper.InvokeStaticMethod(_cachedSunhavenTodoPlugin, "SaveData");
                GUILayout.EndHorizontal();
            }
            catch (Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", label);
            }

            GUILayout.EndVertical();
        }

        private static void DrawAlmanac(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label("Haven's Almanac", sectionHeader);

            try
            {
                if (_cachedHavensAlmanacPlugin == null)
                    _cachedHavensAlmanacPlugin = ReflectionHelper.FindType("Plugin", "HavensAlmanac");
                if (_cachedHavensAlmanacPlugin == null) { GUILayout.Label("HavensAlmanac.Plugin not found", label); GUILayout.EndVertical(); return; }

                if (_cachedHavensAlmanacGetAggregator == null)
                    _cachedHavensAlmanacGetAggregator = _cachedHavensAlmanacPlugin.GetMethod("GetDataAggregator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (_cachedHavensAlmanacGetAggregator == null) { GUILayout.Label("GetDataAggregator not found (internal)", label); GUILayout.EndVertical(); return; }

                var aggregator = _cachedHavensAlmanacGetAggregator.Invoke(null, null);
                if (aggregator == null) { GUILayout.Label("Aggregator not loaded", label); GUILayout.EndVertical(); return; }

                var modCountProp = aggregator.GetType().GetProperty("InstalledModCount");
                var hasDataProp = aggregator.GetType().GetProperty("HasAnyData");
                int modCount = (int)(modCountProp?.GetValue(aggregator) ?? 0);
                bool hasData = (bool)(hasDataProp?.GetValue(aggregator) ?? false);

                GUILayout.Label($"Installed mods: {modCount}", label);
                GUILayout.Label($"Has data: {hasData}", label);

                var providersProp = aggregator.GetType().GetProperty("Providers");
                var providers = providersProp?.GetValue(aggregator) as System.Collections.IList;
                if (providers != null && providers.Count > 0)
                {
                    GUILayout.Label("Providers:", label);
                    foreach (var p in providers)
                    {
                        var modNameProp = p?.GetType().GetProperty("ModName");
                        var isReadyProp = p?.GetType().GetProperty("IsReady");
                        string modName = modNameProp?.GetValue(p)?.ToString() ?? "?";
                        bool ready = (bool)(isReadyProp?.GetValue(p) ?? false);
                        GUILayout.Label($"  - {modName} {(ready ? "[Ready]" : "")}", label);
                    }
                }

                GUILayout.Space(8);
                if (GUILayout.Button("Refresh All", button))
                {
                    aggregator.GetType().GetMethod("RefreshAll")?.Invoke(aggregator, null);
                    Plugin.Log?.LogInfo("[AzraelsMods] Refreshed Almanac data");
                }
            }
            catch (Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", label);
            }

            GUILayout.EndVertical();
        }

        private static IDevToolsPanel _cachedTrinketFortunePanel;

        private static void DrawTrinketFortune(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            var panel = DevToolsRegistry.Panels.FirstOrDefault(p => p.ModGuid == "com.azraelgodking.trinketfortune")
                ?? _cachedTrinketFortunePanel;

            if (panel == null)
            {
                try
                {
                    var panelType = Type.GetType("TrinketFortune.DevTools.TrinketFortunePanel, TrinketFortune");
                    if (panelType != null)
                    {
                        panel = (IDevToolsPanel)Activator.CreateInstance(panelType);
                        _cachedTrinketFortunePanel = panel;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogDebug($"[AzraelsMods] Trinket Fortune panel fallback: {ex.Message}");
                }
            }

            if (panel == null)
            {
                GUILayout.BeginVertical(box);
                GUILayout.Label("Trinket Fortune", sectionHeader);
                GUILayout.Label("Trinket Fortune not loaded.", label);
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical(box);
            GUILayout.Label(panel.DisplayName, sectionHeader);
            GUILayout.Space(5);
            try
            {
                panel.Draw(box, button, label);
            }
            catch (Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", label);
            }
            GUILayout.EndVertical();
        }
    }
}
