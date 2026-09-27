using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using HavensMirror.Config;
using HavensMirror.Gallery;
using HavensMirror.Look;
using UnityEngine;
using UnityEngine.UI;

namespace HavensMirror.Dialogue
{
    /// <summary>
    /// Draws the player's gallery bust on the left side of the dialogue screen.
    /// Vanilla <c>DialogueController._bust</c> lives under a <c>BustOffset</c> parent at
    /// anchoredPosition ~(512 + npcOffset, y). Mirroring to -|x| under that same parent
    /// pushes the player off-canvas (BustOffset is already right-biased). Instead we parent
    /// under the Canvas with left-edge anchors and a fixed inset X.
    /// </summary>
    public sealed class PlayerBustHud : MonoBehaviour
    {
        private const float LeftMargin = 24f;

        private static PlayerBustHud _instance;
        private static bool _loggedLayout;

        public static PlayerBustHud Instance => _instance;

        private Image _image;
        private RectTransform _rect;
        private bool _ready;
        private Image _npcBust;
        private Coroutine _relayoutRoutine;

        /// <summary>
        /// Attach to the DialogueController host (preferred) or any dialogue UI root.
        /// </summary>
        public static PlayerBustHud EnsureOn(GameObject host)
        {
            if (host == null)
                return null;

            if (_instance != null && _instance.gameObject != null)
                return _instance;

            var existing = host.GetComponent<PlayerBustHud>();
            if (existing != null)
            {
                _instance = existing;
                return existing;
            }

            var hud = host.AddComponent<PlayerBustHud>();
            _instance = hud;
            return hud;
        }

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// Cache the vanilla NPC bust Image (DialogueController._bust) used as the layout template.
        /// </summary>
        public void BindNpcBust(Image npcBust)
        {
            if (npcBust == null)
                return;

            _npcBust = npcBust;
        }

        private bool TryResolveNpcBust()
        {
            if (_npcBust != null)
                return true;

            try
            {
                Type dialogueType = AccessTools.TypeByName("Wish.DialogueController")
                                     ?? AccessTools.TypeByName("DialogueController");
                if (dialogueType == null)
                    return false;

                object controller = null;
                FieldInfo instanceField = AccessTools.Field(dialogueType, "Instance");
                if (instanceField != null)
                    controller = instanceField.GetValue(null);

                if (controller == null)
                {
                    PropertyInfo instanceProp = AccessTools.Property(dialogueType, "Instance");
                    if (instanceProp != null)
                        controller = instanceProp.GetValue(null, null);
                }

                if (controller == null)
                    return false;

                FieldInfo bustField = AccessTools.Field(dialogueType, "_bust")
                                      ?? AccessTools.Field(dialogueType, "bust")
                                      ?? AccessTools.Field(dialogueType, "Bust");
                if (bustField != null)
                    _npcBust = bustField.GetValue(controller) as Image;

                if (_npcBust == null)
                {
                    PropertyInfo bustProp = AccessTools.Property(dialogueType, "Bust");
                    if (bustProp != null)
                        _npcBust = bustProp.GetValue(controller, null) as Image;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[Dialogue] Resolve NPC bust: {ex.Message}");
            }

            return _npcBust != null;
        }

        private void EnsureVisual()
        {
            if (_ready && _image != null && _rect != null)
                return;

            try
            {
                TryResolveNpcBust();

                Transform parent = transform;
                Canvas canvas = null;
                if (_npcBust != null)
                    canvas = _npcBust.GetComponentInParent<Canvas>();
                if (canvas == null)
                    canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                    parent = canvas.transform;

                var go = new GameObject("HavensMirror_PlayerBust", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                _image = go.GetComponent<Image>();
                _rect = go.GetComponent<RectTransform>();

                _image.preserveAspect = true;
                _image.raycastTarget = false;
                _image.color = Color.white;
                go.SetActive(false);

                PlaceLeftOfDialogue();
                _ready = _image != null;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[Dialogue] PlayerBustHud setup failed: {ex.Message}");
                _ready = false;
            }
        }

        /// <summary>
        /// Fixed left-of-screen placement under the Canvas (not under BustOffset).
        /// Same Y as the NPC bust center; size matches NPC when available.
        /// </summary>
        private void PlaceLeftOfDialogue()
        {
            if (_rect == null || _image == null)
                return;

            TryResolveNpcBust();
            RectTransform npcRect = _npcBust != null ? _npcBust.rectTransform : null;

            Canvas canvas = null;
            if (npcRect != null)
                canvas = npcRect.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            RectTransform layoutParent = canvas != null ? canvas.transform as RectTransform : null;

            // Escape BustOffset: prefer Canvas, else parent of BustOffset (sibling of offset node).
            if (layoutParent == null && npcRect != null && npcRect.parent != null)
            {
                if (npcRect.parent.parent is RectTransform grandparent)
                    layoutParent = grandparent;
                else if (npcRect.parent is RectTransform offsetParent)
                    layoutParent = offsetParent;
            }

            if (layoutParent == null)
                layoutParent = transform as RectTransform;

            if (layoutParent != null && _rect.parent != layoutParent)
                _rect.SetParent(layoutParent, false);

            _rect.anchorMin = new Vector2(0f, 0.5f);
            _rect.anchorMax = new Vector2(0f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.localScale = Vector3.one;
            _rect.localRotation = Quaternion.identity;

            Vector2 size = _rect.sizeDelta;
            if (npcRect != null)
            {
                Vector2 npcSize = npcRect.sizeDelta;
                if (npcSize.x > 1f && npcSize.y > 1f)
                    size = npcSize;
            }

            if ((size.x <= 1f || size.y <= 1f) && _image.sprite != null)
            {
                _image.SetNativeSize();
                size = _rect.sizeDelta;
            }

            if (size.x <= 1f || size.y <= 1f)
                size = new Vector2(166f, 199f);

            _rect.sizeDelta = size;

            float x = size.x * 0.5f + LeftMargin;
            float y = 0f;

            if (npcRect != null && layoutParent != null)
            {
                Vector3 npcWorldCenter = npcRect.TransformPoint(npcRect.rect.center);
                Vector3 npcLocal = layoutParent.InverseTransformPoint(npcWorldCenter);
                // Left-middle anchor reference is parent's left edge at vertical center.
                Rect parentRect = layoutParent.rect;
                y = npcLocal.y - parentRect.center.y;
            }

            _rect.anchoredPosition = new Vector2(x, y);

            if (layoutParent != null)
                ClampFullyVisible(layoutParent);

            _image.color = Color.white;
            _image.preserveAspect = true;
            _image.raycastTarget = false;
            _rect.SetAsLastSibling();
        }

        /// <summary>
        /// Keep the bust fully inside the layout parent (canvas) by shifting X only.
        /// </summary>
        private void ClampFullyVisible(RectTransform layoutParent)
        {
            if (_rect == null || layoutParent == null)
                return;

            Vector3[] bustCorners = new Vector3[4];
            Vector3[] parentCorners = new Vector3[4];
            _rect.GetWorldCorners(bustCorners);
            layoutParent.GetWorldCorners(parentCorners);

            float bustMinX = bustCorners[0].x;
            float bustMaxX = bustCorners[2].x;
            float parentMinX = parentCorners[0].x;
            float parentMaxX = parentCorners[2].x;

            float worldShift = 0f;
            if (bustMinX < parentMinX)
                worldShift = parentMinX - bustMinX;
            else if (bustMaxX > parentMaxX)
                worldShift = parentMaxX - bustMaxX;

            if (Mathf.Abs(worldShift) < 0.01f)
                return;

            Vector3 localShift = layoutParent.InverseTransformVector(new Vector3(worldShift, 0f, 0f));
            _rect.anchoredPosition += new Vector2(localShift.x, 0f);
        }

        public void Hide()
        {
            if (_relayoutRoutine != null)
            {
                StopCoroutine(_relayoutRoutine);
                _relayoutRoutine = null;
            }

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
            _image.color = Color.white;
            _image.SetNativeSize();
            // Stay hidden until final left placement so we never flash then jump off-screen.
            _image.gameObject.SetActive(false);

            if (_relayoutRoutine != null)
                StopCoroutine(_relayoutRoutine);
            _relayoutRoutine = StartCoroutine(PresentAfterNpcSettles(slot, shelf.LoadedFrom));
        }

        private IEnumerator PresentAfterNpcSettles(LookSlot slot, string loadedFrom)
        {
            // Wait for vanilla LoadBust + ActivateBustsNextFrame (size/active), with a short cap.
            for (int i = 0; i < 45; i++)
            {
                yield return null;
                if (_npcBust != null
                    && _npcBust.gameObject.activeInHierarchy
                    && _npcBust.sprite != null
                    && _npcBust.rectTransform.sizeDelta.x > 1f)
                {
                    break;
                }
            }

            // One more frame so NPC size/offset stick before we sample Y.
            yield return null;

            PlaceLeftOfDialogue();
            if (_image != null)
                _image.gameObject.SetActive(true);

            if (!_loggedLayout && _rect != null)
            {
                _loggedLayout = true;
                string parentName = _rect.parent != null ? _rect.parent.name : "(null)";
                string npcParent = _npcBust != null && _npcBust.rectTransform.parent != null
                    ? _npcBust.rectTransform.parent.name
                    : "(null)";
                Vector2 npcPos = _npcBust != null
                    ? _npcBust.rectTransform.anchoredPosition
                    : Vector2.zero;
                Plugin.Log?.LogInfo(
                    $"[Dialogue] Player bust layout: parent='{parentName}' anchoredPosition={_rect.anchoredPosition} " +
                    $"sizeDelta={_rect.sizeDelta} anchors=({_rect.anchorMin},{_rect.anchorMax}) " +
                    $"npcParent='{npcParent}' npcAnchored={npcPos} siblingIndex={_rect.GetSiblingIndex()}");
            }

            Plugin.Log?.LogInfo($"[Dialogue] Present player bust '{slot}' from '{loadedFrom}'.");
            _relayoutRoutine = null;
        }
    }
}
