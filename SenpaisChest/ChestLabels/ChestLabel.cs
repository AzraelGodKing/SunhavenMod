using System;
using System.Linq;
using SenpaisChest.ChestLabels.Extensions;
using SenpaisChest.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wish;

namespace SenpaisChest.ChestLabels
{
    internal class ChestLabel : MonoBehaviour
    {
        private const float LabelVerticalOffset = 0.35f;

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

        private Canvas _canvas;
        private Image _image;
        private TextMeshProUGUI _label;
        private ChestHitbox _hitbox;
        private Chest _chest;
        private bool _hasImage;
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

            TextMeshProUGUI yearUI = null;
            try
            {
                var dayCycle = SingletonBehaviour<DayCycle>.Instance;
                yearUI = dayCycle?.GetYearUI();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"ChestLabel.Init: Could not get DayCycle font: {ex.Message}");
            }

            _canvas = new GameObject("SenpaisChest_LabelCanvas").AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.worldCamera = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 5000;
            _canvas.transform.SetParent(transform, false);
            _canvas.transform.eulerAngles = new Vector3(315f, 0f, 0f);
            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Max(1.8f, boxCollider.size.x * 2f), Mathf.Max(0.9f, boxCollider.size.y));
            UpdateCanvasAnchor(boxCollider);

            _label = new GameObject("SenpaisChest_Label").AddComponent<TextMeshProUGUI>();
            _label.raycastTarget = false;
            _label.transform.SetParent(_canvas.transform, false);
            _label.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Max(2.2f, boxCollider.size.x * 2.2f), Mathf.Max(0.9f, boxCollider.size.y * 1.2f));
            _label.transform.localPosition = Vector3.zero;
            _label.alignment = TextAlignmentOptions.Center;
            _label.enableAutoSizing = true;
            _label.enableWordWrapping = false;
            if (yearUI != null && yearUI.font != null)
                _label.font = yearUI.font;
            else
                Plugin.Log?.LogError("Chest Labels: Could not find font - labels may not display correctly.");
            _label.fontSizeMin = 0.3f;
            _label.fontSizeMax = 0.5f;
            _label.isOverlay = true;
            _label.outlineWidth = 0.15f;

            _image = new GameObject("SenpaisChest_LabelImage").AddComponent<Image>();
            _image.raycastTarget = false;
            _image.transform.SetParent(_canvas.transform, false);
            _image.GetComponent<RectTransform>().sizeDelta = Vector2.one * 0.75f;
            _image.transform.localPosition = new Vector3(0f, -0.32f, -0.1f);
            _image.preserveAspect = true;

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

            UpdateCanvasAnchor();
            var data = _chest.GetChestData();
            SetTextAndIcon(data.name ?? "", data.color);
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
                _hasImage = false;
                _pendingItemId = -1;
                return;
            }

            try
            {
                if (!PSS.Database.ValidID(itemId))
                {
                    _label.text = text;
                    _hasImage = false;
                    _pendingItemId = -1;
                    return;
                }
                _label.text = parts[parts.Length - 1];
                _pendingItemId = itemId;
                _hasImage = false;
                _image.sprite = null;
                PSS.Database.GetData<ItemData>(itemId, data =>
                {
                    if (data != null && _image != null && _pendingItemId == itemId)
                    {
                        _image.sprite = data.icon;
                        _hasImage = _image.sprite != null;
                    }
                }, null);
            }
            catch
            {
                _label.text = text;
                _hasImage = false;
                _pendingItemId = -1;
            }
        }

        private void LateUpdate()
        {
            var config = Plugin.GetConfig();
            if (config == null || !config.EnableChestLabels.Value) return;

            if (_canvas != null && _canvas.worldCamera == null && Camera.main != null)
                _canvas.worldCamera = Camera.main;
            UpdateCanvasAnchor();
            if (_label != null)
                _label.enabled = ShouldBeVisible(config.LabelVisibility.Value);
            if (_image != null)
                _image.enabled = _hasImage && ShouldBeVisible(config.IconVisibility.Value);
        }

        private void UpdateCanvasAnchor(BoxCollider2D fallbackCollider = null)
        {
            if (_canvas == null) return;
            if (_chest == null)
                _chest = transform.GetComponentInParent<Chest>();
            if (_chest == null) return;

            var anchor = ResolveAnchorWorldPosition(_chest, fallbackCollider);
            _canvas.transform.position = anchor;
        }

        private static Vector3 ResolveAnchorWorldPosition(Chest chest, BoxCollider2D fallbackCollider)
        {
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

            if (!hasBounds && fallbackCollider != null)
            {
                worldBounds = fallbackCollider.bounds;
                hasBounds = true;
            }

            if (!hasBounds)
            {
                var pos = chest.transform.position;
                return new Vector3(pos.x, pos.y + 1f, pos.z - 0.1f);
            }

            return new Vector3(worldBounds.center.x, worldBounds.max.y + LabelVerticalOffset, chest.transform.position.z - 0.1f);
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
            if (visibility == SmartChestConfig.ChestLabelVisibility.Hidden) return false;
            if (visibility == SmartChestConfig.ChestLabelVisibility.Visible) return true;
            return _hitbox != null && (_hitbox.MouseOver || PlayerOver);
        }
    }
}
