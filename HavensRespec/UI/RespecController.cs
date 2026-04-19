using System.Collections.Generic;
using BepInEx.Logging;
using HavensRespec.Config;
using HavensRespec.Patches;
using HavensRespec.Services;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Wish;
using UIButton = UnityEngine.UI.Button;

namespace HavensRespec.UI
{
    /// <summary>
    /// Coordinates per-panel Reset/Undo button injection, the confirmation dialog, the cost
    /// deduction, and the undo stack. One <see cref="RespecController"/> is created by the
    /// plugin and lives for the game session — it re-hooks panels on every Skills build.
    /// </summary>
    internal sealed class RespecController
    {
        private readonly ManualLogSource _log;
        private readonly RespecConfig _config;
        private readonly SkillResetService _resetService;
        private readonly CostService _costService;

        private readonly Dictionary<ProfessionType, RespecButtonInjector> _injectors = new Dictionary<ProfessionType, RespecButtonInjector>();
        private Skills _activeSkills;
        private ConfirmResetDialog _dialog;
        private GameObject _resetAllButton;

        public RespecController(ManualLogSource log, RespecConfig config, SkillResetService resetService, CostService costService)
        {
            _log = log;
            _config = config;
            _resetService = resetService;
            _costService = costService;
        }

        public void Install()
        {
            SkillsSetupProfessionPatch.OnSetup = HandleSetupProfession;
        }

        public void Uninstall()
        {
            SkillsSetupProfessionPatch.OnSetup = null;
            foreach (var injector in _injectors.Values)
                injector.Destroy();
            _injectors.Clear();
            if (_dialog != null)
            {
                UnityEngine.Object.Destroy(_dialog.gameObject);
                _dialog = null;
            }
            if (_resetAllButton != null)
            {
                UnityEngine.Object.Destroy(_resetAllButton);
                _resetAllButton = null;
            }
        }

        /// <summary>
        /// Trigger a reset of <paramref name="profession"/> from an external caller
        /// (e.g. a hotkey) using the same confirm + cost flow as the UI button.
        /// </summary>
        public void TryResetCurrentTab(ProfessionType profession, bool bypassConfirm)
        {
            if (_activeSkills == null)
                return;
            BeginResetFlow(_activeSkills, profession, bypassConfirm);
        }

        public void TryUndoCurrentTab(ProfessionType profession)
        {
            if (_activeSkills == null)
                return;
            PerformUndo(_activeSkills, profession);
        }

        public bool HasUndo(ProfessionType profession) => _resetService.HasUndo(profession);

        /// <summary>
        /// Resolve which profession panel is currently open from the cached Skills instance.
        /// </summary>
        public ProfessionType? TryGetActiveProfessionTab()
        {
            if (_activeSkills == null)
                return null;

            try
            {
                foreach (var profession in ProfessionUiMap.OrderedProfessions)
                {
                    string fieldName = ProfessionUiMap.ResolvePanelFieldName(profession);
                    var panel = fieldName == null ? null : AccessTools.Field(typeof(Skills), fieldName)?.GetValue(_activeSkills) as Component;
                    if (panel != null && panel.gameObject.activeInHierarchy)
                        return profession;
                }
            }
            catch (System.Exception ex)
            {
                _log?.LogDebug($"[Respec] TryGetActiveProfessionTab failed: {ex.Message}");
            }

            return null;
        }

        // ------------------------------------------------------------------- internals

        private void HandleSetupProfession(Skills skills, ProfessionType profession, SkillTree panel)
        {
            _activeSkills = skills;

            if (_injectors.TryGetValue(profession, out var existing))
            {
                existing.Destroy();
                _injectors.Remove(profession);
            }

            if (!_config.InjectButtons.Value)
                return;

            var injector = new RespecButtonInjector(_log);
            if (!injector.TryAttach(panel))
                return;

            injector.OnResetClicked += () => BeginResetFlow(skills, profession, bypassConfirm: _config.ShiftSkipsConfirmation.Value && IsShiftHeld());
            injector.OnUndoClicked += () => PerformUndo(skills, profession);
            injector.SetUndoVisible(_config.EnableUndo.Value && _resetService.HasUndo(profession));
            _injectors[profession] = injector;

            EnsureDialog(panel);
            EnsureResetAllButton(skills, panel);
        }

        private void BeginResetFlow(Skills skills, ProfessionType profession, bool bypassConfirm)
        {
            // Preflight point estimate, used only to sanity-check affordability and to word
            // the confirmation dialog. The real refund count comes from ResetProfession's
            // node-walk and is authoritative.
            int estimatedPoints = _resetService.GetAllocatedPoints(profession);

            if (!_costService.CanAfford(estimatedPoints, out var balance, out var cost))
            {
                _log?.LogWarning($"[Respec] Cannot afford reset of {profession}: cost={cost}, balance={balance}.");
                return;
            }

            if (!_config.RequireConfirmation.Value || bypassConfirm)
            {
                PerformReset(skills, profession, estimatedPoints);
                return;
            }

            EnsureDialog(skills);
            if (_dialog == null)
            {
                PerformReset(skills, profession, estimatedPoints);
                return;
            }

            string body = BuildConfirmBody(profession, estimatedPoints, cost);
            _dialog.Show(
                title: $"Reset {profession}?",
                body: body,
                onConfirm: () => PerformReset(skills, profession, estimatedPoints));
        }

        private void PerformReset(Skills skills, ProfessionType profession, int estimatedPoints)
        {
            if (!_resetService.ResetProfession(skills, profession, out var refunded))
            {
                _log?.LogWarning($"[Respec] Reset of {profession} failed.");
                return;
            }

            // Cost is charged from the authoritative, post-reset refunded point count.
            int actualCost = _costService.CalculateCost(refunded);
            if (actualCost > 0)
            {
                if (!_costService.CanAfford(refunded, out var balance, out _))
                {
                    _log?.LogWarning($"[Respec] Actual reset cost check failed for {profession}: cost={actualCost}, balance={balance}. Rolling back reset.");
                    _resetService.UndoLastReset(skills, profession, out _);
                    return;
                }

                if (!_costService.TryDeduct(refunded))
                {
                    _log?.LogWarning($"[Respec] Cost deduction failed for {profession} (actual cost {actualCost}). Rolling back reset.");
                    _resetService.UndoLastReset(skills, profession, out _);
                    return;
                }

                _resetService.AttachUndoCharge(profession, actualCost, _config.CostMode.Value);
            }

            if (_injectors.TryGetValue(profession, out var injector))
                injector.SetUndoVisible(_config.EnableUndo.Value && _resetService.HasUndo(profession));

            if (refunded == 0)
            {
                _log?.LogInfo($"[Respec] {profession} reset complete; nothing was allocated (0 active node(s) — skill-points counter unchanged).");
            }
            else
            {
                _log?.LogInfo($"[Respec] {profession} reset complete; refunded {refunded} point(s){(estimatedPoints != refunded ? $" (pre-flight estimate was {estimatedPoints})" : string.Empty)}.");
            }
        }

        private void PerformUndo(Skills skills, ProfessionType profession)
        {
            if (!_resetService.UndoLastReset(skills, profession, out var snapshot))
                return;

            if (snapshot != null && snapshot.ChargedCost > 0 && snapshot.ChargedCostMode != RespecCostMode.None)
            {
                if (!_costService.TryRefund(snapshot.ChargedCost, snapshot.ChargedCostMode))
                {
                    _log?.LogWarning($"[Respec] Undo restored {profession} nodes but failed to refund {snapshot.ChargedCost} ({snapshot.ChargedCostMode}).");
                }
                else
                {
                    _log?.LogInfo($"[Respec] Undo refunded {snapshot.ChargedCost} ({snapshot.ChargedCostMode}) for {profession}.");
                }
            }

            if (_injectors.TryGetValue(profession, out var injector))
                injector.SetUndoVisible(_config.EnableUndo.Value && _resetService.HasUndo(profession));
        }

        private void BeginResetAllFlow(Skills skills, bool bypassConfirm)
        {
            int totalEstimated = 0;
            int totalEstimatedCost = 0;
            foreach (var profession in ProfessionUiMap.OrderedProfessions)
            {
                int estimate = _resetService.GetAllocatedPoints(profession);
                totalEstimated += estimate;
                totalEstimatedCost += _costService.CalculateCost(estimate);
            }

            if (!_config.RequireConfirmation.Value || bypassConfirm)
            {
                PerformResetAll(skills);
                return;
            }

            EnsureDialog(skills);
            if (_dialog == null)
            {
                PerformResetAll(skills);
                return;
            }

            string body =
                $"This will reset all profession trees.\n\nEstimated total refund: {totalEstimated} point(s)." +
                (totalEstimatedCost > 0 ? $"\nEstimated total cost: {_costService.CostLabel(totalEstimated)} (final charge uses actual refunded points)." : "\nCost: Free") +
                (_config.EnableUndo.Value ? "\n\nUndo remains per-profession and will refund each profession's charged cost." : "\n\nUndo is disabled in config — this cannot be reversed.");

            _dialog.Show(
                title: "Reset all professions?",
                body: body,
                onConfirm: () => PerformResetAll(skills));
        }

        private void PerformResetAll(Skills skills)
        {
            int processed = 0;
            foreach (var profession in ProfessionUiMap.OrderedProfessions)
            {
                PerformReset(skills, profession, _resetService.GetAllocatedPoints(profession));
                processed++;
            }

            _log?.LogInfo($"[Respec] Reset All complete: processed {processed} profession(s).");
        }

        private void EnsureDialog(Component sceneAnchor)
        {
            if (_dialog != null)
                return;
            if (sceneAnchor == null)
                return;

            // Find the nearest Canvas above the Skills panel so the dialog inherits the same
            // scaler / sorting as the game's skill UI. Fallback to the top-most root canvas.
            Canvas canvas = sceneAnchor.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null)
                return;

            _dialog = ConfirmResetDialog.BuildUnder(canvas.transform);
        }

        private string BuildConfirmBody(ProfessionType profession, int pointsRefunded, int cost)
        {
            string line1 = pointsRefunded > 0
                ? $"This will clear every allocated node in {profession}. Estimated refund: {pointsRefunded} skill point{(pointsRefunded == 1 ? string.Empty : "s")}."
                : $"This will clear every allocated node in {profession}.";
            string costLine = cost > 0
                ? $"\n\nEstimated cost: {_costService.CostLabel(pointsRefunded)} (final charge uses actual refunded points)."
                : string.Empty;
            string undoLine = _config.EnableUndo.Value
                ? "\n\nYou can press \"Undo\" to restore your previous allocation (and any charged cost) until the game closes."
                : "\n\nUndo is disabled in config — this cannot be reversed.";
            return line1 + costLine + undoLine;
        }

        private static bool IsShiftHeld()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        private void EnsureResetAllButton(Skills skills, SkillTree panel)
        {
            if (!_config.EnableResetAll.Value || !_config.InjectButtons.Value || panel == null)
            {
                if (_resetAllButton != null)
                {
                    UnityEngine.Object.Destroy(_resetAllButton);
                    _resetAllButton = null;
                }
                return;
            }

            if (_resetAllButton != null)
                return;

            var parent = panel.transform.parent;
            if (parent == null)
                return;

            var go = new GameObject("HavensRespec_ResetAllButton");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(130f, 28f);
            rt.anchoredPosition = new Vector2(0f, -8f);
            rt.localScale = Vector3.one;

            var image = go.AddComponent<Image>();
            image.sprite = RespecStyle.SolidRounded(Color.white, new Color(0f, 0f, 0f, 0f), 32, 0, 8);
            image.type = Image.Type.Sliced;
            image.color = RespecStyle.Danger;

            var button = go.AddComponent<UIButton>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => BeginResetAllFlow(skills, _config.ShiftSkipsConfirmation.Value && IsShiftHeld()));

            var tint = go.AddComponent<HoverTint>();
            tint.Target = image;
            tint.Normal = RespecStyle.Danger;
            tint.Hover = RespecStyle.DangerHover;
            tint.Pressed = RespecStyle.DangerPressed;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.fontSize = 12f;
            label.fontStyle = TMPro.FontStyles.Bold;
            label.enableWordWrapping = false;
            label.color = RespecStyle.Parchment;
            label.text = "RESET ALL";
            label.raycastTarget = false;

            _resetAllButton = go;
        }

        private sealed class HoverTint : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
        {
            public Image Target;
            public Color Normal;
            public Color Hover;
            public Color Pressed;

            private bool _isOver;

            public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData _) { _isOver = true; Apply(Hover); }
            public void OnPointerExit(UnityEngine.EventSystems.PointerEventData _) { _isOver = false; Apply(Normal); }
            public void OnPointerDown(UnityEngine.EventSystems.PointerEventData _) { Apply(Pressed); }
            public void OnPointerUp(UnityEngine.EventSystems.PointerEventData _) { Apply(_isOver ? Hover : Normal); }

            private void Apply(Color fill)
            {
                if (Target == null) return;
                Target.color = fill;
            }
        }
    }
}
