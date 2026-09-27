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
    /// Draws the player's gallery bust as a left-side mirror of the vanilla NPC dialogue portrait.
    /// Vanilla <c>DialogueController._bust</c> sits at anchoredPosition ~(512, 0) beside the dialogue
    /// panel (not inside it); the player image is a sibling with X mirrored.
    /// </summary>
    public sealed class PlayerBustHud : MonoBehaviour
    {
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
            {
                // Re-parent if NPC bust parent changed (or we were under the wrong root).
                if (_npcBust != null && _rect.parent != _npcBust.transform.parent)
                    PlaceAsNpcSibling();
                return;
            }

            try
            {
                TryResolveNpcBust();

                Transform parent = null;
                if (_npcBust != null)
                    parent = _npcBust.transform.parent;

                if (parent == null)
                    parent = transform;

                var go = new GameObject("HavensMirror_PlayerBust", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                _image = go.GetComponent<Image>();
                _rect = go.GetComponent<RectTransform>();

                _image.preserveAspect = true;
                _image.raycastTarget = false;
                _image.color = Color.white;
                go.SetActive(false);

                PlaceAsNpcSibling();
                _ready = _image != null;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[Dialogue] PlayerBustHud setup failed: {ex.Message}");
                _ready = false;
            }
        }

        /// <summary>
        /// Mirror the NPC bust: same parent/anchors/pivot/size/Y, X negated, drawn last among siblings.
        /// </summary>
        private void PlaceAsNpcSibling()
        {
            if (_rect == null || _image == null)
                return;

            if (!TryResolveNpcBust() || _npcBust == null)
            {
                // Fallback if NPC bust is not yet wired — left of a centered dialogue panel chrome.
                _rect.anchorMin = new Vector2(0.5f, 0.5f);
                _rect.anchorMax = new Vector2(0.5f, 0.5f);
                _rect.pivot = new Vector2(0.5f, 0.5f);
                _rect.anchoredPosition = new Vector2(-512f, 0f);
                _rect.sizeDelta = new Vector2(166f, 199f);
                _image.color = Color.white;
                _rect.SetAsLastSibling();
                return;
            }

            RectTransform npcRect = _npcBust.rectTransform;
            Transform npcParent = npcRect.parent;
            if (npcParent != null && _rect.parent != npcParent)
                _rect.SetParent(npcParent, false);

            _rect.anchorMin = npcRect.anchorMin;
            _rect.anchorMax = npcRect.anchorMax;
            _rect.pivot = npcRect.pivot;
            _rect.localScale = npcRect.localScale;
            _rect.localRotation = npcRect.localRotation;

            Vector2 npcPos = npcRect.anchoredPosition;
            // Vanilla places NPC at ~(512 + offset.x, offset.y). Player mirrors to the left.
            float mirrorX = npcPos.x;
            if (Mathf.Abs(mirrorX) < 1f)
                mirrorX = 512f;
            _rect.anchoredPosition = new Vector2(-Mathf.Abs(mirrorX), npcPos.y);

            Vector2 npcSize = npcRect.sizeDelta;
            if (npcSize.x > 1f && npcSize.y > 1f)
                _rect.sizeDelta = npcSize;
            else if (_image.sprite != null)
                _image.SetNativeSize();

            _image.color = Color.white;
            _image.preserveAspect = true;
            _image.raycastTarget = false;
            // Immediately after the NPC bust so we share its layer above the dim overlay.
            if (npcRect.parent != null)
            {
                int npcIndex = npcRect.GetSiblingIndex();
                _rect.SetSiblingIndex(Mathf.Min(npcIndex + 1, npcRect.parent.childCount - 1));
            }
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
            // Match vanilla LoadBust sizing; PlaceAsNpcSibling may overwrite with NPC sizeDelta.
            _image.SetNativeSize();
            PlaceAsNpcSibling();
            _image.gameObject.SetActive(true);

            // Vanilla LoadBust yields then SetNativeSize + ActivateBustsNextFrame (2 frames).
            // Re-mirror after that so we pick up final NPC size/offset.
            if (_relayoutRoutine != null)
                StopCoroutine(_relayoutRoutine);
            _relayoutRoutine = StartCoroutine(RelayoutAfterNpcSettles(slot, shelf.LoadedFrom));
        }

        private IEnumerator RelayoutAfterNpcSettles(LookSlot slot, string loadedFrom)
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

            PlaceAsNpcSibling();

            if (!_loggedLayout && _rect != null)
            {
                _loggedLayout = true;
                string parentName = _rect.parent != null ? _rect.parent.name : "(null)";
                Plugin.Log?.LogInfo(
                    $"[Dialogue] Player bust layout: parent='{parentName}' anchoredPosition={_rect.anchoredPosition} " +
                    $"sizeDelta={_rect.sizeDelta} siblingIndex={_rect.GetSiblingIndex()}");
            }

            Plugin.Log?.LogInfo($"[Dialogue] Present player bust '{slot}' from '{loadedFrom}'.");
            _relayoutRoutine = null;
        }
    }
}
