using System;
using SunhavenMods.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wish;

namespace HavensRespec.UI
{
    /// <summary>
    /// Confirmation modal drawn on the Skills canvas.
    /// Sun Haven's skill window accepts clicks on the Reset button, then stops delivering
    /// uGUI clicks to anything created beside that panel. Confirm and Cancel therefore
    /// read the mouse themselves. Shift-click never opens this dialog.
    /// </summary>
    internal sealed class ConfirmResetDialog : MonoBehaviour
    {
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
        private TextMeshProUGUI _cancelLabel;
        private TextMeshProUGUI _confirmLabel;
        private RectTransform _cardRt;
        private RectTransform _cancelRt;
        private RectTransform _confirmRt;
        private Image _cancelImage;
        private Image _confirmImage;
        private Action _onConfirm;
        private string _pendingTitle;
        private string _pendingBody;
        private bool _closeQueued;
        private bool _shown;
        private int _acceptClicksFrame;

        public event Action Dismissed;

        /// <summary>
        /// Build the dialog under <paramref name="parent"/> (the canvas that owns the Skills panel).
        /// The dialog starts hidden.
        /// </summary>
        public static ConfirmResetDialog BuildUnder(Transform parent)
        {
            var go = new GameObject("HavensRespec_ConfirmDialog", typeof(RectTransform));
            var rootRt = go.GetComponent<RectTransform>();
            rootRt.SetParent(parent, false);
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.localScale = Vector3.one;

            // Parent skill-window groups set interactable/blocksRaycasts off for strangers.
            // ignoreParentGroups keeps this modal hittable and keeps it from passing clicks through.
            var group = go.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            group.ignoreParentGroups = true;

            var dialog = go.AddComponent<ConfirmResetDialog>();
            dialog.BuildHierarchy();
            dialog.Hide();
            return dialog;
        }

        public void Show(string title, string body, Action onConfirm)
        {
            _pendingTitle = title ?? ModLocalization.T("respec.dialog.title");
            _pendingBody = body ?? string.Empty;
            _title.text = _pendingTitle;
            _body.text = _pendingBody;
            LayoutCardForBody(_pendingBody);
            _onConfirm = onConfirm;
            _shown = true;
            _closeQueued = false;
            _acceptClicksFrame = Time.frameCount + 1;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
        }

        private void Update()
        {
            if (!_shown)
                return;

            if (CancelPressed())
            {
                Close(runConfirm: false);
                return;
            }

            PaintButtons();

            if (Time.frameCount < _acceptClicksFrame || !PrimaryDown())
                return;

            if (Contains(_confirmRt))
                Close(runConfirm: true);
            else if (Contains(_cancelRt) || !Contains(_cardRt))
                Close(runConfirm: false);
        }

        private void LateUpdate()
        {
            if (!_shown)
                return;

            if (transform.parent != null && transform.GetSiblingIndex() != transform.parent.childCount - 1)
                transform.SetAsLastSibling();

            SuppressTextRaycasts(_title);
            SuppressTextRaycasts(_body);
        }

        public void Hide()
        {
            bool wasShown = _shown;
            _shown = false;
            _closeQueued = false;
            _onConfirm = null;
            _pendingTitle = null;
            _pendingBody = null;
            if (wasShown && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            if (wasShown)
                Dismissed?.Invoke();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Close from Update, not from a uGUI onClick. Hiding a button inside onClick leaves
        /// the game's pointer stuck and the Skills window stops taking input.
        /// </summary>
        private void Close(bool runConfirm)
        {
            if (!_shown || _closeQueued)
                return;

            _closeQueued = true;
            var handler = runConfirm ? _onConfirm : null;
            _onConfirm = null;
            Hide();
            try
            {
                handler?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Haven's Respec] dialog action failed: {ex}");
            }
        }

        private static bool CancelPressed()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                return true;

            try
            {
                if (PlayerInput.GetButtonDown("UICancel"))
                    return true;
                if (PlayerInput.GetButtonDown("Cancel"))
                    return true;
                if (PlayerInput.GetButtonDown("Close"))
                    return true;
            }
            catch (Exception)
            {
                // Rewired has no action by that name.
            }

            return false;
        }

        private static bool PrimaryDown()
        {
            try
            {
                if (PlayerInput.GetMouseButtonDown(0))
                    return true;
            }
            catch (Exception)
            {
                // PlayerInput is not ready; fall through to Unity.
            }

            return Input.GetMouseButtonDown(0);
        }

        private void PaintButtons()
        {
            bool held = false;
            try
            {
                held = PlayerInput.GetMouseButton(0);
            }
            catch (Exception)
            {
                held = Input.GetMouseButton(0);
            }

            Paint(_confirmImage, Contains(_confirmRt), held, RespecStyle.Danger, RespecStyle.DangerHover, RespecStyle.DangerPressed);
            Paint(_cancelImage, Contains(_cancelRt), held, RespecStyle.Neutral, RespecStyle.NeutralHover, RespecStyle.NeutralPressed);
        }

        private static void Paint(Image image, bool over, bool held, Color normal, Color hover, Color pressed)
        {
            if (image == null)
                return;
            image.color = over ? (held ? pressed : hover) : normal;
        }

        private static bool Contains(RectTransform rt)
        {
            if (rt == null || !rt.gameObject.activeInHierarchy)
                return false;

            Vector2 screen = Input.mousePosition;
            try
            {
                if (MouseVisualManager.UsingController)
                    screen = MouseVisualManager.mousePosition;
            }
            catch (Exception)
            {
                screen = Input.mousePosition;
            }

            var canvas = rt.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            if (RectTransformUtility.RectangleContainsScreenPoint(rt, screen, cam))
                return true;

            // Some Sun Haven canvases report overlay while still drawing through a camera.
            if (cam == null && canvas != null && canvas.worldCamera != null)
                return RectTransformUtility.RectangleContainsScreenPoint(rt, screen, canvas.worldCamera);
            if (cam != null)
                return RectTransformUtility.RectangleContainsScreenPoint(rt, screen, null);
            return false;
        }

        private static void SuppressTextRaycasts(Graphic graphic)
        {
            if (graphic == null)
                return;
            graphic.raycastTarget = false;
            var graphics = graphic.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].raycastTarget = false;
        }

        public void RefreshLocalizedLabels()
        {
            if (_title != null && gameObject.activeSelf && !string.IsNullOrEmpty(_pendingTitle))
                _title.text = _pendingTitle;
            if (_body != null && gameObject.activeSelf)
            {
                _body.text = _pendingBody ?? string.Empty;
                LayoutCardForBody(_pendingBody ?? string.Empty);
            }
            if (_cancelLabel != null)
                _cancelLabel.text = ModLocalization.T("respec.dialog.cancel");
            if (_confirmLabel != null)
                _confirmLabel.text = ModLocalization.T("respec.dialog.confirm");
        }

        private void BuildHierarchy()
        {
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

            var cardGo = new GameObject("Card");
            cardGo.transform.SetParent(transform, false);
            var cardRt = cardGo.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(CardWidth, DefaultCardHeight);
            _cardRt = cardRt;
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
            _title.text = ModLocalization.T("respec.dialog.title");
            _title.raycastTarget = false;

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(fillGo.transform, false);
            var bodyRt = bodyGo.AddComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(18f, ButtonFooterHeight);
            bodyRt.offsetMax = new Vector2(-18f, -TitleAreaHeight);
            _body = bodyGo.AddComponent<TextMeshProUGUI>();
            _body.alignment = TextAlignmentOptions.TopLeft;
            _body.fontSize = 15f;
            _body.lineSpacing = 2f;
            _body.enableWordWrapping = true;
            _body.color = new Color(0.95f, 0.92f, 0.83f, 1f);
            _body.text = string.Empty;
            _body.raycastTarget = false;

            BuildButton(fillGo.transform, ModLocalization.T("respec.dialog.cancel"), new Vector2(-14f, 14f),
                RespecStyle.Neutral, out _cancelRt, out _cancelImage, out _cancelLabel);
            BuildButton(fillGo.transform, ModLocalization.T("respec.dialog.confirm"), new Vector2(-134f, 14f),
                RespecStyle.Danger, out _confirmRt, out _confirmImage, out _confirmLabel);
        }

        private const float CardWidth = 420f;
        private const float DefaultCardHeight = 210f;
        private const float TitleAreaHeight = 52f;
        private const float ButtonFooterHeight = 72f;
        private const float MaxCardHeight = 340f;

        private void LayoutCardForBody(string bodyText)
        {
            if (_cardRt == null || _body == null)
                return;

            float textWidth = CardWidth - 36f;
            var preferred = _body.GetPreferredValues(bodyText ?? string.Empty, textWidth, 0f);
            float cardHeight = Mathf.Clamp(
                TitleAreaHeight + preferred.y + ButtonFooterHeight + 12f,
                DefaultCardHeight,
                MaxCardHeight);
            _cardRt.sizeDelta = new Vector2(CardWidth, cardHeight);
        }

        private static void BuildButton(
            Transform parent, string label, Vector2 anchoredPos, Color normal,
            out RectTransform rect, out Image image, out TextMeshProUGUI labelTmp)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(110f, 36f);
            rt.anchoredPosition = anchoredPos;
            rect = rt;

            image = go.AddComponent<Image>();
            image.sprite = RespecStyle.SolidRounded(Color.white, new Color(0f, 0f, 0f, 0.45f), 20, 1, 5);
            image.type = Image.Type.Sliced;
            image.color = normal;
            image.raycastTarget = true;

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
            labelTmp = tmp;
        }
    }
}
