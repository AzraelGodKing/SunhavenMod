using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CropOptimizer.UI
{
    /// <summary>
    /// Sun Haven-styled uGUI HUD for the Crop Optimizer summary panel.
    /// Wood frame + parchment body + pixel-y icons, with drag-to-move on the header.
    /// </summary>
    internal sealed class CropHudView
    {
        private readonly Transform _canvasRoot;
        private GameObject _panel;
        private RectTransform _panelRt;
        private TMP_Text _trackedLabel;
        private TMP_Text _valueLabel;
        private DragHandle _dragHandle;
        private Image _tooltipToggleIcon;
        private TooltipToggleButton _tooltipToggle;

        public event Action<float, float> PlacementChanged;

        /// <summary>Fired when the user clicks the tooltip toggle in the header.</summary>
        public event Action TooltipToggleClicked;

        public RectTransform Rect => _panelRt;
        public bool IsVisible => _panel != null && _panel.activeSelf;

        public CropHudView(Transform canvasRoot)
        {
            _canvasRoot = canvasRoot;
            Build();
        }

        public void SetVisible(bool visible)
        {
            if (_panel == null) Build();
            if (_panel != null) _panel.SetActive(visible);
        }

        public void SetPlacement(float x, float y)
        {
            if (_panelRt == null) Build();
            if (_panelRt != null) _panelRt.anchoredPosition = new Vector2(x, -y);
        }

        public void SetScale(float scale)
        {
            if (_panelRt == null) Build();
            if (_panelRt != null) _panelRt.localScale = new Vector3(scale, scale, 1f);
        }

        public void UpdateStats(int trackedCount, long projectedGold)
        {
            if (_trackedLabel == null) return;
            _trackedLabel.text = $"Tracked crops: <b>{trackedCount}</b>";
            _valueLabel.text = $"Projected sell: <color=#F7D982><b>{projectedGold:N0}g</b></color>";
        }

        /// <summary>Reflects the tooltip-enabled config value on the toggle button (icon + color + hover label).</summary>
        public void SetTooltipEnabled(bool enabled)
        {
            if (_tooltipToggleIcon == null) return;
            _tooltipToggleIcon.sprite = UiStyle.GetIcon(enabled ? UiStyle.IconKind.Tooltip : UiStyle.IconKind.TooltipOff);
            _tooltipToggleIcon.color = enabled ? UiStyle.HeaderGold : UiStyle.TextSub;
            if (_tooltipToggle != null)
                _tooltipToggle.SetTooltipLabel(enabled
                    ? "Toggle Crop Tooltips <color=#B8A078>(on)</color>"
                    : "Toggle Crop Tooltips <color=#B8A078>(off)</color>");
        }

        public void Destroy()
        {
            if (_panel != null)
            {
                UnityEngine.Object.Destroy(_panel);
                _panel = null;
                _panelRt = null;
                _trackedLabel = null;
                _valueLabel = null;
                _dragHandle = null;
            }
        }

        private void Build()
        {
            if (_canvasRoot == null) return;

            // Outer panel = the wood-colored border. Inner panel (child, inset 3px on each side)
            // = the near-black fill. Nothing else renders a background, so there are no 9-slice
            // artifacts to worry about.
            _panel = new GameObject("CropOptimizer_Hud", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_canvasRoot, false);
            _panelRt = (RectTransform)_panel.transform;
            _panelRt.anchorMin = new Vector2(0f, 1f);
            _panelRt.anchorMax = new Vector2(0f, 1f);
            _panelRt.pivot = new Vector2(0f, 1f);
            _panelRt.anchoredPosition = new Vector2(24f, -80f);
            _panelRt.sizeDelta = new Vector2(320f, 96f);

            var border = _panel.GetComponent<Image>();
            border.sprite = UiStyle.SolidSprite;
            border.type = Image.Type.Simple;
            border.color = UiStyle.WoodBorder;
            border.raycastTarget = true; // keeps drag-catching on the whole panel

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(_panel.transform, false);
            var frt = (RectTransform)fill.transform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(3f, 3f); frt.offsetMax = new Vector2(-3f, -3f);
            var fillImg = fill.GetComponent<Image>();
            fillImg.sprite = UiStyle.SolidSprite;
            fillImg.color = UiStyle.PanelFill;
            fillImg.raycastTarget = false;

            // Header band (darker strip at the top of the fill, acts as drag handle)
            var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(fill.transform, false);
            var hrt = (RectTransform)header.transform;
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = Vector2.zero;
            hrt.sizeDelta = new Vector2(0f, 30f);

            var himg = header.GetComponent<Image>();
            himg.sprite = UiStyle.SolidSprite;
            himg.color = UiStyle.HeaderFill;

            _dragHandle = header.AddComponent<DragHandle>();
            _dragHandle.Target = _panelRt;
            _dragHandle.Moved += (x, y) => PlacementChanged?.Invoke(x, -y);

            // Divider under the header
            var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(fill.transform, false);
            var drt = (RectTransform)divider.transform;
            drt.anchorMin = new Vector2(0f, 1f);
            drt.anchorMax = new Vector2(1f, 1f);
            drt.pivot = new Vector2(0.5f, 1f);
            drt.anchoredPosition = new Vector2(0f, -30f);
            drt.sizeDelta = new Vector2(0f, 1f);
            var dimg = divider.GetComponent<Image>();
            dimg.sprite = UiStyle.SolidSprite;
            dimg.color = UiStyle.WoodBorderLit;
            dimg.raycastTarget = false;

            var headerIcon = CreateIcon(header.transform, UiStyle.IconKind.Sprout, UiStyle.Sprout, 20f);
            var hicRt = headerIcon.rectTransform;
            hicRt.anchorMin = new Vector2(0f, 0.5f);
            hicRt.anchorMax = new Vector2(0f, 0.5f);
            hicRt.pivot = new Vector2(0f, 0.5f);
            hicRt.anchoredPosition = new Vector2(10f, 0f);

            var title = CreateText(header.transform, "Crop Optimizer", 16f, FontStyles.Bold, UiStyle.HeaderGold, TextAlignmentOptions.MidlineLeft);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(36f, 0f); trt.offsetMax = new Vector2(-36f, 0f);

            // Tooltip toggle button (right side of header)
            var btn = new GameObject("TooltipToggle", typeof(RectTransform), typeof(Image));
            btn.transform.SetParent(header.transform, false);
            var brtn = (RectTransform)btn.transform;
            brtn.anchorMin = new Vector2(1f, 0.5f);
            brtn.anchorMax = new Vector2(1f, 0.5f);
            brtn.pivot = new Vector2(1f, 0.5f);
            brtn.anchoredPosition = new Vector2(-8f, 0f);
            brtn.sizeDelta = new Vector2(24f, 24f);
            var btnBg = btn.GetComponent<Image>();
            btnBg.sprite = UiStyle.SolidSprite;
            btnBg.color = new Color32(0, 0, 0, 0);
            btnBg.raycastTarget = true;

            _tooltipToggleIcon = CreateIcon(btn.transform, UiStyle.IconKind.Tooltip, UiStyle.HeaderGold, 16f);
            var tiRt = _tooltipToggleIcon.rectTransform;
            tiRt.anchorMin = new Vector2(0.5f, 0.5f);
            tiRt.anchorMax = new Vector2(0.5f, 0.5f);
            tiRt.pivot = new Vector2(0.5f, 0.5f);
            tiRt.anchoredPosition = Vector2.zero;
            _tooltipToggleIcon.raycastTarget = false;

            _tooltipToggle = btn.AddComponent<TooltipToggleButton>();
            _tooltipToggle.Background = btnBg;
            _tooltipToggle.NormalColor = new Color32(0, 0, 0, 0);
            _tooltipToggle.HoverColor = new Color32(0xB0, 0x7A, 0x3E, 0x60);
            _tooltipToggle.PressedColor = new Color32(0xB0, 0x7A, 0x3E, 0xA0);
            _tooltipToggle.Clicked += () => TooltipToggleClicked?.Invoke();

            // Body container (2 rows)
            var body = new GameObject("Body", typeof(RectTransform));
            body.transform.SetParent(fill.transform, false);
            var brt = (RectTransform)body.transform;
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = new Vector2(14f, 8f);
            brt.offsetMax = new Vector2(-14f, -36f);

            _trackedLabel = AddRow(body.transform, UiStyle.IconKind.Sprout, UiStyle.Sprout, "Tracked crops: 0", 0);
            _valueLabel = AddRow(body.transform, UiStyle.IconKind.Coin, UiStyle.Coin, "Projected sell: 0g", 1);
        }

        private TMP_Text AddRow(Transform parent, UiStyle.IconKind icon, Color32 iconColor, string initialText, int rowIndex)
        {
            var row = new GameObject($"Row_{icon}", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rrt = (RectTransform)row.transform;
            rrt.anchorMin = new Vector2(0f, 1f);
            rrt.anchorMax = new Vector2(1f, 1f);
            rrt.pivot = new Vector2(0f, 1f);
            rrt.anchoredPosition = new Vector2(0f, -rowIndex * 24f);
            rrt.sizeDelta = new Vector2(0f, 22f);

            var ic = CreateIcon(row.transform, icon, iconColor, 18f);
            var irt = ic.rectTransform;
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(0f, 0f);

            var t = CreateText(row.transform, initialText, 15f, FontStyles.Normal, UiStyle.TextDark, TextAlignmentOptions.MidlineLeft);
            var trt = t.rectTransform;
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(26f, 0f);
            trt.offsetMax = new Vector2(-2f, 0f);

            return t;
        }

        private static Image CreateIcon(Transform parent, UiStyle.IconKind kind, Color32 color, float size)
        {
            var go = new GameObject($"Icon_{kind}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = UiStyle.GetIcon(kind);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TMP_Text CreateText(Transform parent, string text, float size, FontStyles style, Color32 color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            t.richText = true;
            if (UiStyle.Font != null) t.font = UiStyle.Font;
            return t;
        }

        /// <summary>
        /// Tiny click-only button with hover/pressed background color feedback plus a small
        /// floating label that pops up on hover. The label lives as a child GameObject of the
        /// button itself — deliberately separate from <see cref="CropTooltipView"/>, so it is
        /// always shown on hover regardless of the `HUD.HoverTooltip` crop-tooltip toggle.
        /// </summary>
        internal sealed class TooltipToggleButton : MonoBehaviour,
            IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
        {
            public Image Background;
            public Color32 NormalColor;
            public Color32 HoverColor;
            public Color32 PressedColor;

            public event Action Clicked;

            private GameObject _hoverLabelRoot;
            private TMP_Text _hoverLabelText;
            private string _pendingLabel = "Toggle Crop Tooltips";
            private bool _hovered;
            private bool _pressed;

            public void SetTooltipLabel(string label)
            {
                _pendingLabel = label ?? string.Empty;
                if (_hoverLabelText != null)
                {
                    _hoverLabelText.text = _pendingLabel;
                    ResizeLabelToContent();
                }
            }

            public void OnPointerEnter(PointerEventData e) { _hovered = true; EnsureHoverLabel(); SetLabelVisible(true); Refresh(); }
            public void OnPointerExit(PointerEventData e) { _hovered = false; _pressed = false; SetLabelVisible(false); Refresh(); }
            public void OnPointerDown(PointerEventData e) { _pressed = true; Refresh(); }
            public void OnPointerUp(PointerEventData e) { _pressed = false; Refresh(); }

            public void OnPointerClick(PointerEventData e)
            {
                if (e.button != PointerEventData.InputButton.Left) return;
                Clicked?.Invoke();
            }

            private void Refresh()
            {
                if (Background == null) return;
                Background.color = _pressed ? PressedColor : (_hovered ? HoverColor : NormalColor);
            }

            private void EnsureHoverLabel()
            {
                if (_hoverLabelRoot != null) return;

                _hoverLabelRoot = new GameObject("HoverLabel", typeof(RectTransform), typeof(Image));
                _hoverLabelRoot.transform.SetParent(transform, false);
                var rt = (RectTransform)_hoverLabelRoot.transform;
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -6f);
                rt.sizeDelta = new Vector2(140f, 22f);

                var border = _hoverLabelRoot.GetComponent<Image>();
                border.sprite = UiStyle.SolidSprite;
                border.color = UiStyle.WoodBorder;
                border.raycastTarget = false;

                var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fill.transform.SetParent(_hoverLabelRoot.transform, false);
                var frt = (RectTransform)fill.transform;
                frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
                frt.offsetMin = new Vector2(2f, 2f); frt.offsetMax = new Vector2(-2f, -2f);
                var fimg = fill.GetComponent<Image>();
                fimg.sprite = UiStyle.SolidSprite;
                fimg.color = UiStyle.PanelFill;
                fimg.raycastTarget = false;

                var textGo = new GameObject("Text", typeof(RectTransform));
                textGo.transform.SetParent(_hoverLabelRoot.transform, false);
                _hoverLabelText = textGo.AddComponent<TextMeshProUGUI>();
                _hoverLabelText.alignment = TextAlignmentOptions.Center;
                _hoverLabelText.fontSize = 12f;
                _hoverLabelText.color = UiStyle.HeaderGold;
                _hoverLabelText.enableWordWrapping = false;
                _hoverLabelText.overflowMode = TextOverflowModes.Overflow;
                _hoverLabelText.raycastTarget = false;
                _hoverLabelText.richText = true;
                if (UiStyle.Font != null) _hoverLabelText.font = UiStyle.Font;
                _hoverLabelText.text = _pendingLabel;

                var trt = _hoverLabelText.rectTransform;
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(6f, 0f);
                trt.offsetMax = new Vector2(-6f, 0f);

                ResizeLabelToContent();
                _hoverLabelRoot.SetActive(false);
            }

            private void ResizeLabelToContent()
            {
                if (_hoverLabelRoot == null || _hoverLabelText == null) return;
                _hoverLabelText.ForceMeshUpdate();
                float w = Mathf.Max(80f, _hoverLabelText.preferredWidth + 16f);
                var rt = (RectTransform)_hoverLabelRoot.transform;
                rt.sizeDelta = new Vector2(w, 22f);
            }

            private void SetLabelVisible(bool visible)
            {
                if (_hoverLabelRoot == null) return;
                _hoverLabelRoot.SetActive(visible);
                if (visible) _hoverLabelRoot.transform.SetAsLastSibling();
            }

            private void OnDisable()
            {
                _hovered = false;
                _pressed = false;
                SetLabelVisible(false);
                Refresh();
            }
        }

        /// <summary>Simple drag handler that moves a target RectTransform when its own GO is dragged.</summary>
        internal sealed class DragHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler
        {
            public RectTransform Target;
            public event Action<float, float> Moved;

            private Vector2 _grabOffset;

            public void OnPointerDown(PointerEventData e)
            {
                if (Target == null) return;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)Target.parent, e.position, e.pressEventCamera, out Vector2 local);
                _grabOffset = Target.anchoredPosition - local;
            }

            public void OnBeginDrag(PointerEventData e)
            {
                OnPointerDown(e);
            }

            public void OnDrag(PointerEventData e)
            {
                if (Target == null) return;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)Target.parent, e.position, e.pressEventCamera, out Vector2 local)) return;
                Target.anchoredPosition = local + _grabOffset;
                Moved?.Invoke(Target.anchoredPosition.x, Target.anchoredPosition.y);
            }
        }
    }
}
