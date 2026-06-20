using System;
using System.Collections.Generic;
using HavenDevTools.API;
using HavenDevTools.Config;
using HavenDevTools.Integrations;
using HavenDevTools.Services;
using SunhavenMods.Shared;
using UnityEngine;
using Wish;

namespace HavenDevTools.UI
{
    /// <summary>
    /// Main debug window UI (IMGUI-based)
    /// </summary>
    public class DebugWindow : MonoBehaviour
    {
        private bool _isVisible;
        private Rect _windowRect;
        private Vector2 _scrollPosition;

        // Styles
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _boxStyle;
        private bool _stylesInitialized;

        // Tab state
        private int _selectedTab;
        private int _toolsSubTab;

        // Item search (Senpai's Chest style)
        private string _itemSearchText = "";
        private string _lastItemSearchQuery = null;
        private string _itemIdInput = "";
        private string _spawnAmount = "1";
        private int _selectedItemId;
        private string _selectedItemName = "";
        private List<KeyValuePair<int, string>> _searchResults = new List<KeyValuePair<int, string>>();
        private Vector2 _itemScrollPosition;

        // Currency state
        private Vector2 _currencyScrollPosition;

        // Bundle state
        private int _selectedSectionIndex;
        private int _selectedBundleIndex;
        private Vector2 _bundleScrollPosition;

        // Race state
        private int _selectedRaceIndex;
        private Vector2 _raceScrollPosition;

        // Version checking state
        private static Dictionary<string, VersionChecker.VersionCheckResult> _versionResults = new Dictionary<string, VersionChecker.VersionCheckResult>();
        private static bool _isCheckingVersions;
        private Vector2 _versionScrollPosition;

        // Known mod GUIDs and versions
        private static readonly (string guid, string name, string version)[] _knownMods = new[]
        {
            ("com.azraelgodking.havendevtools", "Haven Dev Tools", "1.2.2"),
            ("com.azraelgodking.senpaischest", "Senpai's Chest", "2.7.0"),
            ("com.azraelgodking.squirrelsbirthdayreminder", "Birthday Reminder", "1.4.2"),
            ("com.azraelgodking.havensbirthright", "Haven's Birthright", "2.2.2"),
            ("com.azraelgodking.sunhavenmuseumutilitytracker", "S.M.U.T.", "2.4.2"),
            ("com.azraelgodking.sunhaventodo", "Sunhaven Todo", "1.4.3"),
            ("com.azraelgodking.thevault", "The Vault", "3.3.2"),
            ("com.azraelgodking.havensalmanac", "Haven's Almanac", "1.4.2"),
            ("com.azraelgodking.fasterraces", "Faster Races", "1.4.2"),
            ("com.azraelgodking.trinketfortune", "Trinket Fortune", "1.2.2"),
            ("com.azraelgodking.cropoptimizer", "Crop Optimizer", "1.4.3"),
            ("com.azraelgodking.havensrespec", "Haven's Respec", "1.3.2"),
        };

        private const string PAUSE_ID = "HavenDevTools_Debug";

        private void Awake()
        {
            _windowRect = new Rect(50, 50, 500, 600);
        }

        public void Toggle()
        {
            if (_isVisible)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            _isVisible = true;
            BlockInput();
            Plugin.Log?.LogInfo("[DebugWindow] Shown");
        }

        public void Hide()
        {
            _isVisible = false;
            UnblockInput();
            Plugin.Log?.LogInfo("[DebugWindow] Hidden");
        }

        private void BlockInput()
        {
            try
            {
                if (Wish.Player.Instance != null)
                {
                    Wish.Player.Instance.AddPauseObject(PAUSE_ID);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[DebugWindow] Could not block input: {ex.Message}");
            }
        }

        private void UnblockInput()
        {
            try
            {
                if (Wish.Player.Instance != null)
                {
                    Wish.Player.Instance.RemovePauseObject(PAUSE_ID);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[DebugWindow] Could not unblock input: {ex.Message}");
            }
        }

        private void Update()
        {
            if (_isVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        private void OnGUI()
        {
            if (!_isVisible || !Plugin.IsAuthorized) return;

            InitializeStyles();

            _windowRect = GUI.Window(
                GetHashCode(),
                _windowRect,
                DrawWindow,
                "",
                _windowStyle
            );
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            var darkBg = new Texture2D(1, 1);
            darkBg.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.12f, 0.98f));
            darkBg.Apply();

            var buttonBg = new Texture2D(1, 1);
            buttonBg.SetPixel(0, 0, new Color(0.2f, 0.4f, 0.6f, 0.9f));
            buttonBg.Apply();

            var buttonHover = new Texture2D(1, 1);
            buttonHover.SetPixel(0, 0, new Color(0.3f, 0.5f, 0.7f, 0.95f));
            buttonHover.Apply();

            var sectionBg = new Texture2D(1, 1);
            sectionBg.SetPixel(0, 0, new Color(0.12f, 0.12f, 0.18f, 0.9f));
            sectionBg.Apply();

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = darkBg, textColor = Color.white },
                padding = new RectOffset(10, 10, 25, 10)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.4f, 0.8f, 1f) }
            };

            _sectionHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                normal = { background = buttonBg, textColor = Color.white },
                hover = { background = buttonHover, textColor = Color.white },
                padding = new RectOffset(8, 8, 4, 4)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
            };

            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = sectionBg }
            };

            _stylesInitialized = true;
        }

        private void DrawWindow(int windowId)
        {
            // Header
            GUILayout.Label(ModLocalization.T("devtools.title"), _headerStyle);
            GUILayout.Label(ModLocalization.T("devtools.player", Plugin.CurrentPlayerName ?? string.Empty), _labelStyle);
            GUILayout.Space(5);

            // Mod status
            string yes = ModLocalization.T("devtools.yes");
            string no = ModLocalization.T("devtools.no");
            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("devtools.mod.thevault", Plugin.HasTheVault ? yes : no), _labelStyle, GUILayout.Width(100));
            GUILayout.Label(ModLocalization.T("devtools.mod.smut", Plugin.HasSMUT ? yes : no), _labelStyle, GUILayout.Width(80));
            GUILayout.Label(ModLocalization.T("devtools.mod.birthright", Plugin.HasHavensBirthright ? yes : no), _labelStyle, GUILayout.Width(100));
            GUILayout.Label(ModLocalization.T("devtools.mod.senpai", Plugin.HasSenpaisChest ? yes : no), _labelStyle, GUILayout.Width(80));
            GUILayout.Label(ModLocalization.T("devtools.mod.todo", Plugin.HasSunhavenTodo ? yes : no), _labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Tabs
            _selectedTab = GUILayout.Toolbar(_selectedTab, new[]
            {
                ModLocalization.T("devtools.tab.tools"),
                ModLocalization.T("devtools.tab.azraels_mods"),
                ModLocalization.T("devtools.tab.extensions")
            }, _buttonStyle);
            GUILayout.Space(10);

            // Content
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0: DrawToolsTab(); break;
                case 1: AzraelsModsPanel.Draw(_boxStyle, _buttonStyle, _labelStyle, _sectionHeaderStyle); break;
                case 2: DrawExtensionsTab(); break;
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Close button
            if (GUILayout.Button(ModLocalization.T("devtools.close"), _buttonStyle))
            {
                Hide();
            }

                GUI.DragWindow(new Rect(0, 0, 500, 30));
        }

        private void DrawExtensionsTab()
        {
            var panels = DevToolsRegistry.Panels;
            if (panels.Count == 0)
            {
                GUILayout.BeginVertical(_boxStyle);
                GUILayout.Label(ModLocalization.T("devtools.extensions"), _sectionHeaderStyle);
                GUILayout.Space(5);
                GUILayout.Label(ModLocalization.T("devtools.extensions.none"), _labelStyle);
                GUILayout.EndVertical();
                return;
            }

            // Azrael's mods shown in Azrael's Mods tab - exclude from Extensions
            var azraelsModGuids = new HashSet<string> { "com.azraelgodking.trinketfortune" };
            foreach (var panel in panels)
            {
                if (azraelsModGuids.Contains(panel.ModGuid)) continue;
                GUILayout.BeginVertical(_boxStyle);
                GUILayout.Label(panel.DisplayName, _sectionHeaderStyle);
                GUILayout.Space(5);
                try
                {
                    panel.Draw(_boxStyle, _buttonStyle, _labelStyle);
                }
                catch (Exception ex)
                {
                    GUILayout.Label($"Error: {ex.Message}", _labelStyle);
                }
                GUILayout.EndVertical();
                GUILayout.Space(8);
            }
        }

        #region Tools Tab

        private void DrawToolsTab()
        {
            _toolsSubTab = GUILayout.Toolbar(_toolsSubTab, new[]
            {
                ModLocalization.T("devtools.tab.items"),
                ModLocalization.T("devtools.tab.currencies"),
                ModLocalization.T("devtools.tab.console"),
                ModLocalization.T("devtools.tab.log"),
                ModLocalization.T("devtools.tab.perf"),
                ModLocalization.T("devtools.tab.utility")
            }, _buttonStyle);
            GUILayout.Space(8);

            switch (_toolsSubTab)
            {
                case 0: DrawItemsTab(); break;
                case 1: DrawCurrenciesTab(); break;
                case 2: DrawConsoleTab(); break;
                case 3: DrawLogViewerTab(); break;
                case 4: DrawPerformanceTab(); break;
                case 5: DrawUtilityTab(); break;
            }
        }

        private void DrawConsoleTab()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label(ModLocalization.T("devtools.console.title"), _sectionHeaderStyle);
            GUILayout.Space(5);
            GUILayout.Label(ModLocalization.T("devtools.console.hint"), _labelStyle);
            var console = Plugin.GetCommandConsole();
            if (console != null)
            {
                console.Draw(_boxStyle, _buttonStyle, _labelStyle, _textFieldStyle);
            }
            else
            {
                GUILayout.Label(ModLocalization.T("devtools.console.init"), _labelStyle);
            }
            GUILayout.EndVertical();
        }

        private void DrawLogViewerTab()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label(ModLocalization.T("devtools.log.title"), _sectionHeaderStyle);
            GUILayout.Space(5);
            var logViewer = Plugin.GetLogViewer();
            if (logViewer != null)
            {
                logViewer.Draw(_boxStyle, _buttonStyle, _labelStyle);
            }
            else
            {
                GUILayout.Label(ModLocalization.T("devtools.log.init"), _labelStyle);
            }
            GUILayout.EndVertical();
        }

        private void DrawPerformanceTab()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label(ModLocalization.T("devtools.perf.title"), _sectionHeaderStyle);
            GUILayout.Space(5);
            float fps = 1f / Mathf.Max(0.0001f, Time.deltaTime);
            long memBytes = GC.GetTotalMemory(false);
            float memMB = memBytes / (1024f * 1024f);
            GUILayout.Label(ModLocalization.T("devtools.perf.fps", fps), _labelStyle);
            GUILayout.Label(ModLocalization.T("devtools.perf.memory", memMB), _labelStyle);
            GUILayout.Label(ModLocalization.T("devtools.perf.overlayHint"), _labelStyle);
            GUILayout.EndVertical();
        }

        #endregion

        #region Items Tab

        private void DrawItemsTab()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label(ModLocalization.T("devtools.items.title"), _sectionHeaderStyle);
            GUILayout.Space(5);

            // Held item
            var heldItem = Plugin.GetItemInspector()?.GetHeldItem();
            if (heldItem.HasValue)
                GUILayout.Label(ModLocalization.T("devtools.items.held", heldItem.Value.name, heldItem.Value.id), _labelStyle);
            else
                GUILayout.Label(ModLocalization.T("devtools.items.heldNone"), _labelStyle);

            GUILayout.Space(10);

            // Search (Senpai's Chest style - live search, 2+ chars)
            GUILayout.Label(ModLocalization.T("devtools.search"), _labelStyle);
            GUILayout.BeginHorizontal();
            _itemSearchText = GUILayout.TextField(_itemSearchText, _textFieldStyle);
            if (GUILayout.Button(ModLocalization.T("devtools.clear"), _buttonStyle, GUILayout.Width(50)))
            {
                _itemSearchText = "";
                _lastItemSearchQuery = "";
                _searchResults.Clear();
            }
            GUILayout.EndHorizontal();

            if (_itemSearchText != _lastItemSearchQuery)
            {
                _lastItemSearchQuery = _itemSearchText;
                _searchResults = ItemSearch.SearchItems(_itemSearchText, 50);
            }

            if (_selectedItemId > 0)
            {
                GUILayout.Space(2);
                GUILayout.Label(ModLocalization.T("devtools.selected", ItemSearch.FormatDisplay(_selectedItemName, _selectedItemId)), _labelStyle);
            }

            if (_searchResults.Count > 0)
            {
                GUILayout.Space(4);
                float h = Mathf.Min(180, 24 + _searchResults.Count * 22);
                _itemScrollPosition = GUILayout.BeginScrollView(_itemScrollPosition, GUILayout.Height(h));
                foreach (var result in _searchResults)
                {
                    string label = ItemSearch.FormatDisplay(result.Value, result.Key);
                    bool selected = result.Key == _selectedItemId;
                    if (GUILayout.Button(label, selected ? _buttonStyle : _labelStyle, GUILayout.Height(22)))
                    {
                        _selectedItemId = result.Key;
                        _selectedItemName = result.Value;
                        _itemIdInput = result.Key.ToString();
                    }
                }
                GUILayout.EndScrollView();
            }
            else if (_itemSearchText.Length >= 2)
            {
                GUILayout.Space(2);
                GUILayout.Label(ModLocalization.T("devtools.items.notFound"), _labelStyle);
            }

            GUILayout.Space(10);

            // Spawn
            GUILayout.Label(ModLocalization.T("devtools.spawn"), _labelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("devtools.spawn.id"), _labelStyle, GUILayout.Width(22));
            _itemIdInput = GUILayout.TextField(_itemIdInput, _textFieldStyle, GUILayout.Width(70));
            GUILayout.Label(ModLocalization.T("devtools.spawn.qty"), _labelStyle, GUILayout.Width(28));
            _spawnAmount = GUILayout.TextField(_spawnAmount, _textFieldStyle, GUILayout.Width(40));
            if (GUILayout.Button("1", _buttonStyle, GUILayout.Width(22))) _spawnAmount = "1";
            if (GUILayout.Button("10", _buttonStyle, GUILayout.Width(28))) _spawnAmount = "10";
            if (GUILayout.Button("99", _buttonStyle, GUILayout.Width(28))) _spawnAmount = "99";
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(ModLocalization.T("devtools.spawn.toInventory"), _buttonStyle))
            {
                if (int.TryParse(_itemIdInput, out int itemId) && int.TryParse(_spawnAmount, out int amount) && amount > 0)
                    Plugin.GetItemInspector()?.SpawnItem(itemId, amount);
            }
            if (_selectedItemId > 0 && GUILayout.Button(ModLocalization.T("devtools.spawn.one"), _buttonStyle, GUILayout.Width(70)))
            {
                Plugin.GetItemInspector()?.SpawnItem(_selectedItemId, 1);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        #endregion

        #region Currencies Tab

        private void DrawCurrenciesTab()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label(ModLocalization.T("devtools.currency.title"), _sectionHeaderStyle);
            GUILayout.Space(5);

            var tracker = Plugin.GetCurrencyTracker();
            if (tracker == null)
            {
                GUILayout.Label(ModLocalization.T("devtools.currency.unavailable"), _labelStyle);
                GUILayout.EndVertical();
                return;
            }

            var summary = tracker.GetSummary();

            // Basic currencies
            GUILayout.Label(ModLocalization.T("devtools.currency.gold", summary.Gold), _labelStyle);
            GUILayout.Label(ModLocalization.T("devtools.currency.orbs", summary.Orbs), _labelStyle);
            GUILayout.Label(ModLocalization.T("devtools.currency.tickets", summary.Tickets), _labelStyle);

            GUILayout.Space(10);

            // Inventory currencies
            GUILayout.Label(ModLocalization.T("devtools.currency.inventory"), _sectionHeaderStyle);
            _currencyScrollPosition = GUILayout.BeginScrollView(_currencyScrollPosition, GUILayout.Height(120));
            if (summary.InventoryCurrencies.Count == 0)
            {
                GUILayout.Label(ModLocalization.T("devtools.currency.none"), _labelStyle);
            }
            else
            {
                foreach (var kvp in summary.InventoryCurrencies)
                {
                    GUILayout.Label($"  {kvp.Key}: {kvp.Value}", _labelStyle);
                }
            }
            GUILayout.EndScrollView();

            // Vault currencies
            if (Plugin.HasTheVault)
            {
                GUILayout.Space(10);
                GUILayout.Label(ModLocalization.T("devtools.currency.vault"), _sectionHeaderStyle);
                if (summary.VaultCurrencies.Count == 0)
                {
                    GUILayout.Label(ModLocalization.T("devtools.currency.vaultEmpty"), _labelStyle);
                }
                else
                {
                    foreach (var kvp in summary.VaultCurrencies)
                    {
                        GUILayout.Label($"  {kvp.Key}: {kvp.Value}", _labelStyle);
                    }
                }
            }
            else
            {
                GUILayout.Space(5);
                GUILayout.Label(ModLocalization.T("devtools.currency.vaultMissing"), _labelStyle);
            }

            GUILayout.EndVertical();
        }

        #endregion

        #region Bundles Tab

        private void DrawBundlesTab()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label(ModLocalization.T("devtools.museum.title"), _sectionHeaderStyle);
            GUILayout.Space(5);

            if (!Plugin.HasSMUT)
            {
                GUILayout.Label(ModLocalization.T("devtools.museum.notInstalled"), _labelStyle);
                GUILayout.EndVertical();
                return;
            }

            var inspector = Plugin.GetBundleInspector();
            if (inspector == null)
            {
                GUILayout.Label(ModLocalization.T("devtools.museum.unavailable"), _labelStyle);
                GUILayout.EndVertical();
                return;
            }

            // Donation stats
            var stats = inspector.GetDonationStats();
            if (stats.IsLoaded)
            {
                GUILayout.Label(ModLocalization.T("devtools.museum.character", stats.CharacterName), _labelStyle);
                GUILayout.Label(ModLocalization.T("devtools.museum.progress", stats.TotalDonated, stats.TotalItems, stats.CompletionPercent), _labelStyle);
            }
            else
            {
                GUILayout.Label(ModLocalization.T("devtools.museum.notLoaded"), _labelStyle);
            }

            GUILayout.Space(10);

            // Sections
            var sections = inspector.GetAllSections();
            if (sections.Count == 0)
            {
                GUILayout.Label(ModLocalization.T("devtools.museum.noData"), _labelStyle);
                GUILayout.EndVertical();
                return;
            }

            // Section selector
            string[] sectionNames = new string[sections.Count];
            for (int i = 0; i < sections.Count; i++)
            {
                sectionNames[i] = sections[i].Name;
            }

            GUILayout.Label(ModLocalization.T("devtools.museum.section"), _labelStyle);
            _selectedSectionIndex = GUILayout.SelectionGrid(_selectedSectionIndex, sectionNames, 3, _buttonStyle);

            if (_selectedSectionIndex >= sections.Count) _selectedSectionIndex = 0;
            var selectedSection = sections[_selectedSectionIndex];

            GUILayout.Space(5);

            // Bundle selector
            if (selectedSection.Bundles.Count > 0)
            {
                string[] bundleNames = new string[selectedSection.Bundles.Count];
                for (int i = 0; i < selectedSection.Bundles.Count; i++)
                {
                    bundleNames[i] = selectedSection.Bundles[i].Name;
                }

                if (_selectedBundleIndex >= selectedSection.Bundles.Count) _selectedBundleIndex = 0;

                GUILayout.Label(ModLocalization.T("devtools.museum.bundle"), _labelStyle);
                _selectedBundleIndex = GUILayout.SelectionGrid(_selectedBundleIndex, bundleNames, 2, _buttonStyle);

                var selectedBundle = selectedSection.Bundles[_selectedBundleIndex];

                GUILayout.Space(5);

                // Items in bundle
                GUILayout.Label(ModLocalization.T("devtools.museum.itemsIn", selectedBundle.Name), _sectionHeaderStyle);
                _bundleScrollPosition = GUILayout.BeginScrollView(_bundleScrollPosition, GUILayout.Height(150));
                foreach (var item in selectedBundle.Items)
                {
                    bool donated = inspector.HasDonated(item.Id);
                    string status = donated ? "[X]" : "[ ]";
                    string qtyLabel = item.Quantity > 1 ? $" x{item.Quantity}" : "";
                    bool canSpawn = item.GameItemId > 0;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{status} {item.Name} (ID: {item.GameItemId})", _labelStyle, GUILayout.Width(320));
                    var prevEnabled = GUI.enabled;
                    GUI.enabled = canSpawn;
                    if (GUILayout.Button(canSpawn ? ModLocalization.T("devtools.museum.spawn", qtyLabel) : "—", _buttonStyle, GUILayout.Width(90)))
                    {
                        if (canSpawn) Plugin.GetItemInspector()?.SpawnItem(item.GameItemId, item.Quantity);
                    }
                    GUI.enabled = prevEnabled;
                    if (!canSpawn) GUILayout.Label(ModLocalization.T("devtools.museum.idInAssets"), _labelStyle, GUILayout.Width(100));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

        #endregion

        #region Race Bonuses Tab

        private void DrawRaceBonusesTab()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label(ModLocalization.T("devtools.race.title"), _sectionHeaderStyle);
            GUILayout.Space(5);

            var tracker = Plugin.GetRaceModifierTracker();
            if (tracker == null)
            {
                GUILayout.Label(ModLocalization.T("devtools.race.unavailable"), _labelStyle);
                GUILayout.EndVertical();
                return;
            }

            // Current race
            string currentRace = tracker.GetCurrentRace();
            GUILayout.Label(ModLocalization.T("devtools.race.current", currentRace), _labelStyle);

            GUILayout.Space(10);

            // Active bonuses
            if (Plugin.HasHavensBirthright)
            {
                var activeBonuses = tracker.GetActiveRaceBonuses();
                GUILayout.Label(ModLocalization.T("devtools.race.bonuses"), _sectionHeaderStyle);
                if (activeBonuses.Count == 0)
                {
                    GUILayout.Label(ModLocalization.T("devtools.race.noBonuses"), _labelStyle);
                }
                else
                {
                    foreach (var bonus in activeBonuses)
                    {
                        GUILayout.Label($"  {bonus.Type}: {bonus.GetFormattedValue()}", _labelStyle);
                        GUILayout.Label($"    {bonus.Description}", _labelStyle);
                    }
                }
            }
            else
            {
                GUILayout.Label(ModLocalization.T("devtools.race.birthrightMissing"), _labelStyle);
            }

            GUILayout.Space(10);

            // Race browser
            GUILayout.Label(ModLocalization.T("devtools.race.browse"), _sectionHeaderStyle);
            var races = tracker.GetAllRaces();
            if (races.Count > 0)
            {
                string[] raceNames = races.ToArray();
                if (_selectedRaceIndex >= races.Count) _selectedRaceIndex = 0;

                _selectedRaceIndex = GUILayout.SelectionGrid(_selectedRaceIndex, raceNames, 4, _buttonStyle);

                GUILayout.Space(5);

                var bonuses = tracker.GetBonusesForRace(races[_selectedRaceIndex]);
                _raceScrollPosition = GUILayout.BeginScrollView(_raceScrollPosition, GUILayout.Height(150));
                if (bonuses.Count == 0)
                {
                    GUILayout.Label(ModLocalization.T("devtools.race.noDefined"), _labelStyle);
                }
                else
                {
                    foreach (var bonus in bonuses)
                    {
                        GUILayout.Label($"  {bonus.Type}: {bonus.GetFormattedValue()}", _labelStyle);
                        GUILayout.Label($"    {bonus.Description}", _labelStyle);
                    }
                }
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

        #endregion

        #region Utility Tab

        private void DrawUtilityTab()
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label(ModLocalization.T("devtools.utility"), _sectionHeaderStyle);
            GUILayout.Space(5);

            // Version Checker Section
            DrawVersionCheckerSection();

            GUILayout.Space(10);

            // Config
            GUILayout.Label(ModLocalization.T("devtools.config"), _sectionHeaderStyle);
            if (GUILayout.Button(ModLocalization.T("devtools.config.reload"), _buttonStyle))
            {
                ConfigReloader.ReloadHavenDevToolsConfig();
            }

            GUILayout.Space(10);

            // Logging
            GUILayout.Label(ModLocalization.T("devtools.logging"), _sectionHeaderStyle);

            if (GUILayout.Button(ModLocalization.T("devtools.logCurrencyIds"), _buttonStyle))
            {
                Plugin.Log?.LogInfo("=== Currency Item IDs ===");
                foreach (var kvp in CurrencyTracker.CurrencyItemIds)
                {
                    Plugin.Log?.LogInfo($"  {kvp.Key}: {kvp.Value}");
                }
            }

            if (GUILayout.Button(ModLocalization.T("devtools.logPlayerStats"), _buttonStyle))
            {
                LogPlayerStats();
            }

            GUILayout.Space(10);

            // Hash generator
            GUILayout.Label(ModLocalization.T("devtools.auth.title"), _sectionHeaderStyle);
            GUILayout.Label(ModLocalization.T("devtools.auth.hint"), _labelStyle);
            if (GUILayout.Button(ModLocalization.T("devtools.auth.logHash"), _buttonStyle))
            {
                try
                {
                    var steamId = typeof(Plugin).GetMethod("TryGetSteamId",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        ?.Invoke(null, null) as string;
                    if (!string.IsNullOrEmpty(steamId))
                    {
                        string hash = Plugin.GenerateHash(steamId);
                        Plugin.Log?.LogInfo($"Steam ID: {steamId}");
                        Plugin.Log?.LogInfo($"Steam ID Hash: {hash}");
                    }
                    else
                    {
                        Plugin.Log?.LogWarning("Could not retrieve Steam ID");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogError($"Error generating hash: {ex.Message}");
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawVersionCheckerSection()
        {
            GUILayout.Label(ModLocalization.T("devtools.versions.title"), _sectionHeaderStyle);
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();

            // Check all button
            GUI.enabled = !_isCheckingVersions;
            if (GUILayout.Button(_isCheckingVersions ? ModLocalization.T("devtools.versions.checking") : ModLocalization.T("devtools.versions.checkAll"), _buttonStyle))
            {
                CheckAllModVersions();
            }
            GUI.enabled = true;

            // Test notification button
            if (GUILayout.Button(ModLocalization.T("devtools.versions.testNotify"), _buttonStyle, GUILayout.Width(120)))
            {
                TestUpdateNotification();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Results display
            _versionScrollPosition = GUILayout.BeginScrollView(_versionScrollPosition, GUILayout.Height(140));

            foreach (var mod in _knownMods)
            {
                GUILayout.BeginHorizontal();

                // Mod name and current version
                GUILayout.Label($"{mod.name} v{mod.version}", _labelStyle, GUILayout.Width(200));

                // Status
                if (_versionResults.TryGetValue(mod.guid, out var result))
                {
                    if (!result.Success)
                    {
                        // Error state
                        var errorStyle = new GUIStyle(_labelStyle) { normal = { textColor = new Color(1f, 0.5f, 0.5f) } };
                        GUILayout.Label($"Error: {result.ErrorMessage}", errorStyle);
                    }
                    else if (result.UpdateAvailable)
                    {
                        // Update available
                        var updateStyle = new GUIStyle(_labelStyle) { normal = { textColor = new Color(1f, 0.9f, 0.3f) } };
                        GUILayout.Label(ModLocalization.T("devtools.versions.update", result.LatestVersion), updateStyle);
                    }
                    else
                    {
                        // Up to date
                        var okStyle = new GUIStyle(_labelStyle) { normal = { textColor = new Color(0.5f, 1f, 0.5f) } };
                        GUILayout.Label(ModLocalization.T("devtools.versions.upToDate"), okStyle);
                    }
                }
                else
                {
                    GUILayout.Label(ModLocalization.T("devtools.versions.notChecked"), _labelStyle);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private void CheckAllModVersions()
        {
            _isCheckingVersions = true;
            _versionResults.Clear();
            int pendingChecks = _knownMods.Length;

            foreach (var mod in _knownMods)
            {
                VersionChecker.CheckForUpdate(mod.guid, mod.version, Plugin.Log, result =>
                {
                    _versionResults[mod.guid] = result;
                    pendingChecks--;
                    if (pendingChecks <= 0)
                    {
                        _isCheckingVersions = false;
                    }

                    // Log result
                    if (result.Success)
                    {
                        if (result.UpdateAvailable)
                            Plugin.Log?.LogInfo($"[VersionChecker] {mod.name}: Update available v{result.LatestVersion}");
                        else
                            Plugin.Log?.LogInfo($"[VersionChecker] {mod.name}: Up to date (v{mod.version})");
                    }
                    else
                    {
                        Plugin.Log?.LogWarning($"[VersionChecker] {mod.name}: {result.ErrorMessage}");
                    }
                });
            }
        }

        private void TestUpdateNotification()
        {
            // Create a fake result to test the notification system
            var fakeResult = new VersionChecker.VersionCheckResult
            {
                Success = true,
                UpdateAvailable = true,
                CurrentVersion = "1.0.0",
                LatestVersion = "9.9.9",
                ModName = "Test Mod (HavenDevTools)",
                NexusUrl = "https://www.nexusmods.com/sunhaven"
            };

            Plugin.Log?.LogInfo("[VersionChecker] Testing notification system...");
            fakeResult.NotifyUpdateAvailable(Plugin.Log);
        }

        private void LogPlayerStats()
        {
            try
            {
                if (Wish.Player.Instance == null)
                {
                    Plugin.Log?.LogInfo("Player not available");
                    return;
                }

                Plugin.Log?.LogInfo("=== Player Stats ===");

                var playerType = Wish.Player.Instance.GetType();
                var props = playerType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                foreach (var prop in props)
                {
                    try
                    {
                        if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                        {
                            var value = prop.GetValue(Wish.Player.Instance);
                            if (value != null && (value is int || value is float || value is string || value is bool))
                            {
                                Plugin.Log?.LogInfo($"  {prop.Name}: {value}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogDebug($"[DebugWindow] Player stats property iteration: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error logging player stats: {ex.Message}");
            }
        }

        #endregion
    }
}
