using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HavensRespec.UI
{
    /// <summary>
    /// Minimal uGUI confirmation modal that appears on top of the Skills panel.
    /// Built programmatically so the mod does not need any asset bundles.
    /// </summary>
    internal sealed class ConfirmResetDialog : MonoBehaviour
    {
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
        private Action _onConfirm;

        /// <summary>
        /// Build the dialog GameObject tree under <paramref name="parent"/> (usually the canvas
        /// that owns the Skills panel). The dialog starts hidden.
        /// </summary>
        public static ConfirmResetDialog BuildUnder(Transform parent)
        {
            // Must be a RectTransform stretched to the parent canvas — a plain Transform breaks
            // layout for full-screen children and can leave raycasts not matching visuals.
            var go = new GameObject("HavensRespec_ConfirmDialog", typeof(RectTransform));
            var rootRt = go.GetComponent<RectTransform>();
            rootRt.SetParent(parent, false);
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.localScale = Vector3.one;

            // Nested canvas above sibling UI (inventory overlays, other panels) so our scrim and
            // buttons reliably receive pointer events. Parent canvas already has a raycaster; a
            // child canvas needs its own GraphicRaycaster for its draw hierarchy.
            var parentCanvas = parent.GetComponent<Canvas>() ?? parent.GetComponentInParent<Canvas>();
            var overlayCanvas = go.AddComponent<Canvas>();
            if (parentCanvas != null)
            {
                overlayCanvas.renderMode = parentCanvas.renderMode;
                overlayCanvas.worldCamera = parentCanvas.worldCamera;
                overlayCanvas.planeDistance = parentCanvas.planeDistance;
            }
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = parentCanvas != null ? parentCanvas.sortingOrder + 200 : 32000;
            go.AddComponent<GraphicRaycaster>();

            var dialog = go.AddComponent<ConfirmResetDialog>();
            dialog.BuildHierarchy();
            dialog.Hide();
            return dialog;
        }

        public void Show(string title, string body, Action onConfirm)
        {
            _title.text = title ?? "Confirm reset?";
            _body.text = body ?? string.Empty;
            _onConfirm = onConfirm;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _onConfirm = null;
        }

        private void BuildHierarchy()
        {
            // Content is parented directly to this behaviour's RectTransform (full screen).
            // Full-screen scrim — eats clicks so the player cannot click underneath the dialog.
            var scrimGo = new GameObject("Scrim");
            scrimGo.transform.SetParent(transform, false);
            var scrimRt = scrimGo.AddComponent<RectTransform>();
            scrimRt.anchorMin = Vector2.zero;
            scrimRt.anchorMax = Vector2.one;
            scrimRt.offsetMin = Vector2.zero;
            scrimRt.offsetMax = Vector2.zero;
            var scrim = scrimGo.AddComponent<Image>();
            scrim.sprite = RespecStyle.Solid();
            scrim.color = new Color(0f, 0f, 0f, 0.55f);
            scrim.raycastTarget = true;
            var scrimBtn = scrimGo.AddComponent<Button>();
            scrimBtn.transition = Selectable.Transition.None;
            scrimBtn.onClick.AddListener(Hide);

            // Card.
            var cardGo = new GameObject("Card");
            cardGo.transform.SetParent(transform, false);
            var cardRt = cardGo.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(420f, 200f);
            var cardBorder = cardGo.AddComponent<Image>();
            cardBorder.sprite = RespecStyle.SolidRounded(RespecStyle.Wood, RespecStyle.WoodShadow, 28, 3, 8);
            cardBorder.type = Image.Type.Sliced;
            cardBorder.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(cardGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(4f, 4f);
            fillRt.offsetMax = new Vector2(-4f, -4f);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = RespecStyle.SolidRounded(new Color(0.08f, 0.06f, 0.05f, 0.95f), new Color(0.08f, 0.06f, 0.05f, 0.95f), 22, 0, 6);
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = true;

            // Title.
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(fillGo.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 36f);
            titleRt.anchoredPosition = new Vector2(0f, -12f);
            _title = titleGo.AddComponent<TextMeshProUGUI>();
            _title.alignment = TextAlignmentOptions.Center;
            _title.fontSize = 20f;
            _title.fontStyle = FontStyles.Bold;
            _title.color = RespecStyle.AccentGold;
            _title.text = "Confirm reset?";
            _title.raycastTarget = false;

            // Body.
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(fillGo.transform, false);
            var bodyRt = bodyGo.AddComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(18f, 60f);
            bodyRt.offsetMax = new Vector2(-18f, -52f);
            _body = bodyGo.AddComponent<TextMeshProUGUI>();
            _body.alignment = TextAlignmentOptions.TopLeft;
            _body.fontSize = 15f;
            _body.color = new Color(0.95f, 0.92f, 0.83f, 1f);
            _body.text = string.Empty;
            _body.raycastTarget = false;

            // Buttons row.
            BuildButton(fillGo.transform, "Cancel", new Vector2(-10f, 12f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                RespecStyle.Neutral, RespecStyle.NeutralHover, RespecStyle.NeutralPressed, Hide);
            BuildButton(fillGo.transform, "Reset", new Vector2(-130f, 12f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                RespecStyle.Danger, RespecStyle.DangerHover, RespecStyle.DangerPressed, () =>
                {
                    var handler = _onConfirm;
                    Hide();
                    handler?.Invoke();
                });
        }

        private static void BuildButton(
            Transform parent, string label,
            Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Color normal, Color hover, Color pressed, Action onClick)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(110f, 36f);
            rt.anchoredPosition = anchoredPos;

            var image = go.AddComponent<Image>();
            image.sprite = RespecStyle.SolidRounded(Color.white, new Color(0f, 0f, 0f, 0.45f), 20, 1, 5);
            image.type = Image.Type.Sliced;
            image.color = normal;
            image.raycastTarget = true;

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            button.colors = colors;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onClick?.Invoke());

            var hoverHandler = go.AddComponent<ButtonHoverTint>();
            hoverHandler.Target = image;
            hoverHandler.Normal = normal;
            hoverHandler.Hover = hover;
            hoverHandler.Pressed = pressed;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 16f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.text = label;
            tmp.raycastTarget = false;
        }

        /// <summary>Updates button tint on hover / pressed state without regenerating sprites.</summary>
        private sealed class ButtonHoverTint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
        {
            public Image Target;
            public Color Normal;
            public Color Hover;
            public Color Pressed;

            private bool _isOver;

            public void OnPointerEnter(PointerEventData _) { _isOver = true; Apply(Hover); }
            public void OnPointerExit(PointerEventData _) { _isOver = false; Apply(Normal); }
            public void OnPointerDown(PointerEventData _) { Apply(Pressed); }
            public void OnPointerUp(PointerEventData _) { Apply(_isOver ? Hover : Normal); }

            private void Apply(Color fill)
            {
                if (Target == null) return;
                Target.color = fill;
            }
        }
    }
}
