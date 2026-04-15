using System;
using System.Linq;
using SenpaisChest.ChestLabels.Extensions;
using SenpaisChest.Config;
using SenpaisChest.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wish;

namespace SenpaisChest.ChestLabels
{
    internal class ChestLabel : MonoBehaviour
    {
        private const float LabelVerticalOffset = 0.45f;
        private const float ScreenYOffset = 16f;

        private static readonly (Color32 color, Color32 outlineColor)[] ChestColors = new (int, int)[]
        {
            (6045747, 3021313),
            (8723740, 3476491),
            (14375446, 4529159),
            (13220101, 4735746),
            (5403146, 2107141),
            (224944, 1908533),
            (6098836, 2759479),
            (13334429, 5838661),
            (16776438, 1840926),
            (7237744, 1840926),
            (2761770, 854797)
        }.Select(hex => (hex.Item1.ToColor(), hex.Item2.ToColor())).ToArray();

        private static Canvas _overlayCanvas;
        private static RectTransform _overlayRoot;

        private RectTransform _labelRoot;
        private Image _iconImage;
        private TextMeshProUGUI _label;
        private ChestHitbox _hitbox;
        private Chest _chest;
        private bool _hasIcon;
        private int _pendingItemId = -1;
        public bool PlayerOver { get; private set; }

        public ChestLabel Init()
        {
            _chest = transform.GetComponentInParent<Chest>();
            if (_chest == null)
            {
                Plugin.Log?.LogError("ChestLabel.Init: Could not find Chest in parent.");
                return this;
            }

            var boxCollider = _chest.GetComponent<BoxCollider2D>() ?? _chest.GetComponentInChildren<BoxCollider2D>();
            if (boxCollider == null)
            {
                Plugin.Log?.LogError("ChestLabel.Init: Chest has no BoxCollider2D - labels will not show.");
                return this;
            }

            EnsureOverlayCanvas();
            if (_overlayRoot == null)
            {
                Plugin.Log?.LogError("ChestLabel.Init: Could not initialize overlay canvas.");
                return this;
            }

            _labelRoot = new GameObject("SenpaisChest_LabelRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            _labelRoot.SetParent(_overlayRoot, false);
            _labelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _labelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _labelRoot.pivot = new Vector2(0.5f, 0f);
            _labelRoot.sizeDelta = new Vector2(240f, 42f);

            _label = new GameObject("SenpaisChest_LabelText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _label.transform.SetParent(_labelRoot, false);
            _label.alignment = TextAlignmentOptions.Center;
            _label.enableWordWrapping = false;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.fontSize = 24f;
            _label.outlineWidth = 0.15f;
            _label.text = "Chest";
            var labelRt = _label.rectTransform;
            labelRt.anchorMin = new Vector2(0.5f, 0f);
            labelRt.anchorMax = new Vector2(0.5f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = new Vector2(240f, 30f);

            _iconImage = new GameObject("SenpaisChest_LabelIcon", typeof(RectTransform)).AddComponent<Image>();
            _iconImage.transform.SetParent(_labelRoot, false);
            var iconRt = _iconImage.rectTransform;
            iconRt.anchorMin = new Vector2(0.5f, 0f);
            iconRt.anchorMax = new Vector2(0.5f, 0f);
            iconRt.pivot = new Vector2(0.5f, 0f);
            iconRt.anchoredPosition = new Vector2(0f, -18f);
            iconRt.sizeDelta = new Vector2(16f, 16f);
            _iconImage.enabled = false;
            _iconImage.raycastTarget = false;

            var hitboxGo = new GameObject("SenpaisChest_LabelHitbox");
            hitboxGo.transform.SetParent(transform, false);
            hitboxGo.transform.localPosition = new Vector3(0f, -0.2f, -0.3f);
            _hitbox = hitboxGo.AddComponent<ChestHitbox>();

            return this;
        }

        public void DoUpdate()
        {
            if (_label == null) return; // Not initialized yet (InitWhenReady may still be running)
            if (_chest == null)
                _chest = transform.GetComponentInParent<Chest>();
            if (_chest == null) return;

            UpdateAnchor();
            var data = _chest.GetChestData();
            string labelText = data.name;
            if (string.IsNullOrWhiteSpace(labelText))
                labelText = SmartChestManager.GetChestName(_chest);
            if (string.Equals(labelText, "Chest", StringComparison.OrdinalIgnoreCase))
                labelText = string.Empty;
            SetTextAndIcon(labelText, data.color);
        }

        public string GetText()
        {
            return _label != null ? _label.text : "";
        }

        public void SetTextAndIcon(string text, int color)
        {
            if (_label == null) return; // Guard against DoUpdate before Init completes
            text ??= "";
            var parts = text.Split(new[] { ' ' }, 2);

            var colorIdx = Mathf.Clamp(color, 0, ChestColors.Length - 1);
            var (textColor, outlineColor) = ChestColors[colorIdx];
            _label.color = textColor;
            _label.outlineColor = outlineColor;

            if (parts.Length == 1 || !int.TryParse(parts[0], out var itemId))
            {
                _label.text = text;
                _hasIcon = false;
                _pendingItemId = -1;
                if (_iconImage != null)
                    _iconImage.enabled = false;
                return;
            }

            try
            {
                if (!PSS.Database.ValidID(itemId))
                {
                    _label.text = text;
                    _hasIcon = false;
                    _pendingItemId = -1;
                    if (_iconImage != null)
                        _iconImage.enabled = false;
                    return;
                }
                _label.text = parts[parts.Length - 1];
                _pendingItemId = itemId;
                _hasIcon = false;
                if (_iconImage != null)
                {
                    _iconImage.sprite = null;
                    _iconImage.enabled = false;
                }
                PSS.Database.GetData<ItemData>(itemId, data =>
                {
                    if (data != null && _iconImage != null && _pendingItemId == itemId)
                    {
                        _iconImage.sprite = data.icon;
                        _hasIcon = _iconImage.sprite != null;
                        _iconImage.enabled = _hasIcon;
                    }
                }, null);
            }
            catch
            {
                _label.text = text;
                _hasIcon = false;
                _pendingItemId = -1;
                if (_iconImage != null)
                    _iconImage.enabled = false;
            }
        }

        private void LateUpdate()
        {
            var config = Plugin.GetConfig();
            if (config == null || !config.EnableChestLabels.Value) return;

            UpdateAnchor();
            if (_label != null)
                _label.enabled = ShouldBeVisible(config.LabelVisibility.Value);
            if (_iconImage != null)
                _iconImage.enabled = _hasIcon && ShouldBeVisible(config.IconVisibility.Value);
        }

        private void UpdateAnchor(BoxCollider2D fallbackCollider = null)
        {
            if (_labelRoot == null) return;
            if (_chest == null)
                _chest = transform.GetComponentInParent<Chest>();
            if (_chest == null) return;

            var anchor = ResolveAnchorWorldPosition(_chest, fallbackCollider);
            var cam = Camera.main;
            if (cam == null)
                return;
            Vector3 screen = cam.WorldToScreenPoint(anchor);
            if (screen.z <= 0f)
            {
                _labelRoot.gameObject.SetActive(false);
                return;
            }
            _labelRoot.gameObject.SetActive(true);
            if (_overlayRoot == null)
                return;
            var screenPoint = new Vector2(screen.x, screen.y + ScreenYOffset);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlayRoot, screenPoint, null, out var localPoint))
            {
                _labelRoot.anchoredPosition = localPoint;
            }
        }

        private static Vector3 ResolveAnchorWorldPosition(Chest chest, BoxCollider2D fallbackCollider)
        {
            // Primary anchor: chest collider top-center is the most stable "above chest" point.
            var collider = fallbackCollider ?? chest.GetComponent<BoxCollider2D>() ?? chest.GetComponentInChildren<BoxCollider2D>();
            if (collider != null)
            {
                var b = collider.bounds;
                return new Vector3(b.center.x, b.max.y + LabelVerticalOffset, chest.transform.position.z);
            }

            // Fallback: sprite bounds if collider is unavailable.
            bool hasBounds = false;
            Bounds worldBounds = default;
            var renderers = chest.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                    continue;
                if (!hasBounds)
                {
                    worldBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderer.bounds);
                }
            }

            if (hasBounds)
                return new Vector3(worldBounds.center.x, worldBounds.max.y + LabelVerticalOffset, chest.transform.position.z);

            var pos = chest.transform.position;
            return new Vector3(pos.x, pos.y + 1f, pos.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Player.Instance != null && other.gameObject == Player.Instance.gameObject)
                PlayerOver = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (Player.Instance != null && other.gameObject == Player.Instance.gameObject)
                PlayerOver = false;
        }

        private bool ShouldBeVisible(SmartChestConfig.ChestLabelVisibility visibility)
        {
            if (Plugin.CurrentInteractingChest != null)
                return false;
            if (visibility == SmartChestConfig.ChestLabelVisibility.Hidden) return false;
            if (visibility == SmartChestConfig.ChestLabelVisibility.Visible) return true;
            return _hitbox != null && (_hitbox.MouseOver || PlayerOver);
        }

        private static void EnsureOverlayCanvas()
        {
            if (_overlayCanvas != null && _overlayRoot != null)
                return;

            var go = new GameObject("SenpaisChest_LabelOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(go);

            _overlayCanvas = go.GetComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = short.MaxValue;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _overlayRoot = go.GetComponent<RectTransform>();
        }

        private void OnDestroy()
        {
            if (_labelRoot != null)
                UnityEngine.Object.Destroy(_labelRoot.gameObject);
        }
    }
}
