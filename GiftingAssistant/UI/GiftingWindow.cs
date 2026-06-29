using System;
using System.Collections.Generic;
using System.Reflection;
using GiftingAssistant.Data;
using GiftingAssistant.Game;
using GiftingAssistant.Integration;
using SunhavenMods.Shared;
using UnityEngine;
using Wish;

namespace GiftingAssistant.UI
{
    /// <summary>
    /// IMGUI window: a per-character daily gift routine. Pick NPCs, see their loved/liked gift
    /// options with icons, track who's been gifted today, mark what you carry, and sort by priority.
    /// </summary>
    public class GiftingWindow : MonoBehaviour
    {
        private const int WINDOW_ID = 0x617A47; // "AzG"
        private const float BASE_WINDOW_WIDTH = 560f;
        private const float BASE_WINDOW_HEIGHT = 620f;
        private const float BASE_HEADER_HEIGHT = 46f;
        private const float BASE_ICON_SIZE = 26f;
        private const int MAX_LOVED_ICONS = 4;
        private const int MAX_LIKED_ICONS = 3;
        private const string PAUSE_ID = "GiftingAssistant_UI";
        private const float BIRTHDAY_REFRESH_INTERVAL = 10f;

        private float _scale = 1f;
        private bool _showPossession = true;

        private bool _isVisible;
        private bool _showPicker;
        private Rect _windowRect;
        private Vector2 _scrollRoster;
        private Vector2 _scrollPicker;
        private Vector2 _scrollGiftEdit;
        private string _pickerSearch = "";

        // Non-null while the per-NPC preferred-gift selector is open (holds the NPC name).
        private string _editGiftsNpc;
        private string _pendingGiftSelectorNpc;
        private bool _giftSelectorLoading;
        private float _openAnimation;

        // Picker list built off the IMGUI thread.
        private readonly List<GiftNpcInfo> _pickerCandidates = new List<GiftNpcInfo>();
        private bool _pickerListDirty = true;
        private string _pickerSearchApplied = "";

        // Gift selector draws a limited number of icon rows per frame.
        private const int GIFT_SELECTOR_ROWS_PER_FRAME = 12;
        private int _giftSelectorVisibleRows;
        private readonly Dictionary<int, string> _itemNameCache = new Dictionary<int, string>();

        private GiftRosterManager _manager;
        private BirthdayIntegration _birthdayIntegration;
        private TodoIntegration _todoIntegration;

        private UiStyle _style;
        private bool _stylesDirty = true;

        // Cached game NPC gift tables, keyed by name.
        private Dictionary<string, GiftNpcInfo> _npcIndex;
        private bool _npcIndexDirty = true;

        // Cached sorted roster view.
        private readonly List<GiftRosterEntry> _sortedRoster = new List<GiftRosterEntry>();
        private bool _sortDirty = true;

        // Birthday flags (refreshed periodically when the integration is available).
        private HashSet<string> _birthdayNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private float _birthdayTimer;

        private float WindowWidth => BASE_WINDOW_WIDTH * _scale;
        private float WindowHeight => BASE_WINDOW_HEIGHT * _scale;
        private float HeaderHeight => BASE_HEADER_HEIGHT * _scale;
        private float IconSize => BASE_ICON_SIZE * _scale;

        public bool IsVisible => _isVisible;

        public void Initialize(GiftRosterManager manager, BirthdayIntegration birthdayIntegration, TodoIntegration todoIntegration)
        {
            _manager = manager;
            _birthdayIntegration = birthdayIntegration;
            _todoIntegration = todoIntegration;
            if (_manager != null)
            {
                _manager.OnRosterChanged += MarkDirty;
                _manager.OnDataLoaded += OnDataLoaded;
            }
            float w = WindowWidth;
            float h = WindowHeight;
            _windowRect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        }

        private void OnDataLoaded()
        {
            _sortDirty = true;
        }

        private void MarkDirty()
        {
            _sortDirty = true;
            _pickerListDirty = true;
        }

        public void SetScale(float scale)
        {
            _scale = Mathf.Clamp(scale, 0.5f, 2.5f);
            _stylesDirty = true;
            _windowRect = new Rect((Screen.width - WindowWidth) / 2f, (Screen.height - WindowHeight) / 2f, WindowWidth, WindowHeight);
        }

        public void SetShowPossession(bool show) => _showPossession = show;

        public void RefreshLocalization()
        {
            // Text is resolved through ModLocalization.T() per draw, so nothing to cache.
        }

        public void InvalidateNpcIndex()
        {
            _npcIndexDirty = true;
            GiftGameData.ScheduleNpcCacheWarm();
        }

        /// <summary>Marks roster sort stale after an external gifted-today update (e.g. in-game gift).</summary>
        public void MarkGiftedSortDirty()
        {
            _sortDirty = true;
        }

        /// <summary>After a new in-game day, refresh gifted flags and re-sort.</summary>
        public void InvalidateGiftedTodaySort()
        {
            _sortDirty = true;
            GiftSuggestionResolver.ClearPrimaryIconCache();
        }

        /// <summary>Called when deferred NPC cache finishes building.</summary>
        public void NotifyNpcCacheReady()
        {
            if (!GiftGameData.IsNpcCacheReady)
                return;
            _npcIndexDirty = true;
            _pickerListDirty = true;
        }

        public void Show()
        {
            _isVisible = true;
            _openAnimation = 0f;
            _npcIndexDirty = true;
            _sortDirty = true;
            GiftSuggestionResolver.ClearPrimaryIconCache();
            GiftGameData.RefreshGiftedTodayCache();
            GiftGameData.RefreshRelationshipCache();
            RefreshBirthdays();
            GiftGameData.ScheduleNpcCacheWarm();
            IconCache.LoadAllIcons();
            PauseGame(true);
        }

        public void Hide()
        {
            _isVisible = false;
            _showPicker = false;
            _editGiftsNpc = null;
            _pendingGiftSelectorNpc = null;
            _giftSelectorLoading = false;
            PauseGame(false);
        }

        public void Toggle()
        {
            if (_isVisible)
                Hide();
            else
                Show();
        }

        private void PauseGame(bool pause)
        {
            try
            {
                if (Player.Instance != null)
                {
                    if (pause)
                        Player.Instance.AddPauseObject(PAUSE_ID);
                    else
                        Player.Instance.RemovePauseObject(PAUSE_ID);
                }

                var playerInputType = Type.GetType("PlayerInput, Assembly-CSharp");
                if (playerInputType != null)
                {
                    var method = playerInputType.GetMethod(pause ? "DisableInput" : "EnableInput",
                        BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                    method?.Invoke(null, new object[] { PAUSE_ID });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[GiftingAssistant] Input blocking failed: {ex.Message}");
            }
        }

        private void Update()
        {
            if (!_isVisible)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_editGiftsNpc != null || _giftSelectorLoading || _pendingGiftSelectorNpc != null)
                {
                    _editGiftsNpc = null;
                    _pendingGiftSelectorNpc = null;
                    _giftSelectorLoading = false;
                }
                else if (_showPicker)
                    _showPicker = false;
                else
                    Hide();
                return;
            }

            _openAnimation = Mathf.MoveTowards(_openAnimation, 1f, Time.unscaledDeltaTime * 8f);

            _birthdayTimer += Time.unscaledDeltaTime;
            if (_birthdayTimer >= BIRTHDAY_REFRESH_INTERVAL)
            {
                _birthdayTimer = 0f;
                RefreshBirthdays();
            }

            ProcessDeferredUiWork();
        }

        private void ProcessDeferredUiWork()
        {
            if (_sortDirty && !_sortProcessing)
            {
                _sortProcessing = true;
                try
                {
                    EnsureSorted();
                }
                finally
                {
                    _sortProcessing = false;
                }
            }

            if (_showPicker && _pickerListDirty)
                RebuildPickerCandidates();

            string searchNow = _pickerSearch?.Trim().ToLowerInvariant() ?? "";
            if (_showPicker && searchNow != _pickerSearchApplied)
                _pickerListDirty = true;

            if (_pendingGiftSelectorNpc != null)
            {
                GiftGameData.EnsureNpcCacheReady();
                _editGiftsNpc = _pendingGiftSelectorNpc;
                _pendingGiftSelectorNpc = null;
                _giftSelectorLoading = false;
                _giftSelectorVisibleRows = GIFT_SELECTOR_ROWS_PER_FRAME;
                _scrollGiftEdit = Vector2.zero;
                _itemNameCache.Clear();
            }

            if (_editGiftsNpc != null && !_giftSelectorLoading)
            {
                _giftSelectorVisibleRows = Mathf.Min(
                    _giftSelectorVisibleRows + GIFT_SELECTOR_ROWS_PER_FRAME,
                    GetGiftSelectorRowCount(_editGiftsNpc) + GIFT_SELECTOR_ROWS_PER_FRAME);
            }
        }

        private bool _sortProcessing;

        private int GetGiftSelectorRowCount(string npcName)
        {
            var info = ResolveNpc(npcName);
            if (info == null)
                return 0;
            return info.LovedItemIds.Count + info.LikedItemIds.Count;
        }

        private void RebuildPickerCandidates()
        {
            _pickerListDirty = false;
            _pickerCandidates.Clear();
            _pickerSearchApplied = _pickerSearch?.Trim().ToLowerInvariant() ?? "";

            if (!GiftGameData.IsNpcCacheReady)
            {
                GiftGameData.ScheduleNpcCacheWarm();
                return;
            }

            string query = _pickerSearchApplied;
            foreach (var npc in GiftGameData.GetAllNpcs())
            {
                if (_manager.Contains(npc.Name) || _manager.Contains(GiftGameData.NormalizeNpcName(npc.Name)))
                    continue;
                if (query.Length > 0 && npc.Name.ToLowerInvariant().IndexOf(query, StringComparison.Ordinal) < 0)
                    continue;
                _pickerCandidates.Add(npc);
            }
        }

        private void RefreshBirthdays()
        {
            if (_birthdayIntegration != null && _birthdayIntegration.IsAvailable)
                _birthdayNames = _birthdayIntegration.GetTodaysBirthdayNames();
            else if (_birthdayNames.Count > 0)
                _birthdayNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private void OnGUI()
        {
            if (!_isVisible || _manager == null)
                return;

            if (_stylesDirty || _style == null)
            {
                _style = _style ?? new UiStyle();
                _style.Build(_scale);
                _stylesDirty = false;
            }

            _windowRect.width = WindowWidth;
            _windowRect.height = WindowHeight;

            GUI.color = new Color(1, 1, 1, _openAnimation);
            GUI.depth = -1000;

            _windowRect = GUI.Window(WINDOW_ID, _windowRect, DrawWindow, "", _style.Window);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);

            GUI.color = Color.white;
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            DrawHeader();
            DrawDivider();
            DrawStats();
            DrawTrackingMode();

            if (_editGiftsNpc != null || _giftSelectorLoading || _pendingGiftSelectorNpc != null)
                DrawGiftSelector();
            else if (_showPicker)
                DrawPicker();
            else
                DrawRoster();

            DrawFooter();

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, WindowWidth, HeaderHeight));
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("gift.window.title"), _style.Title);
            GUILayout.FlexibleSpace();

            var addLabel = _showPicker ? ModLocalization.T("gift.button.cancel") : ModLocalization.T("gift.button.add");
            if (GUILayout.Button(addLabel, _style.Button, GUILayout.Width(_style.S(90)), GUILayout.Height(_style.S(28))))
            {
                _showPicker = !_showPicker;
                if (_showPicker)
                {
                    _pickerSearch = "";
                    _pickerListDirty = true;
                    GiftGameData.RefreshRelationshipCache();
                    GiftGameData.ScheduleNpcCacheWarm();
                }
            }

            GUILayout.Space(_style.S(8));

            if (GUILayout.Button("X", _style.Button, GUILayout.Width(_style.S(28)), GUILayout.Height(_style.S(28))))
                Hide();

            GUILayout.EndHorizontal();
        }

        private void DrawDivider()
        {
            GUILayout.Space(_style.S(4));
            var rect = GUILayoutUtility.GetRect(WindowWidth - _style.S(40), _style.S(3));
            if (Event.current.type == UnityEngine.EventType.Repaint && _style.GoldLine != null)
                GUI.DrawTexture(rect, _style.GoldLine);
            GUILayout.Space(_style.S(4));
        }

        private void DrawStats()
        {
            int tracked = _manager.GetEntries().Count;
            int remaining = 0;
            foreach (var entry in _manager.GetEntries())
            {
                if (!IsGiftedToday(entry))
                    remaining++;
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(ModLocalization.T("gift.stats.tracked", tracked), _style.Stats);
            GUILayout.Space(_style.S(20));
            GUILayout.Label(ModLocalization.T("gift.stats.remaining", remaining), _style.Stats);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(_style.S(6));
        }

        private void DrawTrackingMode()
        {
            if (_todoIntegration == null)
                return;

            string modeKey;
            string hintKey = null;

            if (_todoIntegration.CanPushTodos)
            {
                modeKey = "gift.tracking.mode.todo";
            }
            else if (_todoIntegration.IsEnabled && !_todoIntegration.IsAvailable)
            {
                modeKey = "gift.tracking.mode.roster";
                hintKey = "gift.tracking.hint.todoNotInstalled";
            }
            else
            {
                modeKey = "gift.tracking.mode.roster";
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(ModLocalization.T(modeKey), _style.Footer);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (hintKey != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(ModLocalization.T(hintKey), _style.Footer);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(_style.S(4));
        }

        private void DrawRoster()
        {
            if (_sortDirty)
            {
                GUILayout.Label(ModLocalization.T("gift.list.loading"), _style.Label);
                return;
            }

            _scrollRoster = GUILayout.BeginScrollView(_scrollRoster, GUILayout.ExpandHeight(true));

            if (_sortedRoster.Count == 0)
            {
                GUILayout.Space(_style.S(20));
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(ModLocalization.T("gift.list.empty"), _style.Label);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                for (int i = 0; i < _sortedRoster.Count; i++)
                    DrawRosterRow(_sortedRoster[i], i);
            }

            GUILayout.EndScrollView();
        }

        private void DrawRosterRow(GiftRosterEntry entry, int index)
        {
            bool gifted = IsGiftedToday(entry);
            _style.Row.normal.background = _style.RowBackground(index, gifted);

            GUILayout.BeginVertical(_style.Row);

            // Top line: priority, name (+ birthday), gifted toggle, remove.
            GUILayout.BeginHorizontal();

            var prioColor = _style.PriorityColors.TryGetValue(entry.Priority, out var pc) ? pc : _style.TextDark;
            var prevColor = GUI.color;
            GUI.color = new Color(prioColor.r, prioColor.g, prioColor.b, _openAnimation);
            if (GUILayout.Button(ModLocalization.T($"gift.priority.{entry.Priority}"), _style.PriorityBadge,
                    GUILayout.Width(_style.S(64)), GUILayout.Height(_style.S(24))))
            {
                _manager.SetPriority(entry.NpcName, NextPriority(entry.Priority));
            }
            GUI.color = prevColor;

            GUILayout.Space(_style.S(6));

            string name = entry.NpcName;
            if (_birthdayNames.Contains(entry.NpcName))
                name = $"{name}  ({ModLocalization.T("gift.label.birthday")})";
            GUILayout.Label(name, _style.LabelBold, GUILayout.Height(_style.S(24)));

            string hearts = FormatRelationshipLabel(entry.NpcName);
            if (!string.IsNullOrEmpty(hearts))
            {
                GUILayout.Space(_style.S(4));
                GUILayout.Label(hearts, _style.Label, GUILayout.Height(_style.S(24)));
            }

            GUILayout.FlexibleSpace();

            bool newGifted = GUILayout.Toggle(gifted, gifted
                ? ModLocalization.T("gift.label.gifted")
                : ModLocalization.T("gift.label.notGifted"), _style.Button,
                GUILayout.Width(_style.S(96)), GUILayout.Height(_style.S(24)));
            if (newGifted != gifted)
            {
                _manager.SetManualGifted(entry.NpcName, newGifted);
                if (newGifted)
                    GiftGameData.SetGiftedToday(entry.NpcName, true);
                _sortDirty = true;
            }

            GUILayout.Space(_style.S(4));
            if (GUILayout.Button(ModLocalization.T("gift.button.gifts"), _style.Button,
                    GUILayout.Width(_style.S(54)), GUILayout.Height(_style.S(24))))
            {
                _showPicker = false;
                _pendingGiftSelectorNpc = entry.NpcName;
                _giftSelectorLoading = true;
                _editGiftsNpc = null;
                GiftGameData.ScheduleNpcCacheWarm();
            }

            if (_todoIntegration != null && _todoIntegration.CanPushTodos)
            {
                GUILayout.Space(_style.S(4));
                if (GUILayout.Button(ModLocalization.T("gift.button.todo"), _style.Button,
                        GUILayout.Width(_style.S(54)), GUILayout.Height(_style.S(24))))
                {
                    Plugin.QueuePublishGiftTodo(entry);
                }
            }

            GUILayout.Space(_style.S(4));
            if (GUILayout.Button("x", _style.Button, GUILayout.Width(_style.S(24)), GUILayout.Height(_style.S(24))))
            {
                _manager.RemoveNpc(entry.NpcName);
                Plugin.QueueRemoveGiftTodo(entry.NpcName);
                _sortDirty = true;
                _pickerListDirty = true;
            }

            GUILayout.EndHorizontal();

            // Second line: gift icons.
            DrawGiftIcons(entry);

            GUILayout.EndVertical();
        }

        private void DrawGiftIcons(GiftRosterEntry entry)
        {
            var preferredIds = GiftSuggestionResolver.ResolveDisplayGiftIds(entry);
            if (preferredIds.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(_style.S(6));
                GUILayout.Label(ModLocalization.T("gift.label.preferred"), _style.Label, GUILayout.Width(_style.S(64)));
                DrawIconRow(preferredIds, MAX_LOVED_ICONS);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }

            var info = ResolveNpc(entry.NpcName);
            if (info == null)
                return;

            GUILayout.BeginHorizontal();
            GUILayout.Space(_style.S(6));

            if (info.LovedItemIds.Count > 0)
            {
                GUILayout.Label(ModLocalization.T("gift.label.loves"), _style.Label, GUILayout.Width(_style.S(48)));
                DrawIconRow(info.LovedItemIds, MAX_LOVED_ICONS);
            }

            if (info.LikedItemIds.Count > 0)
            {
                GUILayout.Space(_style.S(8));
                GUILayout.Label(ModLocalization.T("gift.label.likes"), _style.Label, GUILayout.Width(_style.S(48)));
                DrawIconRow(info.LikedItemIds, MAX_LIKED_ICONS);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawIconRow(List<int> itemIds, int max)
        {
            int count = Mathf.Min(itemIds.Count, max);
            for (int i = 0; i < count; i++)
            {
                int itemId = itemIds[i];
                var icon = IconCache.GetIcon(itemId);
                string itemName = GetCachedItemName(itemId);

                int owned = _showPossession ? PlayerInventoryReader.GetAmount(itemId) : 0;
                var iconRect = GUILayoutUtility.GetRect(IconSize, IconSize, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
                if (icon != null)
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                GUI.Label(iconRect, new GUIContent("", itemName));

                if (_showPossession && owned > 0)
                {
                    GUILayout.Label(owned.ToString(), _style.LabelBold, GUILayout.Width(_style.S(22)), GUILayout.Height(IconSize));
                }
                GUILayout.Space(_style.S(2));
            }

            if (itemIds.Count > max)
                GUILayout.Label($"+{itemIds.Count - max}", _style.Label, GUILayout.Height(IconSize));
        }

        private void DrawPicker()
        {
            GUILayout.BeginVertical(_style.Panel);

            GUILayout.Label(ModLocalization.T("gift.picker.title"), _style.Header);
            GUILayout.Space(_style.S(4));

            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("gift.picker.search"), _style.Label, GUILayout.Width(_style.S(56)));
            _pickerSearch = GUILayout.TextField(_pickerSearch, _style.TextField, GUILayout.Height(_style.S(24)));
            GUILayout.EndHorizontal();
            GUILayout.Space(_style.S(6));

            _scrollPicker = GUILayout.BeginScrollView(_scrollPicker, GUILayout.ExpandHeight(true));

            if (!GiftGameData.IsNpcCacheReady || _pickerListDirty)
            {
                GUILayout.Space(_style.S(12));
                GUILayout.Label(ModLocalization.T("gift.picker.loading"), _style.Label);
            }
            else if (_pickerCandidates.Count == 0)
            {
                GUILayout.Space(_style.S(12));
                GUILayout.Label(ModLocalization.T("gift.picker.empty"), _style.Label);
            }
            else
            {
                foreach (var npc in _pickerCandidates)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("+", _style.Button, GUILayout.Width(_style.S(28)), GUILayout.Height(_style.S(24))))
                    {
                    _manager.AddNpc(npc.Name);
                    _sortDirty = true;
                    _pickerListDirty = true;
                    _npcIndexDirty = true;
                    }
                    GUILayout.Space(_style.S(6));
                    GUILayout.Label(npc.Name, _style.Label, GUILayout.Height(_style.S(24)));
                    GUILayout.Space(_style.S(4));
                    GUILayout.Label(FormatRelationshipLabel(npc.Name), _style.Label, GUILayout.Height(_style.S(24)));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(_style.S(2));
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(_style.S(6));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(ModLocalization.T("gift.button.done"), _style.Button,
                    GUILayout.Width(_style.S(90)), GUILayout.Height(_style.S(26))))
            {
                _showPicker = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawGiftSelector()
        {
            GUILayout.BeginVertical(_style.Panel);

            if (_giftSelectorLoading || _pendingGiftSelectorNpc != null)
            {
                string loadingName = _pendingGiftSelectorNpc ?? _editGiftsNpc ?? "";
                GUILayout.Label(ModLocalization.T("gift.gifts.title", loadingName), _style.Header);
                GUILayout.Space(_style.S(12));
                GUILayout.Label(ModLocalization.T("gift.gifts.loading"), _style.Label);
                GUILayout.EndVertical();
                return;
            }

            string npcName = _editGiftsNpc;
            GUILayout.Label(ModLocalization.T("gift.gifts.title", npcName), _style.Header);
            GUILayout.Label(ModLocalization.T("gift.gifts.hint"), _style.Label);
            GUILayout.Space(_style.S(6));

            var info = ResolveNpc(npcName);

            _scrollGiftEdit = GUILayout.BeginScrollView(_scrollGiftEdit, GUILayout.ExpandHeight(true));

            if (info == null || (info.LovedItemIds.Count == 0 && info.LikedItemIds.Count == 0))
            {
                GUILayout.Space(_style.S(12));
                GUILayout.Label(ModLocalization.T("gift.gifts.none"), _style.Label);
            }
            else
            {
                int rowsDrawn = 0;
                int rowBudget = _giftSelectorVisibleRows;

                if (info.LovedItemIds.Count > 0)
                {
                    GUILayout.Label(ModLocalization.T("gift.label.loves"), _style.LabelBold);
                    foreach (int itemId in info.LovedItemIds)
                    {
                        if (rowsDrawn >= rowBudget)
                            break;
                        DrawGiftSelectorRow(npcName, itemId);
                        rowsDrawn++;
                    }
                    if (rowsDrawn < info.LovedItemIds.Count)
                    {
                        GUILayout.Label(ModLocalization.T("gift.gifts.loadingMore"), _style.Label);
                    }
                    else
                    {
                        GUILayout.Space(_style.S(6));
                    }
                }

                if (rowsDrawn < rowBudget && info.LikedItemIds.Count > 0)
                {
                    GUILayout.Label(ModLocalization.T("gift.label.likes"), _style.LabelBold);
                    foreach (int itemId in info.LikedItemIds)
                    {
                        if (rowsDrawn >= rowBudget)
                            break;
                        DrawGiftSelectorRow(npcName, itemId);
                        rowsDrawn++;
                    }
                    if (rowsDrawn < info.LovedItemIds.Count + info.LikedItemIds.Count)
                        GUILayout.Label(ModLocalization.T("gift.gifts.loadingMore"), _style.Label);
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(_style.S(6));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(ModLocalization.T("gift.gifts.clear"), _style.Button,
                    GUILayout.Width(_style.S(110)), GUILayout.Height(_style.S(26))))
            {
                _manager.ClearPreferredGifts(npcName);
                _sortDirty = true;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(ModLocalization.T("gift.button.done"), _style.Button,
                    GUILayout.Width(_style.S(90)), GUILayout.Height(_style.S(26))))
            {
                _editGiftsNpc = null;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawGiftSelectorRow(string npcName, int itemId)
        {
            bool preferred = _manager.IsPreferredGift(npcName, itemId);
            string itemName = GetCachedItemName(itemId);
            float checkSize = _style.S(28);

            _style.Row.normal.background = preferred ? _style.GiftSelectorRowSelected : null;

            GUILayout.BeginHorizontal(_style.Row);

            bool newPreferred = GUILayout.Toggle(preferred, preferred ? "\u2713" : "", _style.Checkbox,
                GUILayout.Width(checkSize), GUILayout.MinWidth(checkSize), GUILayout.Height(IconSize));
            if (newPreferred != preferred)
            {
                _manager.TogglePreferredGift(npcName, itemId);
                _sortDirty = true;
            }

            GUILayout.Space(_style.S(6));

            var icon = IconCache.GetIcon(itemId);
            var iconRect = GUILayoutUtility.GetRect(IconSize, IconSize, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
            if (icon != null)
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            GUI.Label(iconRect, new GUIContent("", itemName));

            GUILayout.Space(_style.S(8));

            var nameStyle = preferred ? _style.LabelBold : _style.Label;
            GUILayout.Label(itemName, nameStyle, GUILayout.ExpandWidth(true), GUILayout.Height(IconSize));

            if (_showPossession)
            {
                int owned = PlayerInventoryReader.GetAmount(itemId);
                if (owned > 0)
                {
                    GUILayout.Label(ModLocalization.T("gift.gifts.owned", owned), _style.Label,
                        GUILayout.Width(_style.S(72)), GUILayout.Height(IconSize));
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(_style.S(2));
        }

        private void DrawFooter()
        {
            DrawDivider();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(ModLocalization.T("gift.footer.hint", Plugin.GetOpenShortcutDisplay()), _style.Footer);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void EnsureSorted()
        {
            if (!_sortDirty)
                return;
            _sortDirty = false;

            _sortedRoster.Clear();
            _sortedRoster.AddRange(_manager.GetEntries());
            _sortedRoster.Sort((a, b) =>
            {
                bool ga = IsGiftedToday(a);
                bool gb = IsGiftedToday(b);
                if (ga != gb)
                    return ga ? 1 : -1;
                int pri = ((int)b.Priority).CompareTo((int)a.Priority);
                if (pri != 0)
                    return pri;
                return string.Compare(a.NpcName, b.NpcName, StringComparison.OrdinalIgnoreCase);
            });
        }

        private bool IsGiftedToday(GiftRosterEntry entry)
        {
            if (entry == null)
                return false;
            if (entry.ManualGiftedToday)
                return true;
            return GiftGameData.HasGivenGiftToday(entry.NpcName);
        }

        private GiftNpcInfo ResolveNpc(string npcName)
        {
            if (_npcIndex == null || _npcIndexDirty)
                RebuildNpcIndex();

            if (_npcIndex == null)
                return null;

            if (_npcIndex.TryGetValue(npcName, out var info))
                return info;

            string normalized = GiftGameData.NormalizeNpcName(npcName);
            return !string.IsNullOrEmpty(normalized) && _npcIndex.TryGetValue(normalized, out info)
                ? info
                : null;
        }

        private void RebuildNpcIndex()
        {
            _npcIndexDirty = false;
            _npcIndex = new Dictionary<string, GiftNpcInfo>(StringComparer.OrdinalIgnoreCase);

            if (!GiftGameData.IsNpcCacheReady)
            {
                GiftGameData.ScheduleNpcCacheWarm();
                return;
            }

            foreach (var entry in _manager.GetEntries())
            {
                if (entry == null || string.IsNullOrEmpty(entry.NpcName))
                    continue;

                var info = GiftGameData.GetNpcInfo(entry.NpcName);
                if (info == null)
                    continue;

                string normalized = GiftGameData.NormalizeNpcName(entry.NpcName);
                if (!string.IsNullOrEmpty(normalized))
                    _npcIndex[normalized] = info;
                _npcIndex[entry.NpcName] = info;
            }
        }

        private string GetCachedItemName(int itemId)
        {
            if (_itemNameCache.TryGetValue(itemId, out string cached))
                return cached;

            cached = ItemSearch.GetItemName(itemId) ?? $"#{itemId}";
            _itemNameCache[itemId] = cached;
            return cached;
        }

        private string FormatRelationshipLabel(string npcName)
        {
            var points = GiftGameData.GetRelationshipPoints(npcName);
            if (!points.HasValue)
                return ModLocalization.T("gift.label.heartsUnknown");

            return GiftGameData.FormatRelationshipHearts(points.Value);
        }

        private static GiftPriority NextPriority(GiftPriority current)
        {
            switch (current)
            {
                case GiftPriority.Low: return GiftPriority.Normal;
                case GiftPriority.Normal: return GiftPriority.High;
                case GiftPriority.High: return GiftPriority.Urgent;
                default: return GiftPriority.Low;
            }
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnRosterChanged -= MarkDirty;
                _manager.OnDataLoaded -= OnDataLoaded;
            }
            _style?.Destroy();
        }
    }
}
