using System;
using HavensMirror.Config;
using HavensMirror.Gallery;
using HavensMirror.Look;
using UnityEngine;
using UnityEngine.UI;

namespace HavensMirror.Dialogue
{
    /// <summary>
    /// Lightweight UI layer that draws the player's custom bust during dialogue.
    /// Attached to the dialogue panel root at runtime.
    /// </summary>
    public sealed class PlayerBustHud : MonoBehaviour
    {
        private static PlayerBustHud _instance;

        public static PlayerBustHud Instance => _instance;

        private Image _image;
        private RectTransform _rect;
        private bool _ready;

        public static PlayerBustHud EnsureOn(GameObject dialoguePanel)
        {
            if (dialoguePanel == null)
                return null;

            if (_instance != null && _instance.gameObject != null)
                return _instance;

            var existing = dialoguePanel.GetComponent<PlayerBustHud>();
            if (existing != null)
            {
                _instance = existing;
                existing.EnsureVisual();
                return existing;
            }

            var hud = dialoguePanel.AddComponent<PlayerBustHud>();
            _instance = hud;
            hud.EnsureVisual();
            return hud;
        }

        private void Awake()
        {
            _instance = this;
            EnsureVisual();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void EnsureVisual()
        {
            if (_ready && _image != null)
                return;

            try
            {
                // Prefer cloning an existing bust node so layout matches the game chrome.
                Transform template = FindChildRecursive(transform, "BustOffset")
                                     ?? FindChildRecursive(transform, "Bust")
                                     ?? FindChildRecursive(transform, "bust");

                GameObject visual;
                if (template != null)
                {
                    visual = Instantiate(template.gameObject, transform);
                    visual.name = "HavensMirror_PlayerBust";

                    // If BustOffset wraps the image, use the first Image child; else the root.
                    _image = visual.GetComponentInChildren<Image>(true);
                    if (_image == null)
                    {
                        Destroy(visual);
                        visual = BuildBareImage();
                    }
                    else
                    {
                        // Flatten to image object if we cloned a wrapper with extras.
                        visual = _image.gameObject;
                        visual.name = "HavensMirror_PlayerBust";
                    }
                }
                else
                {
                    visual = BuildBareImage();
                }

                _rect = visual.GetComponent<RectTransform>();
                if (_rect == null)
                    _rect = visual.AddComponent<RectTransform>();

                // Player-side placement (left of dialogue). Tuned to typical Sun Haven bust framing.
                _rect.anchorMin = new Vector2(0f, 0.5f);
                _rect.anchorMax = new Vector2(0f, 0.5f);
                _rect.pivot = new Vector2(0.5f, 0.5f);
                _rect.anchoredPosition = new Vector2(190f, 0f);
                _rect.sizeDelta = new Vector2(166f, 199f);

                if (_image == null)
                    _image = visual.GetComponent<Image>();

                if (_image != null)
                {
                    _image.preserveAspect = true;
                    _image.raycastTarget = false;
                    _image.gameObject.SetActive(false);
                }

                _ready = _image != null;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[Dialogue] PlayerBustHud setup failed: {ex.Message}");
                _ready = false;
            }
        }

        private GameObject BuildBareImage()
        {
            var go = new GameObject("HavensMirror_PlayerBust", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            _image = go.GetComponent<Image>();
            return go;
        }

        public void Hide()
        {
            if (_image != null)
                _image.gameObject.SetActive(false);
        }

        /// <summary>
        /// Show the player's gallery bust. Vanilla <c>isRefreshBust</c> is intentionally ignored —
        /// NPCAI.Interact always passes true (NPC bust flicker during addressable load).
        /// </summary>
        public void Present(bool vows, bool shore, bool costume)
        {
            if (MirrorOptions.Enabled == null || !MirrorOptions.Enabled.Value)
            {
                Hide();
                return;
            }

            EnsureVisual();
            if (!_ready || _image == null)
            {
                Plugin.Log?.LogWarning("[Dialogue] Present: PlayerBustHud image not ready.");
                return;
            }

            var shelf = Plugin.Shelf;
            if (shelf == null || !shelf.HasAny)
            {
                Plugin.Log?.LogDebug("[Dialogue] Present: shelf empty — hiding.");
                Hide();
                return;
            }

            LookSlot slot = ForcedLookResolver.ResolveActiveLook(vows, shore, costume);
            Sprite sprite = shelf.Get(slot);
            if (sprite == null)
            {
                Plugin.Log?.LogWarning($"[Dialogue] Present: no sprite for look '{slot}'.");
                Hide();
                return;
            }

            _image.sprite = sprite;
            _image.gameObject.SetActive(true);
            Plugin.Log?.LogInfo($"[Dialogue] Present player bust '{slot}' from '{shelf.LoadedFrom}'.");
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase))
                    return child;

                Transform nested = FindChildRecursive(child, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
