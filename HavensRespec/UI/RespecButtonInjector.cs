using System;
using System.Reflection;
using BepInEx.Logging;
using SunhavenMods.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wish;
using UIButton = UnityEngine.UI.Button;

namespace HavensRespec.UI
{
    /// <summary>
    /// Builds and attaches the per-tab "Reset" (and optional "Undo") buttons onto a
    /// <see cref="SkillTree"/> panel. One instance is tracked per (Skills, ProfessionType)
    /// pair to avoid duplicates when SetupProfession is called multiple times.
    ///
    /// Visual layering (bottom-up): drop shadow → tintable white fill → top-half highlight
    /// gloss → fixed gold border ring → TextMeshPro label. Hover changes only the fill's
    /// Image.color and the root's localScale — no sprite regeneration per pointer event.
    /// </summary>
    internal sealed class RespecButtonInjector
    {
        private readonly ManualLogSource _log;
        private static FieldInfo _skillPointsTmpField;

        public GameObject ResetButton { get; private set; }
        public GameObject ResetAllButton { get; private set; }
        public GameObject UndoButton { get; private set; }
        private TextMeshProUGUI _resetLabel;
        private TextMeshProUGUI _resetAllLabel;
        private TextMeshProUGUI _undoLabel;

        public event Action OnResetClicked;
        public event Action OnResetAllClicked;
        public event Action OnUndoClicked;

        public RespecButtonInjector(ManualLogSource log)
        {
            _log = log;
        }

        public bool TryAttach(SkillTree panel, bool enableResetAll)
        {
            if (panel == null)
                return false;

            try
            {
                if (_skillPointsTmpField == null)
                {
                    _skillPointsTmpField = typeof(SkillTree).GetField("_skillPointsTMP", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                }

                var pointsTmp = _skillPointsTmpField?.GetValue(panel) as TextMeshProUGUI;
                if (pointsTmp == null)
                {
                    _log?.LogWarning("[Respec] _skillPointsTMP not found on SkillTree; cannot attach button.");
                    return false;
                }

                var parent = pointsTmp.transform.parent;
                var anchorRt = pointsTmp.GetComponent<RectTransform>();

                int rowIndex = 0;

                ResetButton = BuildButton(
                    parent,
                    anchorRt,
                    rowIndex++,
                    ModLocalization.T("respec.button.reset"),
                    ButtonWidth,
                    RespecStyle.Danger, RespecStyle.DangerHover, RespecStyle.DangerPressed,
                    () => OnResetClicked?.Invoke(),
                    out _resetLabel);

                if (enableResetAll)
                {
                    ResetAllButton = BuildButton(
                        parent,
                        anchorRt,
                        rowIndex++,
                        ModLocalization.T("respec.button.reset_all"),
                        ResetAllButtonWidth,
                        RespecStyle.Danger, RespecStyle.DangerHover, RespecStyle.DangerPressed,
                        () => OnResetAllClicked?.Invoke(),
                        out _resetAllLabel,
                        ResetAllExtraYOffset);
                }

                UndoButton = BuildButton(
                    parent,
                    anchorRt,
                    rowIndex,
                    ModLocalization.T("respec.button.undo"),
                    ButtonWidth,
                    RespecStyle.Neutral, RespecStyle.NeutralHover, RespecStyle.NeutralPressed,
                    () => OnUndoClicked?.Invoke(),
                    out _undoLabel);
                UndoButton.SetActive(false);

                return true;
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Respec] RespecButtonInjector.TryAttach failed: {ex}");
                return false;
            }
        }

        public void SetUndoVisible(bool visible)
        {
            if (UndoButton != null)
                UndoButton.SetActive(visible);
        }

        public void RefreshLocalizedLabels()
        {
            if (_resetLabel != null)
                _resetLabel.text = ModLocalization.T("respec.button.reset").ToUpperInvariant();
            if (_resetAllLabel != null)
                _resetAllLabel.text = ModLocalization.T("respec.button.reset_all").ToUpperInvariant();
            if (_undoLabel != null)
                _undoLabel.text = ModLocalization.T("respec.button.undo").ToUpperInvariant();
        }

        public void Destroy()
        {
            if (ResetButton != null) UnityEngine.Object.Destroy(ResetButton);
            if (ResetAllButton != null) UnityEngine.Object.Destroy(ResetAllButton);
            if (UndoButton != null) UnityEngine.Object.Destroy(UndoButton);
            ResetButton = null;
            ResetAllButton = null;
            UndoButton = null;
        }

        // --- Layout constants ----------------------------------------------------------
        // Compact pill sized to sit in the empty sidebar space below the EXP bar.
        private const float ButtonWidth = 96f;
        private const float ResetAllButtonWidth = 118f;
        private const float ButtonHeight = 26f;
        // Distance from _skillPointsTMP's localPosition down to the center of the first row.
        // _skillPointsTMP is the big count ("91"); below it the panel renders the
        // "Skill Points" caption, the "{Profession} EXP" label, and the EXP bar.
        private const float FirstRowCenterOffset = 142f;
        private const float RowSpacing = 35f;
        // Extra nudge so Reset All sits clearly below Reset without crowding the EXP bar.
        private const float ResetAllExtraYOffset = 6f;
        // _skillPointsTMP is centred on its own RectTransform but the sidebar column itself
        // sits slightly right-of-centre inside the skill panel. -18 nudges the stack left
        // so the wider Reset All pill aligns cleanly under Reset.
        private const float HorizontalOffset = -18f;

        // Reusable sprites — baked once, tinted via Image.color per button.
        private static Sprite _plaqueFillSprite;
        private static Sprite _plaqueBorderSprite;
        private static Sprite _plaqueShadowSprite;

        private static void EnsureSprites()
        {
            if (_plaqueFillSprite == null)
                _plaqueFillSprite = RespecStyle.SolidRounded(Color.white, new Color(0f, 0f, 0f, 0f), 32, 0, 8);
            if (_plaqueBorderSprite == null)
                _plaqueBorderSprite = RespecStyle.SolidRounded(new Color(0f, 0f, 0f, 0f), RespecStyle.AccentGold, 32, 2, 8);
            if (_plaqueShadowSprite == null)
                _plaqueShadowSprite = RespecStyle.SolidRounded(Color.white, new Color(0f, 0f, 0f, 0f), 32, 0, 9);
        }

        private static GameObject BuildButton(
            Transform parent,
            RectTransform anchorRt,
            int rowIndex,
            string label,
            float buttonWidth,
            Color normal, Color hover, Color pressed,
            Action onClick,
            out TextMeshProUGUI labelTmp,
            float extraYOffset = 0f)
        {
            EnsureSprites();

            // --- Root container ---------------------------------------------------------
            var go = new GameObject($"HavensRespec_{label}Button");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            // Fixed-point anchors so sizeDelta is literally our pixel size and the button
            // can't inherit the stretched anchors of _skillPointsTMP (which would make it
            // balloon to fill the entire sidebar).
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(buttonWidth, ButtonHeight);
            rt.localScale = Vector3.one;

            // Anchor TMP's localPosition is always valid (even pre-layout, before
            // GetWorldCorners has any real data to return), so we offset from it.
            float yOffset = -(FirstRowCenterOffset + rowIndex * RowSpacing + extraYOffset);
            rt.localPosition = anchorRt.localPosition + new Vector3(HorizontalOffset, yOffset, 0f);

            // Invisible raycast target on the root — catches clicks for the whole pill.
            var rootImage = go.AddComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0f);
            rootImage.raycastTarget = true;

            var button = go.AddComponent<UIButton>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = rootImage;
            button.onClick.AddListener(() => onClick?.Invoke());

            // --- Layered visuals (children, drawn in sibling order) ---------------------
            // 1. Drop shadow — slightly bigger + offset down-right, soft black.
            var shadow = AddChildImage(go.transform, "Shadow", _plaqueShadowSprite, new Color(0f, 0f, 0f, 0.45f));
            StretchFill(shadow.rectTransform, new Vector2(-1f, -3f), new Vector2(2f, 0f));
            shadow.raycastTarget = false;

            // 2. Plaque fill — the tintable layer. Its color IS the button's visible hue.
            var fill = AddChildImage(go.transform, "Fill", _plaqueFillSprite, normal);
            StretchFill(fill.rectTransform, Vector2.zero, Vector2.zero);
            fill.raycastTarget = false;

            // 3. Top-half highlight — a subtle lighter band across the top that reads as
            //    a polished / glossy finish, regardless of the fill tint.
            var gloss = AddChildImage(go.transform, "Gloss", _plaqueFillSprite, new Color(1f, 1f, 1f, 0.18f));
            var glossRt = gloss.rectTransform;
            glossRt.anchorMin = new Vector2(0f, 0.5f);
            glossRt.anchorMax = new Vector2(1f, 1f);
            glossRt.offsetMin = new Vector2(3f, 0f);
            glossRt.offsetMax = new Vector2(-3f, -2f);
            gloss.raycastTarget = false;

            // 4. Gold border ring — always gold regardless of fill tint; gives the wooden-
            //    plaque feel that matches the rest of the Sun Haven UI.
            var border = AddChildImage(go.transform, "Border", _plaqueBorderSprite, RespecStyle.AccentGold);
            StretchFill(border.rectTransform, Vector2.zero, Vector2.zero);
            border.raycastTarget = false;

            // 5. Label — all-caps, letter-spaced, parchment cream on dark fills.
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(0f, 0f);
            textRt.offsetMax = new Vector2(0f, -1f); // nudge up 1u so the label sits visually centred against the gloss
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 12f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.characterSpacing = buttonWidth > ButtonWidth ? 4f : 8f;
            tmp.color = RespecStyle.Parchment;
            tmp.text = label.ToUpperInvariant();
            tmp.raycastTarget = false;
            labelTmp = tmp;

            // NOTE: we intentionally do NOT touch tmp.fontSharedMaterial / tmp.outlineColor
            // here. SetupProfession fires before TMP has resolved its font asset, so the
            // shared material is still null at this moment and `new Material(null)` throws
            // ArgumentNullException, which would abort the entire button build. The cream
            // label on the dark fill with a bold weight stays legible without an outline.

            // Strip any I2 Localize component the parent might auto-attach to fresh TMPs —
            // mod-owned labels use ModLocalization and RefreshLocalizedLabels on language change.
            var localizeType = Type.GetType("I2.Loc.Localize, I2Localization") ?? Type.GetType("I2.Loc.Localize, Assembly-CSharp");
            if (localizeType != null)
            {
                var existing = textGo.GetComponent(localizeType);
                if (existing != null) UnityEngine.Object.Destroy(existing);
            }

            // --- Hover / press feedback -------------------------------------------------
            var tint = go.AddComponent<HoverTint>();
            tint.Fill = fill;
            tint.Root = rt;
            tint.Normal = normal;
            tint.Hover = hover;
            tint.Pressed = pressed;

            return go;
        }

        // ---- helpers ------------------------------------------------------------------
        private static Image AddChildImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static void StretchFill(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        /// <summary>
        /// Swaps the tintable Fill colour and nudges the root scale on pointer events to
        /// give hover lift + press sink without ever rebuilding a sprite.
        /// </summary>
        private sealed class HoverTint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
        {
            public Image Fill;
            public RectTransform Root;
            public Color Normal;
            public Color Hover;
            public Color Pressed;

            private const float HoverScale = 1.04f;
            private const float PressedScale = 0.97f;
            private bool _isOver;

            public void OnPointerEnter(PointerEventData _) { _isOver = true; Apply(Hover, HoverScale); }
            public void OnPointerExit(PointerEventData _) { _isOver = false; Apply(Normal, 1f); }
            public void OnPointerDown(PointerEventData _) { Apply(Pressed, PressedScale); }
            public void OnPointerUp(PointerEventData _) { Apply(_isOver ? Hover : Normal, _isOver ? HoverScale : 1f); }

            private void Apply(Color fill, float scale)
            {
                if (Fill != null) Fill.color = fill;
                if (Root != null) Root.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
