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
        private bool _hasImage;
        private int _pendingItemId = -1;
        public bool PlayerOver { get; private set; }

        public ChestLabel Init()
        {
            var chest = transform.GetComponentInParent<Chest>();
            if (chest == null)
            {
                Plugin.Log?.LogError("ChestLabel.Init: Could not find Chest in parent.");
                return this;
            }

            var boxCollider = chest.GetComponent<BoxCollider2D>() ?? chest.GetComponentInChildren<BoxCollider2D>();
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
            _canvas.transform.localPosition = Vector3.zero;
            _canvas.transform.eulerAngles = new Vector3(315f, 0f, 0f);
            _canvas.GetComponent<RectTransform>().sizeDelta = boxCollider.size;

            _label = new GameObject("SenpaisChest_Label").AddComponent<TextMeshProUGUI>();
            _label.raycastTarget = false;
            _label.transform.SetParent(_canvas.transform, false);
            _label.GetComponent<RectTransform>().sizeDelta = boxCollider.size * 1.75f;
            _label.transform.localPosition = boxCollider.bounds.center - transform.position + new Vector3(0f, 0.9f, 0f);
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
            _image.transform.localPosition = new Vector3(boxCollider.bounds.center.x - transform.position.x, 0.5f, -0.1f);
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
            var chest = transform.GetComponentInParent<Chest>();
            if (chest == null) return;
            var data = chest.GetChestData();
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
            if (_label != null)
                _label.enabled = ShouldBeVisible(config.LabelVisibility.Value);
            if (_image != null)
                _image.enabled = _hasImage && ShouldBeVisible(config.IconVisibility.Value);
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
