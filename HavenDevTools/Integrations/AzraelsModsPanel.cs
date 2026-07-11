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
        private static Type _cachedBirthdayReminderPlugin;
        private static Type _cachedSunhavenTodoPlugin;
        private static Type _cachedHavensAlmanacPlugin;
        private static Type _cachedCropOptimizerPlugin;
        private static MethodInfo _cachedCropGetHudSummary;
        private static Type _cachedFasterRacesPlugin;
        private static Type _cachedTrinketFortunePlugin;
        private static MethodInfo _cachedTrinketGetDevToolsSummary;
        private static Type _cachedGiftingAssistantPlugin;

        private static Type ResolveModPlugin(string assemblyName, ref Type cache, params string[] alternateAssemblyNames)
        {
            if (cache != null
                && string.Equals(cache.Assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
            {
                return cache;
            }

            cache = ReflectionHelper.FindModPlugin(assemblyName);
            if (cache == null && alternateAssemblyNames != null)
            {
                foreach (var alt in alternateAssemblyNames)
                {
                    cache = ReflectionHelper.FindModPlugin(alt);
                    if (cache != null)
                        break;
                }
            }

            return cache;
        }

        public static void Draw(GUIStyle boxStyle, GUIStyle buttonStyle, GUIStyle labelStyle, GUIStyle sectionHeaderStyle)
        {
            Plugin.RefreshInstalledMods();

            var installedTabs = new List<string>();
            var installedIndices = new List<int>();
            int idx = 0;
            if (Plugin.HasSenpaisChest) { installedTabs.Add(ModLocalization.T("azrael.tab.senpais_chest")); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasTheVault) { installedTabs.Add(ModLocalization.T("azrael.tab.the_vault")); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasSMUT) { installedTabs.Add(ModLocalization.T("azrael.tab.smut")); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasHavensBirthright) { installedTabs.Add(ModLocalization.T("azrael.tab.birthright")); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasBirthdayReminder) { installedTabs.Add(ModLocalization.T("azrael.tab.birthday")); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasSunhavenTodo) { installedTabs.Add(ModLocalization.T("azrael.tab.todo")); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasHavensAlmanac) { installedTabs.Add(ModLocalization.T("azrael.tab.almanac")); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasTrinketFortune) { installedTabs.Add(ModLocalization.T("azrael.tab.trinket_fortune")); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasCropOptimizer) { installedTabs.Add(ModLocalization.T("azrael.tab.crop_optimizer")); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasFasterRaces) { installedTabs.Add(ModLocalization.T("azrael.tab.faster_races")); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasHavensRespec) { installedTabs.Add(ModLocalization.T("azrael.tab.havens_respec")); installedIndices.Add(idx); }
            idx++;
            if (Plugin.HasGiftingAssistant) { installedTabs.Add(ModLocalization.T("azrael.tab.gifting_assistant")); installedIndices.Add(idx); }

            if (installedTabs.Count == 0)
            {
                GUILayout.Label(ModLocalization.T("azrael.none_detected"), labelStyle);
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
                case 8: DrawCropOptimizer(boxStyle, buttonStyle, labelStyle, sectionHeaderStyle); break;
                case 9: DrawFasterRaces(boxStyle, buttonStyle, labelStyle, sectionHeaderStyle); break;
                case 10: DrawHavensRespec(boxStyle, buttonStyle, labelStyle, sectionHeaderStyle); break;
                case 11: DrawGiftingAssistant(boxStyle, buttonStyle, labelStyle, sectionHeaderStyle); break;
            }

            GUILayout.EndScrollView();
        }

        private static void DrawSenpaisChest(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label(ModLocalization.T("azrael.senpai.title"), sectionHeader);

            try
            {
                var plugin = ResolveModPlugin("SenpaisChest", ref _cachedSenpaisChestPlugin, "SenpaiChest");
                if (plugin == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.plugin_not_found", "Senpai's Chest"), label);
                    GUILayout.EndVertical();
                    return;
                }

                var manager = ReflectionHelper.InvokeStaticMethod(plugin, "GetManager");
                if (manager == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.awaiting_character"), label);
                    GUILayout.EndVertical();
                    return;
                }

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
                if (GUILayout.Button(ModLocalization.T("azrael.senpai.trigger_scan"), button))
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
            GUILayout.Label(ModLocalization.T("azrael.vault.title"), sectionHeader);

            var tracker = Plugin.GetCurrencyTracker();
            if (tracker == null) { GUILayout.Label(ModLocalization.T("devtools.currency.unavailable"), label); GUILayout.EndVertical(); return; }

            var summary = tracker.GetSummary();
            GUILayout.Label(ModLocalization.T("azrael.vault.currencies"), label);
            if (summary.VaultCurrencies.Count == 0)
                GUILayout.Label(ModLocalization.T("azrael.vault.empty"), label);
            else
            {
                foreach (var kvp in summary.VaultCurrencies)
                    GUILayout.Label($"  {kvp.Key}: {kvp.Value}", label);
            }

            GUILayout.Space(8);
            if (ModConfig.TheVaultFullVaultInspector != null)
            {
                bool inspector = ModConfig.TheVaultFullVaultInspector.Value;
                bool newInspector = GUILayout.Toggle(inspector, ModLocalization.T("azrael.vault.inspector_toggle"), label);
                if (newInspector != inspector)
                    ModConfig.TheVaultFullVaultInspector.Value = newInspector;
                GUILayout.Label(ModLocalization.T("azrael.vault.inspector_hint"), label);
            }

            GUILayout.EndVertical();
        }

        private static void DrawSMUT(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label(ModLocalization.T("azrael.smut.title"), sectionHeader);

            var inspector = Plugin.GetBundleInspector();
            if (inspector == null) { GUILayout.Label(ModLocalization.T("devtools.museum.unavailable"), label); GUILayout.EndVertical(); return; }

            var stats = inspector.GetDonationStats();
            if (stats.IsLoaded)
            {
                GUILayout.Label(ModLocalization.T("devtools.museum.character", stats.CharacterName), label);
                GUILayout.Label(ModLocalization.T("devtools.museum.progress", stats.TotalDonated, stats.TotalItems, stats.CompletionPercent), label);
            }

            var sections = inspector.GetAllSections();
            if (sections.Count == 0) { GUILayout.EndVertical(); return; }

            string[] sectionNames = sections.Select(s => s.Name).ToArray();
            if (_selectedSectionIndex >= sections.Count) _selectedSectionIndex = 0;
            GUILayout.Label(ModLocalization.T("devtools.museum.section"), label);
            _selectedSectionIndex = GUILayout.SelectionGrid(_selectedSectionIndex, sectionNames, 3, button);

            var selectedSection = sections[_selectedSectionIndex];
            if (selectedSection.Bundles.Count > 0)
            {
                string[] bundleNames = selectedSection.Bundles.Select(b => b.Name).ToArray();
                if (_selectedBundleIndex >= selectedSection.Bundles.Count) _selectedBundleIndex = 0;
                GUILayout.Label(ModLocalization.T("devtools.museum.bundle"), label);
                _selectedBundleIndex = GUILayout.SelectionGrid(_selectedBundleIndex, bundleNames, 2, button);

                var selectedBundle = selectedSection.Bundles[_selectedBundleIndex];
                GUILayout.Label(ModLocalization.T("devtools.museum.itemsIn", selectedBundle.Name), sectionHeader);
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
                    if (GUILayout.Button(canSpawn ? ModLocalization.T("devtools.museum.spawn", qty) : "—", button, GUILayout.Width(70)))
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
            GUILayout.Label(ModLocalization.T("azrael.birthright.title"), sectionHeader);

            var tracker = Plugin.GetRaceModifierTracker();
            if (tracker == null) { GUILayout.Label(ModLocalization.T("devtools.race.unavailable"), label); GUILayout.EndVertical(); return; }

            GUILayout.Label(ModLocalization.T("devtools.race.current", tracker.GetCurrentRace()), label);
            var activeBonuses = tracker.GetActiveRaceBonuses();
            if (activeBonuses.Count > 0)
            {
                GUILayout.Label(ModLocalization.T("azrael.birthright.active_bonuses"), label);
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
            GUILayout.Label(ModLocalization.T("azrael.birthday.title"), sectionHeader);

            try
            {
                var plugin = ResolveModPlugin("BirthdayReminder", ref _cachedBirthdayReminderPlugin);
                if (plugin == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.plugin_not_found", "Birthday Reminder"), label);
                    GUILayout.EndVertical();
                    return;
                }

                var manager = ReflectionHelper.InvokeStaticMethod(plugin, "GetManager");
                if (manager == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.awaiting_character"), label);
                    GUILayout.EndVertical();
                    return;
                }

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
                if (GUILayout.Button(ModLocalization.T("azrael.birthday.check"), button))
                {
                    ReflectionHelper.InvokeStaticMethod(plugin, "CheckBirthdays");
                }
                if (GUILayout.Button(ModLocalization.T("azrael.birthday.refresh"), button))
                {
                    manager.GetType().GetMethod("ManualRefresh")?.Invoke(manager, null);
                }
                if (GUILayout.Button(ModLocalization.T("azrael.birthday.test_notify"), button))
                {
                    ReflectionHelper.InvokeStaticMethod(plugin, "SendAllBirthdayNotifications");
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
            GUILayout.Label(ModLocalization.T("azrael.todo.title"), sectionHeader);

            try
            {
                var plugin = ResolveModPlugin("SunhavenTodo", ref _cachedSunhavenTodoPlugin);
                if (plugin == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.plugin_not_found", "Sun Haven Todo"), label);
                    GUILayout.EndVertical();
                    return;
                }

                var manager = ReflectionHelper.InvokeStaticMethod(plugin, "GetTodoManager");
                if (manager == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.awaiting_character"), label);
                    var shortcutOnly = ReflectionHelper.InvokeStaticMethod(plugin, "GetOpenListShortcutDisplay") as string;
                    if (!string.IsNullOrEmpty(shortcutOnly))
                        GUILayout.Label($"Shortcut: {shortcutOnly}", label);
                    GUILayout.EndVertical();
                    return;
                }

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

                var shortcutDisplay = ReflectionHelper.GetStaticMethod(plugin, "GetOpenListShortcutDisplay");
                string shortcut = shortcutDisplay?.Invoke(null, null)?.ToString() ?? "Ctrl+T";

                GUILayout.Label($"Tasks: {count}", label);
                GUILayout.Label($"Character: {charName}", label);
                GUILayout.Label($"Shortcut: {shortcut}", label);

                GUILayout.Space(8);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(ModLocalization.T("azrael.todo.toggle_ui"), button))
                    ReflectionHelper.InvokeStaticMethod(plugin, "ToggleUI");
                if (GUILayout.Button(ModLocalization.T("azrael.todo.toggle_hud"), button))
                    ReflectionHelper.InvokeStaticMethod(plugin, "ToggleHUD");
                if (GUILayout.Button(ModLocalization.T("azrael.todo.save"), button))
                    ReflectionHelper.InvokeStaticMethod(plugin, "SaveData");
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
            GUILayout.Label(ModLocalization.T("azrael.almanac.title"), sectionHeader);

            try
            {
                var plugin = ResolveModPlugin("HavensAlmanac", ref _cachedHavensAlmanacPlugin);
                if (plugin == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.plugin_not_found", "Haven's Almanac"), label);
                    GUILayout.EndVertical();
                    return;
                }

                var aggregator = ReflectionHelper.InvokeStaticMethod(plugin, "GetDataAggregator");
                if (aggregator == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.awaiting_character"), label);
                    GUILayout.EndVertical();
                    return;
                }

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
                if (GUILayout.Button(ModLocalization.T("azrael.almanac.refresh"), button))
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

        private static void DrawTrinketFortune(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            var registeredPanel = DevToolsRegistry.Panels.FirstOrDefault(p =>
                p.ModGuid == "com.azraelgodking.trinketfortune");
            if (registeredPanel != null)
            {
                GUILayout.BeginVertical(box);
                GUILayout.Label(registeredPanel.DisplayName, sectionHeader);
                GUILayout.Space(5);
                try
                {
                    registeredPanel.Draw(box, button, label);
                }
                catch (Exception ex)
                {
                    GUILayout.Label($"Error: {ex.Message}", label);
                }
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical(box);
            GUILayout.Label(ModLocalization.T("azrael.trinket.title"), sectionHeader);

            try
            {
                var plugin = ResolveModPlugin("TrinketFortune", ref _cachedTrinketFortunePlugin);
                if (plugin == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.plugin_not_found", "Trinket Fortune"), label);
                    GUILayout.EndVertical();
                    return;
                }

                if (_cachedTrinketGetDevToolsSummary == null)
                    _cachedTrinketGetDevToolsSummary = ReflectionHelper.GetStaticMethod(plugin, "GetDevToolsSummary");

                string summary = _cachedTrinketGetDevToolsSummary?.Invoke(null, null) as string;
                GUILayout.Label(string.IsNullOrEmpty(summary)
                    ? ModLocalization.T("azrael.trinket.unavailable")
                    : summary, label);
            }
            catch (Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", label);
            }

            GUILayout.EndVertical();
        }

        private static void DrawCropOptimizer(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label(ModLocalization.T("azrael.crop.title"), sectionHeader);

            try
            {
                var plugin = ResolveModPlugin("CropOptimizer", ref _cachedCropOptimizerPlugin);
                if (plugin == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.plugin_not_found", "Crop Optimizer"), label);
                    GUILayout.EndVertical();
                    return;
                }

                if (_cachedCropGetHudSummary == null)
                    _cachedCropGetHudSummary = ReflectionHelper.GetStaticMethod(plugin, "GetHudSummary");

                string summary = _cachedCropGetHudSummary?.Invoke(null, null) as string;
                GUILayout.Label(string.IsNullOrEmpty(summary) ? ModLocalization.T("azrael.crop.unavailable") : summary, label);
            }
            catch (Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", label);
            }

            GUILayout.EndVertical();
        }

        private static void DrawFasterRaces(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label(ModLocalization.T("azrael.races.title"), sectionHeader);

            try
            {
                var plugin = ResolveModPlugin("FasterRaces", ref _cachedFasterRacesPlugin);
                if (plugin == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.plugin_not_found", "Faster Races"), label);
                    GUILayout.EndVertical();
                    return;
                }

                var enableField = plugin.GetField("EnableMod", ReflectionHelper.AllBindingFlags);
                var speedField = plugin.GetField("SpeedBonusPercent", ReflectionHelper.AllBindingFlags);
                bool enabled = ReadConfigEntryBool(enableField?.GetValue(null));
                float speed = ReadConfigEntryFloat(speedField?.GetValue(null));

                if (!enabled)
                    GUILayout.Label(ModLocalization.T("azrael.races.disabled"), label);
                else
                    GUILayout.Label(ModLocalization.T("azrael.races.speed", speed), label);
            }
            catch (Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", label);
            }

            GUILayout.EndVertical();
        }

        private static void DrawHavensRespec(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            RespecSimulatorPanel.Draw(box, button, label, sectionHeader);
        }

        private static void DrawGiftingAssistant(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label(ModLocalization.T("azrael.gifting.title"), sectionHeader);

            try
            {
                var plugin = ResolveModPlugin("GiftingAssistant", ref _cachedGiftingAssistantPlugin);
                if (plugin == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.plugin_not_found", "Gifting Assistant"), label);
                    GUILayout.EndVertical();
                    return;
                }

                var enabledProp = plugin.GetProperty("StaticEnabled", ReflectionHelper.AllBindingFlags);
                bool enabled = enabledProp?.GetValue(null) is bool b && b;
                if (!enabled)
                {
                    GUILayout.Label(ModLocalization.T("azrael.gifting.disabled"), label);
                    GUILayout.EndVertical();
                    return;
                }

                string shortcut = ReflectionHelper.InvokeStaticMethod(plugin, "GetOpenShortcutDisplay") as string ?? "Ctrl+G";
                GUILayout.Label($"Shortcut: {shortcut}", label);

                var manager = ReflectionHelper.InvokeStaticMethod(plugin, "GetManager");
                if (manager == null)
                {
                    GUILayout.Label(ModLocalization.T("azrael.suite.awaiting_character"), label);
                }
                else
                {
                    var getEntries = manager.GetType().GetMethod("GetEntries");
                    var entries = getEntries?.Invoke(manager, null) as System.Collections.ICollection;
                    int count = entries?.Count ?? 0;
                    var charProp = manager.GetType().GetProperty("CurrentCharacter");
                    string charName = charProp?.GetValue(manager)?.ToString();
                    if (string.IsNullOrEmpty(charName))
                        charName = "?";

                    GUILayout.Label(ModLocalization.T("azrael.gifting.roster", count), label);
                    GUILayout.Label($"Character: {charName}", label);
                }

                GUILayout.Space(8);
                if (GUILayout.Button(ModLocalization.T("azrael.gifting.toggle_ui"), button))
                    ReflectionHelper.InvokeStaticMethod(plugin, "ToggleUI");
            }
            catch (Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", label);
            }

            GUILayout.EndVertical();
        }

        private static bool ReadConfigEntryBool(object configEntry)
        {
            if (configEntry == null) return false;
            var valueProp = configEntry.GetType().GetProperty("Value");
            return valueProp?.GetValue(configEntry) is bool b && b;
        }

        private static float ReadConfigEntryFloat(object configEntry)
        {
            if (configEntry == null) return 0f;
            var valueProp = configEntry.GetType().GetProperty("Value");
            return valueProp?.GetValue(configEntry) is float f ? f : 0f;
        }
    }
}
