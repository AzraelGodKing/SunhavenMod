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
    /// Draws the player's gallery bust on the LEFT half of the dialogue screen.
    /// Vanilla <c>DialogueController._bust</c> lives under <c>BustOffset</c> at a right-biased
    /// anchored X (~512). Parenting under that node (or mirroring NPC X) stacks the player on
    /// the NPC. Placement is always under the root Canvas in canvas-local space: fixed left X,
    /// Y/size sampled from the NPC bust only.
    /// </summary>
    public sealed class PlayerBustHud : MonoBehaviour
    {
        private const float LeftMargin = 24f;
        private const float LeftFraction = 0.15f;
        private const float FallbackBustWidth = 166f;
        private const float FallbackBustHeight = 199f;

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
        /// Cache the vanilla NPC bust Image (DialogueController._bust) used as the size/Y template.
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

                RectTransform layoutParent = ResolveLayoutParent();
                Transform parent = layoutParent != null ? (Transform)layoutParent : transform;

                var go = new GameObject("HavensMirror_PlayerBust", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                _image = go.GetComponent<Image>();
                _rect = go.GetComponent<RectTransform>();

                _image.preserveAspect = true;
                _image.raycastTarget = false;
                _image.color = Color.white;
                go.SetActive(false);

                PlaceOnLeftHalf();
                _ready = _image != null;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[Dialogue] PlayerBustHud setup failed: {ex.Message}");
                _ready = false;
            }
        }

        /// <summary>
        /// Root canvas (or DialogueController) — never BustOffset / never NPC's parent.
        /// </summary>
        private RectTransform ResolveLayoutParent()
        {
            TryResolveNpcBust();

            Canvas canvas = null;
            if (_npcBust != null)
                canvas = _npcBust.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                canvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;

            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect != null && !IsBustOffset(canvasRect))
                return canvasRect;

            // DialogueController host (this component) — still never BustOffset.
            if (transform is RectTransform hostRect && !IsBustOffset(hostRect) && !IsUnderBustOffset(hostRect))
                return hostRect;

            // Walk up from NPC past BustOffset to a safe ancestor.
            if (_npcBust != null)
            {
                Transform t = _npcBust.transform.parent;
                while (t != null)
                {
                    if (t is RectTransform rt && !IsBustOffset(rt) && !string.Equals(t.name, "Bust", StringComparison.OrdinalIgnoreCase))
                    {
                        // Prefer the first non-offset ancestor that is not the NPC image itself.
                        if (t != _npcBust.transform)
                            return rt;
                    }

                    t = t.parent;
                }
            }

            return transform as RectTransform;
        }

        private static bool IsBustOffset(Transform t)
        {
            return t != null && t.name != null
                   && t.name.IndexOf("BustOffset", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsUnderBustOffset(Transform t)
        {
            while (t != null)
            {
                if (IsBustOffset(t))
                    return true;
                t = t.parent;
            }

            return false;
        }

        /// <summary>
        /// Canvas-local left placement. X is always a left-half fraction of the canvas —
        /// never NPC anchored X, never mirrored sibling placement under BustOffset.
        /// After NPC settles, only size + Y are refreshed from the NPC; X stays left-locked.
        /// </summary>
        private void PlaceOnLeftHalf()
        {
            if (_rect == null || _image == null)
                return;

            TryResolveNpcBust();
            RectTransform npcRect = _npcBust != null ? _npcBust.rectTransform : null;
            RectTransform layoutParent = ResolveLayoutParent();

            if (layoutParent != null && _rect.parent != layoutParent)
                _rect.SetParent(layoutParent, false);

            // Center anchors so anchoredPosition is absolute canvas-local (not left-edge relative
            // under a right-biased intermediate parent).
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.localScale = Vector3.one;
            _rect.localRotation = Quaternion.identity;

            Vector2 size = ResolvePlayerSize(npcRect);
            _rect.sizeDelta = size;

            Rect canvasRect = layoutParent != null
                ? layoutParent.rect
                : new Rect(-960f, -540f, 1920f, 1080f);

            // Left half only — fraction of canvas width, then pad by half bust width + margin.
            float leftCenterX = canvasRect.xMin + canvasRect.width * LeftFraction;
            float x = leftCenterX;
            // Keep the full sprite on-screen with a left margin.
            float minX = canvasRect.xMin + size.x * 0.5f + LeftMargin;
            if (x < minX)
                x = minX;
            // Never cross into the right half.
            float maxLeftX = canvasRect.xMin + canvasRect.width * 0.45f - size.x * 0.5f;
            if (x > maxLeftX)
                x = maxLeftX;

            float y = ResolvePlayerY(npcRect, layoutParent, canvasRect);

            _rect.anchoredPosition = new Vector2(x, y);

            _image.color = Color.white;
            _image.preserveAspect = true;
            _image.raycastTarget = false;
            _rect.SetAsLastSibling();
        }

        private Vector2 ResolvePlayerSize(RectTransform npcRect)
        {
            Vector2 size = _rect != null ? _rect.sizeDelta : Vector2.zero;

            if (npcRect != null)
            {
                Vector2 npcSize = npcRect.sizeDelta;
                if (npcSize.x > 1f && npcSize.y > 1f)
                    size = npcSize;
            }

            if ((size.x <= 1f || size.y <= 1f) && _image != null && _image.sprite != null)
            {
                _image.SetNativeSize();
                size = _rect.sizeDelta;
            }

            if (size.x <= 1f || size.y <= 1f)
                size = new Vector2(FallbackBustWidth, FallbackBustHeight);

            return size;
        }

        /// <summary>
        /// Y from NPC bust world center → canvas local. Never uses NPC.x for player X.
        /// </summary>
        private static float ResolvePlayerY(RectTransform npcRect, RectTransform layoutParent, Rect canvasRect)
        {
            if (npcRect == null || layoutParent == null)
                return 0f;

            Vector3 npcWorldCenter = npcRect.TransformPoint(npcRect.rect.center);
            Vector3 npcLocal = layoutParent.InverseTransformPoint(npcWorldCenter);
            // Center-anchored child: anchored Y is offset from parent rect center.
            return npcLocal.y - canvasRect.center.y;
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
            // Stay hidden until final left placement so we never flash on the NPC side.
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

            // One more frame so NPC size/offset stick before we sample size + Y.
            yield return null;

            PlaceOnLeftHalf();
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
