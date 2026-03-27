using System;
using System.Collections.Generic;
using SunhavenMods.Shared;
using TheVault;
using TheVault.Patches;
using TheVault.Vault;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Wish;

namespace TheVault.UI
{
    /// <summary>
    /// Unity uGUI vault window (Canvas). Legacy <see cref="VaultUI"/> remains in the project and can be re-enabled via config.
    /// </summary>
    public class VaultUguiPanel : MonoBehaviour
    {
        private const string PauseId = "TheVault_Ugui";

        /// <summary>Logical design size in pixels before <see cref="UpdatePanelScale"/> applies config + screen fit.</summary>
        private const float DesignW = 548f;
        /// <summary>Taller panel so footer + tabs + list keep non-overlapping bands.</summary>
        private const float DesignH = 676f;

        private VaultManager _vaultManager;
        private bool _visible;
        private float _windowScale = 1f;

        private Canvas _canvas;
        private RectTransform _panelRt;
        private Text _titleText;
        private Text _statusText;
        private InputField _amountField;
        private Text _selectionLabel;
        private Transform _listContent;
        private ScrollRect _scroll;
        private GameObject _settingsRoot;
        private CurrencyCategory _category = CurrencyCategory.SeasonalToken;
        private string _selectedCurrencyId = "";
        private float _nextRefresh;
        private bool _debugDumpMode;

        private int _lastScreenW;
        private int _lastScreenH;

        private readonly List<CategoryTabVisual> _categoryTabs = new List<CategoryTabVisual>();

        // Palette — cool slate + gold accents (Sun Haven–friendly)
        private static readonly Color ColPanel = new Color(0.11f, 0.13f, 0.18f, 0.97f);
        private static readonly Color ColPanelDeep = new Color(0.07f, 0.08f, 0.11f, 0.96f);
        private static readonly Color ColHeaderBar = new Color(0.14f, 0.18f, 0.28f, 1f);
        private static readonly Color ColAccentStrip = new Color(0.35f, 0.65f, 0.92f, 1f);
        private static readonly Color ColRowA = new Color(0.13f, 0.15f, 0.21f, 0.96f);
        private static readonly Color ColRowB = new Color(0.16f, 0.18f, 0.24f, 0.96f);
        private static readonly Color ColRowSel = new Color(0.2f, 0.35f, 0.52f, 0.72f);
        private static readonly Color ColTabNormal = new Color(0.15f, 0.18f, 0.25f, 1f);
        private static readonly Color ColTabSelected = new Color(0.24f, 0.38f, 0.58f, 1f);
        private static readonly Color ColTabMuted = new Color(0.11f, 0.13f, 0.17f, 0.75f);
        private static readonly Color ColPrimaryBtn = new Color(0.22f, 0.42f, 0.62f, 1f);
        private static readonly Color ColPrimaryBtnHi = new Color(0.32f, 0.52f, 0.78f, 1f);
        private static readonly Color ColGhostBtn = new Color(0.22f, 0.24f, 0.3f, 0.95f);
        private static readonly Color ColDangerClose = new Color(0.4f, 0.22f, 0.22f, 0.95f);
        private static readonly Color ColDangerCloseHi = new Color(0.55f, 0.3f, 0.3f, 1f);
        private static readonly Color ColGold = new Color(0.95f, 0.82f, 0.38f, 1f);
        private static readonly Color ColTitle = new Color(0.78f, 0.9f, 1f, 1f);
        private static readonly Color ColSubtext = new Color(0.65f, 0.7f, 0.78f, 1f);

        private static readonly CurrencyCategory[] TabCategories =
        {
            CurrencyCategory.SeasonalToken, CurrencyCategory.Key, CurrencyCategory.Special, CurrencyCategory.Custom
        };

        private struct CategoryTabVisual
        {
            public CurrencyCategory Category;
            public Image Background;
        }

        public bool IsVisible => _visible;

        public void Initialize(VaultManager vaultManager)
        {
            _vaultManager = vaultManager;
            BuildUi();
            SetCanvasActive(false);
            Plugin.Log?.LogInfo("VaultUguiPanel initialized");
        }

        public void SetWindowScale(float scale)
        {
            _windowScale = Mathf.Clamp(scale, 0.5f, 2.5f);
            UpdatePanelScale();
        }

        private float ComputeListFitScale()
        {
            if (_vaultManager == null) return 1f;
            if (!_visible) return 1f;
            if (!PlayerPatches.IsVaultLoaded) return 1f;
            if (_debugDumpMode) return 1f;
            if (_settingsRoot != null && _settingsRoot.activeSelf) return 1f;

            // Scroll area geometry is defined by the hardcoded offsets in BuildUi().
            // We use that design-space viewport height and shrink the whole panel further
            // if the current category has too many rows to fit without scrolling.
            const float ScrollViewportDesignH = 286f; // (DesignH - 262 - 122) - (vp 3 + 3)
            const float RowDesignMinH = 56f;
            const float VlgPadTop = 6f;
            const float VlgPadBottom = 6f;
            const float VlgSpacing = 5f;

            var dict = VaultUiShared.GetCurrenciesForCategory(_vaultManager, _category);
            int rows = dict.Count;
            if (rows <= 0) rows = 1; // empty message uses the same min height band

            float desiredContentH = VlgPadTop + VlgPadBottom + rows * RowDesignMinH +
                                     Math.Max(0, rows - 1) * VlgSpacing;
            if (desiredContentH <= 0.01f) return 1f;

            return Mathf.Min(1f, ScrollViewportDesignH / desiredContentH);
        }

        private void UpdatePanelScale()
        {
            if (_panelRt == null) return;
            float cfg = Mathf.Clamp(_windowScale, 0.5f, 2.5f);

            // Screen fit.
            float w = DesignW * cfg;
            float h = DesignH * cfg;
            float maxW = Screen.width * 0.97f;
            float maxH = Screen.height * 0.94f;
            float fScreen = Mathf.Min(1f, maxW / Mathf.Max(1f, w), maxH / Mathf.Max(1f, h));
            float baseScale = cfg * fScreen;

            // Additional fit so list rows can fit in the visible scroll viewport.
            float listFit = ComputeListFitScale();
            float s = Mathf.Clamp(baseScale * listFit, 0.5f, 2.5f);

            _panelRt.sizeDelta = new Vector2(DesignW, DesignH);
            _panelRt.localScale = new Vector3(s, s, 1f);
            RefreshCategoryTabStyles();
        }

        public void Show()
        {
            if (_vaultManager == null || !PlayerPatches.IsVaultLoaded) return;
            _visible = true;
            SetCanvasActive(true);
            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;
            UpdatePanelScale();
            BlockInput(true);
            IconCache.LogStatus();
            SecretGifts.CheckAndGiveFirstOpenGift(PlayerPatches.LoadedCharacterName);
            RefreshList();
            Plugin.Log?.LogInfo("Vault uGUI opened");
        }

        public void Hide()
        {
            if (!_visible) return;
            _visible = false;
            SetCanvasActive(false);
            BlockInput(false);
            Plugin.Log?.LogInfo("Vault uGUI closed");
        }

        public void Toggle()
        {
            if (!PlayerPatches.IsVaultLoaded)
            {
                Plugin.Log?.LogInfo("Cannot toggle vault uGUI - save not yet loaded");
                return;
            }
            if (_visible) Hide();
            else Show();
        }

        private void OnDestroy()
        {
            if (_visible) BlockInput(false);
        }

        private void Update()
        {
            if (!_visible) return;

            if (Screen.width != _lastScreenW || Screen.height != _lastScreenH)
            {
                _lastScreenW = Screen.width;
                _lastScreenH = Screen.height;
                UpdatePanelScale();
            }

            if (Input.GetKeyDown(KeyCode.Escape)) Hide();
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.35f;
                RefreshList();
            }
        }

        private void BlockInput(bool block)
        {
            try
            {
                if (Player.Instance == null) return;
                if (block)
                {
                    Player.Instance.AddPauseObject(PauseId);
                    PlayerInput.DisableInput(PauseId);
                }
                else
                {
                    Player.Instance.RemovePauseObject(PauseId);
                    PlayerInput.EnableInput(PauseId);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Vault uGUI pause/input: {ex.Message}");
            }
        }

        private void SetCanvasActive(bool v)
        {
            if (_canvas != null) _canvas.enabled = v;
        }

        private void EnsureEventSystem()
        {
            // ScrollRect + wheel / keyboard navigation depends on EventSystem modules.
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;

            var esGo = new GameObject("TheVault_EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        /// <summary>
        /// Stretch the Canvas root to the full overlay so child panels are not clipped to Unity's default tiny rect.
        /// </summary>
        private void EnsureRootCanvasFillsScreen()
        {
            var rt = gameObject.GetComponent<RectTransform>();
            if (rt == null)
            {
                Plugin.Log?.LogWarning("[The Vault] VaultUguiPanel: no RectTransform on Canvas root — UI may clip.");
                return;
            }
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private void BuildUi()
        {
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // pixelPerfect rounds layouts and often clips partial pixels badly on non-integer scales
            _canvas.pixelPerfect = false;
            _canvas.sortingOrder = 32000;
            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // Critical: default Canvas GO has a small RectTransform — everything would clip to that box.
            EnsureRootCanvasFillsScreen();
            EnsureEventSystem();

            var panel = CreateChild(gameObject.transform, "Panel", out _panelRt);
            _panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            _panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRt.pivot = new Vector2(0.5f, 0.5f);
            _panelRt.anchoredPosition = Vector2.zero;

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = ColPanel;

            var shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(2f, -2f);

            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.55f, 0.78f, 0.28f);
            outline.effectDistance = new Vector2(1f, -1f);

            const float pad = 14f;

            var header = CreateChild(panel.transform, "Header", out var headerRt);
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.offsetMin = new Vector2(pad, -50f);
            headerRt.offsetMax = new Vector2(-pad, -pad);
            var headerBg = header.AddComponent<Image>();
            headerBg.color = ColHeaderBar;

            _titleText = CreateText(header.transform, "Title", "The Vault", 20, FontStyle.Bold, ColTitle);
            _titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var titleRt = _titleText.rectTransform;
            titleRt.anchorMin = new Vector2(0, 0);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.offsetMin = new Vector2(12f, 0);
            titleRt.offsetMax = new Vector2(-80f, 0);

            var closeBtnGo = CreateButton(header.transform, "Close", "✕", () => Hide(), 40f, 36f, ColDangerClose, ColDangerCloseHi);
            var closeRt = closeBtnGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1, 0.5f);
            closeRt.anchorMax = new Vector2(1, 0.5f);
            closeRt.pivot = new Vector2(1, 0.5f);
            closeRt.anchoredPosition = new Vector2(-6f, 0);

            var accent = CreateChild(panel.transform, "AccentBar", out var accentRt);
            accentRt.anchorMin = new Vector2(0, 1);
            accentRt.anchorMax = new Vector2(1, 1);
            accentRt.pivot = new Vector2(0.5f, 1);
            accentRt.offsetMin = new Vector2(pad, -55f);
            accentRt.offsetMax = new Vector2(-pad, -52f);
            accent.AddComponent<Image>().color = ColAccentStrip;

            var tabs = CreateChild(panel.transform, "Tabs", out var tabsRt);
            tabsRt.anchorMin = new Vector2(0, 1);
            tabsRt.anchorMax = new Vector2(1, 1);
            tabsRt.pivot = new Vector2(0.5f, 1);
            // ~52px tab row so labels are not clipped (was ~38px with force-expand height).
            tabsRt.offsetMin = new Vector2(pad, -118f);
            tabsRt.offsetMax = new Vector2(-pad, -66f);
            var tabH = tabs.AddComponent<HorizontalLayoutGroup>();
            tabH.spacing = 6f;
            tabH.childControlWidth = true;
            tabH.childControlHeight = true;
            tabH.childForceExpandWidth = true;
            tabH.childForceExpandHeight = false;
            tabH.padding = new RectOffset(0, 4, 0, 4);

            _categoryTabs.Clear();
            foreach (var cat in TabCategories)
            {
                var c = cat;
                string label = GetTabLabel(c);
                var tabGo = CreateStyledTabButton(tabs.transform, "Tab_" + c, label, () => SelectTab(c));
                tabGo.GetComponent<LayoutElement>().flexibleWidth = 1f;
                var img = tabGo.GetComponent<Image>();
                _categoryTabs.Add(new CategoryTabVisual { Category = c, Background = img });
            }

            if (Plugin.GetConfigDebugFullVaultInspector())
            {
                var dbgTab = CreateButton(tabs.transform, "Tab_Debug", "Debug", () =>
                {
                    _settingsRoot?.SetActive(false);
                    _debugDumpMode = true;
                    _selectedCurrencyId = "";
                    RefreshList();
                    RefreshCategoryTabStyles();
                }, 0f, 40f, ColGhostBtn, ColPrimaryBtnHi);
                var dbgLe = dbgTab.GetComponent<LayoutElement>();
                dbgLe.flexibleWidth = 0.55f;
                dbgLe.minHeight = 44f;
            }

            var setTab = CreateButton(tabs.transform, "Tab_Settings", "Settings", () => ToggleSettings(), 0f, 40f, ColGhostBtn, ColPrimaryBtnHi);
            var setTabLe = setTab.GetComponent<LayoutElement>();
            setTabLe.flexibleWidth = 0.45f;
            setTabLe.minHeight = 44f;

            var scrollGo = CreateChild(panel.transform, "Scroll", out var scrollRt);
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            // Bottom clears taller footer; top clears taller tab row.
            scrollRt.offsetMin = new Vector2(pad, 262f);
            scrollRt.offsetMax = new Vector2(-pad, -122f);

            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = ColPanelDeep;
            _scroll = scrollGo.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 24f;

            var viewport = CreateChild(scrollGo.transform, "Viewport", out var vpRt);
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(3f, 3f);
            vpRt.offsetMax = new Vector2(-3f, -3f);
            viewport.AddComponent<RectMask2D>();
            var vpGraphic = viewport.AddComponent<Image>();
            vpGraphic.color = new Color(0, 0, 0, 0.001f);
            vpGraphic.raycastTarget = true;

            var content = CreateChild(viewport.transform, "Content", out var contentRt);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.anchoredPosition = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(2, 2, 4, 4);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _listContent = content.transform;
            _scroll.viewport = vpRt;
            _scroll.content = contentRt;

            var footer = CreateChild(panel.transform, "Footer", out var footerRt);
            footerRt.anchorMin = new Vector2(0, 0);
            footerRt.anchorMax = new Vector2(1, 0);
            footerRt.pivot = new Vector2(0.5f, 0);
            footerRt.offsetMin = new Vector2(pad, pad);
            footerRt.offsetMax = new Vector2(-pad, 252f);

            var footerBg = footer.AddComponent<Image>();
            footerBg.color = new Color(0.09f, 0.1f, 0.14f, 0.92f);

            _statusText = CreateText(footer.transform, "Status", "", 13, FontStyle.Normal, ColSubtext);
            _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _statusText.verticalOverflow = VerticalWrapMode.Truncate;
            var stRt = _statusText.rectTransform;
            stRt.anchorMin = new Vector2(0, 1);
            stRt.anchorMax = new Vector2(1, 1);
            stRt.pivot = new Vector2(0.5f, 1f);
            stRt.offsetMin = new Vector2(10f, -34f);
            stRt.offsetMax = new Vector2(-10f, -6f);

            _selectionLabel = CreateText(footer.transform, "Selection", "Select a currency row", 14, FontStyle.Bold, ColGold);
            _selectionLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            _selectionLabel.verticalOverflow = VerticalWrapMode.Truncate;
            var selRt = _selectionLabel.rectTransform;
            selRt.anchorMin = new Vector2(0, 1);
            selRt.anchorMax = new Vector2(1, 1);
            selRt.pivot = new Vector2(0.5f, 1f);
            selRt.offsetMin = new Vector2(10f, -66f);
            selRt.offsetMax = new Vector2(-10f, -38f);

            var rowBtns = CreateChild(footer.transform, "RowBtns", out var rowBtRt);
            // Top-anchored so this band cannot overlap selection (was bottom-anchored into same Y range).
            rowBtRt.anchorMin = new Vector2(0, 1);
            rowBtRt.anchorMax = new Vector2(1, 1);
            rowBtRt.pivot = new Vector2(0.5f, 1f);
            rowBtRt.offsetMin = new Vector2(8f, -124f);
            rowBtRt.offsetMax = new Vector2(-8f, -72f);
            var h = rowBtns.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            var amountHolder = CreateChild(rowBtns.transform, "Amount", out var amtRt);
            amtRt.sizeDelta = new Vector2(96f, 36f);
            var amtLe = amountHolder.AddComponent<LayoutElement>();
            amtLe.minWidth = 72f;
            amtLe.preferredWidth = 96f;
            amtLe.flexibleWidth = 0f;
            var amtImg = amountHolder.AddComponent<Image>();
            amtImg.color = new Color(0.12f, 0.14f, 0.2f, 1f);
            var amtOutline = amountHolder.AddComponent<Outline>();
            amtOutline.effectColor = new Color(ColAccentStrip.r, ColAccentStrip.g, ColAccentStrip.b, 0.25f);
            amtOutline.effectDistance = new Vector2(1f, -1f);
            _amountField = amountHolder.AddComponent<InputField>();
            var amtText = CreateText(amountHolder.transform, "Text", "1", 16, FontStyle.Normal, Color.white);
            amtText.supportRichText = false;
            amtText.alignment = TextAnchor.MiddleLeft;
            var amtRt2 = amtText.rectTransform;
            amtRt2.anchorMin = new Vector2(0, 0);
            amtRt2.anchorMax = new Vector2(1, 1);
            amtRt2.offsetMin = new Vector2(10f, 4f);
            amtRt2.offsetMax = new Vector2(-10f, -4f);
            _amountField.textComponent = amtText;
            _amountField.lineType = InputField.LineType.SingleLine;

            var wBtn = CreateButton(rowBtns.transform, "Withdraw", "Withdraw", DoWithdraw, 76f, 36f, ColPrimaryBtn, ColPrimaryBtnHi);
            var dBtn = CreateButton(rowBtns.transform, "Deposit", "Deposit", DoDeposit, 76f, 36f, ColPrimaryBtn, ColPrimaryBtnHi);
            var wLe = wBtn.GetComponent<LayoutElement>();
            wLe.flexibleWidth = 1f;
            wLe.minWidth = 72f;
            var dLe = dBtn.GetComponent<LayoutElement>();
            dLe.flexibleWidth = 1f;
            dLe.minWidth = 72f;

            var sweepRow = CreateChild(footer.transform, "Sweep", out var swRt);
            swRt.anchorMin = new Vector2(0, 1);
            swRt.anchorMax = new Vector2(1, 1);
            swRt.pivot = new Vector2(0.5f, 1f);
            swRt.offsetMin = new Vector2(8f, -178f);
            swRt.offsetMax = new Vector2(-8f, -132f);
            var sweepH = sweepRow.AddComponent<HorizontalLayoutGroup>();
            sweepH.childAlignment = TextAnchor.MiddleCenter;
            sweepH.childForceExpandWidth = true;
            var sweepBtn = CreateButton(sweepRow.transform, "SweepBtn", "Sweep inventory → vault (auto-deposit)", () =>
            {
                ItemPatches.ForceAutoDepositAll();
                SetStatus("Sweep triggered.", false);
            }, 0f, 34f, ColGhostBtn, ColPrimaryBtnHi);
            sweepBtn.GetComponent<LayoutElement>().minHeight = 34f;
            sweepBtn.GetComponent<LayoutElement>().flexibleWidth = 1f;

            _settingsRoot = CreateChild(panel.transform, "Settings", out var setRt).gameObject;
            _settingsRoot.SetActive(false);
            setRt.anchorMin = Vector2.zero;
            setRt.anchorMax = Vector2.one;
            setRt.offsetMin = new Vector2(pad, 262f);
            setRt.offsetMax = new Vector2(-pad, -122f);
            var setBg = _settingsRoot.AddComponent<Image>();
            setBg.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);
            var setOutline = _settingsRoot.AddComponent<Outline>();
            setOutline.effectColor = new Color(0.4f, 0.6f, 0.85f, 0.2f);
            setOutline.effectDistance = new Vector2(1f, -1f);
            BuildSettingsContent(_settingsRoot.transform);

            UpdatePanelScale();
            RefreshCategoryTabStyles();
        }

        private GameObject CreateStyledTabButton(Transform parent, string name, string label, Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = ColTabNormal;
            var btn = go.AddComponent<UnityEngine.UI.Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var t = CreateText(go.transform, "Label", label, 13, FontStyle.Bold, ColTitle);
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4f, 2f);
            rt.offsetMax = new Vector2(-4f, -2f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 8;
            t.resizeTextMaxSize = 12;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 44f;
            le.preferredHeight = 44f;
            return go;
        }

        private void RefreshCategoryTabStyles()
        {
            bool muted = (_settingsRoot != null && _settingsRoot.activeSelf) || _debugDumpMode;
            foreach (var t in _categoryTabs)
            {
                if (t.Background == null) continue;
                if (muted)
                    t.Background.color = ColTabMuted;
                else if (t.Category == _category)
                    t.Background.color = ColTabSelected;
                else
                    t.Background.color = ColTabNormal;
            }
        }

        private void BuildSettingsContent(Transform parent)
        {
            var v = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(14, 14, 14, 14);
            v.spacing = 10f;
            v.childForceExpandWidth = true;

            CreateText(parent, "Hint", "Settings are saved to the BepInEx config file. Window / HUD controls match the legacy UI.", 12, FontStyle.Italic, ColSubtext)
                .horizontalOverflow = HorizontalWrapMode.Wrap;

            CreateButton(parent, "WinScaleDown", "Window scale −", () =>
            {
                float s = Plugin.GetConfigWindowScale() - 0.1f;
                Plugin.SetConfigWindowScale(s);
                Plugin.Instance?.ApplyVaultWindowScaleToUi();
            }, 0f, 36f, ColGhostBtn, ColPrimaryBtnHi).GetComponent<LayoutElement>().minHeight = 36f;

            CreateButton(parent, "WinScaleUp", "Window scale +", () =>
            {
                float s = Plugin.GetConfigWindowScale() + 0.1f;
                Plugin.SetConfigWindowScale(s);
                Plugin.Instance?.ApplyVaultWindowScaleToUi();
            }, 0f, 36f, ColGhostBtn, ColPrimaryBtnHi).GetComponent<LayoutElement>().minHeight = 36f;

            CreateButton(parent, "HudToggle", "Toggle HUD on/off", () =>
            {
                Plugin.SetConfigHUDEnabled(!Plugin.GetConfigHUDEnabled());
            }, 0f, 36f, ColGhostBtn, ColPrimaryBtnHi).GetComponent<LayoutElement>().minHeight = 36f;

            CreateButton(parent, "HudNormal", "HUD density: Normal", () => Plugin.SetConfigHudDensity("Normal"), 0f, 34f, ColGhostBtn, ColPrimaryBtnHi);
            CreateButton(parent, "HudCompact", "HUD density: Compact", () => Plugin.SetConfigHudDensity("Compact"), 0f, 34f, ColGhostBtn, ColPrimaryBtnHi);
            CreateButton(parent, "HudMin", "HUD density: Minimal", () => Plugin.SetConfigHudDensity("Minimal"), 0f, 34f, ColGhostBtn, ColPrimaryBtnHi);
        }

        private void ToggleSettings()
        {
            if (_settingsRoot == null) return;
            _debugDumpMode = false;
            _settingsRoot.SetActive(!_settingsRoot.activeSelf);
            if (!_settingsRoot.activeSelf) RefreshList();
            RefreshCategoryTabStyles();
        }

        private void ShowDebugDump()
        {
            ClearList();
            if (_vaultManager == null) return;
            var lines = _vaultManager.BuildDebugVaultReport();
            int i = 0;
            foreach (var line in lines)
            {
                if (i++ > 200) break;
                var row = CreateChild(_listContent, "Dbg" + i, out _);
                var t = CreateText(row.transform, "t", line, 11, FontStyle.Normal, ColSubtext);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                var le = row.AddComponent<LayoutElement>();
                le.minHeight = 20f;
            }
            RefreshCategoryTabStyles();
        }

        private void SelectTab(CurrencyCategory c)
        {
            _settingsRoot?.SetActive(false);
            _debugDumpMode = false;
            _category = c;
            _selectedCurrencyId = "";
            RefreshCategoryTabStyles();
            UpdatePanelScale();
            RefreshList();
        }

        private static string GetTabLabel(CurrencyCategory c) => c switch
        {
            CurrencyCategory.SeasonalToken => "Seasonal",
            CurrencyCategory.Key => "Keys",
            CurrencyCategory.Special => "Special",
            CurrencyCategory.Custom => "Custom",
            _ => c.ToString()
        };

        private void ClearList()
        {
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_listContent.GetChild(i).gameObject);
        }

        private void RefreshList()
        {
            if (!_visible || _listContent == null || _vaultManager == null) return;
            if (_settingsRoot != null && _settingsRoot.activeSelf) return;
            if (_debugDumpMode)
            {
                ShowDebugDump();
                UpdateSelectionLabel();
                return;
            }

            var dict = VaultUiShared.GetCurrenciesForCategory(_vaultManager, _category);
            ClearList();

            if (dict.Count == 0)
            {
                var empty = CreateChild(_listContent, "Empty", out _);
                var et = CreateText(empty.transform, "e", GetEmptyMessage(_category), 14, FontStyle.Italic, ColSubtext);
                et.horizontalOverflow = HorizontalWrapMode.Wrap;
                empty.AddComponent<LayoutElement>().minHeight = 56f;
                return;
            }

            bool even = true;
            foreach (var kvp in dict)
            {
                string id = kvp.Key;
                int amount = kvp.Value;
                var row = CreateChild(_listContent, "Row_" + id, out _);
                var le = row.AddComponent<LayoutElement>();
                le.minHeight = 56f;
                le.minWidth = 0f;
                le.preferredWidth = 0f;
                le.flexibleWidth = 1f;

                bool selected = id == _selectedCurrencyId;
                var bg = row.AddComponent<Image>();
                bg.color = selected ? ColRowSel : (even ? ColRowA : ColRowB);
                even = !even;

                // Manual anchored layout so nothing can overflow the viewport.
                const float PadL = 8f;
                const float PadR = 8f;
                const float IconW = 26f;
                const float Gap = 8f;
                const float AmtW = 56f;
                const float QuickW = 28f;

                bool autoOn = ItemPatches.IsAutoDepositEnabled(id);
                var adGo = CreateButton(row.transform, "Ad", autoOn ? "A✓" : "A", () =>
                {
                    ItemPatches.ToggleAutoDeposit(id);
                    RefreshList();
                }, 26f, 26f, ColGhostBtn, ColPrimaryBtnHi);
                var adRt = adGo.GetComponent<RectTransform>();
                adRt.anchorMin = new Vector2(0, 0.5f);
                adRt.anchorMax = new Vector2(0, 0.5f);
                adRt.pivot = new Vector2(0, 0.5f);
                adRt.anchoredPosition = new Vector2(PadL, 0);

                var iconHolder = CreateChild(row.transform, "Icon", out var iconRt);
                iconRt.anchorMin = new Vector2(0, 0.5f);
                iconRt.anchorMax = new Vector2(0, 0.5f);
                iconRt.pivot = new Vector2(0, 0.5f);
                iconRt.sizeDelta = new Vector2(IconW, IconW);
                iconRt.anchoredPosition = new Vector2(PadL + 26f + Gap, 0);
                var iconOutline = iconHolder.AddComponent<Outline>();
                iconOutline.effectColor = new Color(0f, 0f, 0f, 0.35f);
                iconOutline.effectDistance = new Vector2(1f, -1f);
                var raw = iconHolder.AddComponent<RawImage>();
                if (IconCache.IsIconLoaded(id))
                    raw.texture = IconCache.GetIconForCurrency(id);
                else
                {
                    raw.texture = Texture2D.whiteTexture;
                    raw.color = new Color(0.28f, 0.3f, 0.38f, 0.45f);
                }

                string name = VaultUiShared.GetDisplayName(_vaultManager, id);
                var nameT = CreateText(row.transform, "Name", name, 12, FontStyle.Normal, Color.white);
                nameT.horizontalOverflow = HorizontalWrapMode.Overflow;
                nameT.verticalOverflow = VerticalWrapMode.Truncate;
                nameT.alignment = TextAnchor.MiddleLeft;
                nameT.resizeTextForBestFit = true;
                nameT.resizeTextMinSize = 8;
                nameT.resizeTextMaxSize = 12;
                var nameRt = nameT.rectTransform;
                nameRt.anchorMin = new Vector2(0, 0);
                nameRt.anchorMax = new Vector2(1, 1);
                nameRt.pivot = new Vector2(0, 0.5f);
                float left = PadL + 26f + Gap + IconW + Gap;
                float right = PadR + QuickW + Gap + AmtW + Gap;
                nameRt.offsetMin = new Vector2(left, 0);
                nameRt.offsetMax = new Vector2(-right, 0);

                var amtT = CreateText(row.transform, "Amt", "×" + VaultUiShared.FormatNumber(amount), 12, FontStyle.Bold, ColGold);
                amtT.horizontalOverflow = HorizontalWrapMode.Overflow;
                amtT.verticalOverflow = VerticalWrapMode.Truncate;
                amtT.alignment = TextAnchor.MiddleRight;
                amtT.resizeTextForBestFit = true;
                amtT.resizeTextMinSize = 8;
                amtT.resizeTextMaxSize = 12;
                var amtRt = amtT.rectTransform;
                amtRt.anchorMin = new Vector2(1, 0);
                amtRt.anchorMax = new Vector2(1, 1);
                amtRt.pivot = new Vector2(1, 0.5f);
                amtRt.sizeDelta = new Vector2(AmtW, 0);
                amtRt.anchoredPosition = new Vector2(-(PadR + QuickW + Gap), 0);

                var qb = CreateButton(row.transform, "w1", "−1", () => Withdraw(id, 1), QuickW, 26f, ColGhostBtn, ColPrimaryBtnHi);
                var qbRt = qb.GetComponent<RectTransform>();
                qbRt.anchorMin = new Vector2(1, 0.5f);
                qbRt.anchorMax = new Vector2(1, 0.5f);
                qbRt.pivot = new Vector2(1, 0.5f);
                qbRt.anchoredPosition = new Vector2(-PadR, 0);

                var btn = row.AddComponent<UnityEngine.UI.Button>();
                btn.targetGraphic = bg;
                var cb = btn.colors;
                cb.colorMultiplier = 1f;
                btn.colors = cb;
                btn.onClick.AddListener(() =>
                {
                    _selectedCurrencyId = id;
                    UpdateSelectionLabel();
                    RefreshList();
                });
            }

            UpdateSelectionLabel();
            Canvas.ForceUpdateCanvases();
        }

        private void UpdateSelectionLabel()
        {
            if (_selectionLabel == null) return;
            if (string.IsNullOrEmpty(_selectedCurrencyId))
                _selectionLabel.text = "Selected: —  (tap a row)";
            else
                _selectionLabel.text = "Selected: " + VaultUiShared.GetDisplayName(_vaultManager, _selectedCurrencyId);
        }

        private static string GetEmptyMessage(CurrencyCategory c) => c switch
        {
            CurrencyCategory.Custom => "No custom currencies. Mods register via VaultIntegration.RegisterCustomCurrency.",
            CurrencyCategory.SeasonalToken => "No seasonal tokens in the vault.",
            CurrencyCategory.Key => "No keys stored in the vault.",
            CurrencyCategory.Special => "No special currencies in the vault.",
            _ => "Nothing in this category."
        };

        private void Withdraw(string id, int amt)
        {
            int have = _vaultManager.GetCurrency(id);
            if (have < amt)
            {
                SetStatus($"Not enough (have {have}).", true);
                return;
            }
            VaultInventoryOperations.WithdrawToInventory(_vaultManager, id, amt, SetStatus);
            RefreshList();
        }

        private void DoWithdraw()
        {
            if (string.IsNullOrEmpty(_selectedCurrencyId))
            {
                SetStatus("Select a row first.", true);
                return;
            }
            if (!int.TryParse(_amountField?.text, out int n) || n <= 0)
            {
                SetStatus("Enter a positive amount.", true);
                return;
            }
            Withdraw(_selectedCurrencyId, n);
        }

        private void DoDeposit()
        {
            if (string.IsNullOrEmpty(_selectedCurrencyId))
            {
                SetStatus("Select a row first.", true);
                return;
            }
            if (!int.TryParse(_amountField?.text, out int n) || n <= 0)
            {
                SetStatus("Enter a positive amount.", true);
                return;
            }
            VaultInventoryOperations.DepositFromInventory(_vaultManager, _selectedCurrencyId, n, SetStatus);
            RefreshList();
        }

        private void SetStatus(string msg, bool err)
        {
            if (_statusText != null)
            {
                _statusText.text = msg;
                _statusText.color = err ? new Color(1f, 0.48f, 0.48f, 1f) : new Color(0.52f, 0.92f, 0.62f, 1f);
            }
        }

        private static GameObject CreateChild(Transform parent, string name, out RectTransform rt)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            rt = go.AddComponent<RectTransform>();
            rt.localScale = Vector3.one;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle fs, Color col)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = value;
            t.fontSize = size;
            t.fontStyle = fs;
            t.color = col;
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var rt = go.GetComponent<RectTransform>();
            // Important: do NOT force a huge width for layout-driven text.
            // Layout groups + LayoutElement min/preferred widths should determine the width.
            rt.sizeDelta = new Vector2(0f, 28f);
            return t;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Action onClick, float width, float height, Color normal, Color highlighted)
         {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = normal;
            var btn = go.AddComponent<UnityEngine.UI.Button>();
            img.color = Color.white;
            var colors = btn.colors;
            colors.fadeDuration = 0.08f;
            colors.normalColor = normal;
            colors.highlightedColor = Color.Lerp(normal, highlighted, 0.45f);
            colors.pressedColor = new Color(normal.r * 0.85f, normal.g * 0.85f, normal.b * 0.85f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.45f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var t = CreateText(go.transform, "Label", label, 13, FontStyle.Bold, Color.white);
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6f, 2f);
            rt.offsetMax = new Vector2(-6f, -2f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 7;
            t.resizeTextMaxSize = 13;
            var rtBtn = go.GetComponent<RectTransform>();
            if (width > 0f)
                rtBtn.sizeDelta = new Vector2(width, height);
            var le = go.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                le.minWidth = width;
                le.preferredWidth = width;
            }
            le.minHeight = height;
            le.preferredHeight = height;
            return go;
        }
    }
}
