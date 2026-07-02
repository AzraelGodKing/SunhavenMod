using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HavensRespec.Config;
using HavensRespec.Patches;
using HavensRespec.Services;
using HarmonyLib;
using SunhavenMods.Shared;
using UnityEngine;
using Wish;

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
        private Skills _dialogSkills;
        private Action _dialogOnConfirm;
        private ProfessionType? _dialogProfession;
        private int _dialogEstimatedPoints;
        private int _dialogEstimatedCost;

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

        public void RefreshLocalizedUi()
        {
            foreach (var injector in _injectors.Values)
                injector.RefreshLocalizedLabels();
            RefreshOpenDialog();
            _dialog?.RefreshLocalizedLabels();
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
        }

        private void BeginResetFlow(Skills skills, ProfessionType profession, bool bypassConfirm)
        {
            // Preflight point estimate, used only to sanity-check affordability and to word
            // the confirmation dialog. The real refund count comes from ResetProfession's
            // node-walk and is authoritative.
            int estimatedPoints = ResolveEstimatedRefund(skills, profession);

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

            ShowProfessionDialog(skills, profession, estimatedPoints, cost);
        }

        private bool PerformReset(Skills skills, ProfessionType profession, int estimatedPoints)
        {
            if (!_resetService.ResetProfession(skills, profession, out var refunded))
            {
                _log?.LogWarning($"[Respec] Reset of {profession} failed.");
                return false;
            }

            // Cost is charged from the authoritative, post-reset refunded point count.
            int actualCost = _costService.CalculateCost(refunded);
            if (actualCost > 0)
            {
                if (!_costService.CanAfford(refunded, out var balance, out _))
                {
                    _log?.LogWarning($"[Respec] Actual reset cost check failed for {profession}: cost={actualCost}, balance={balance}. Rolling back reset.");
                    _resetService.UndoLastReset(skills, profession, out _);
                    return false;
                }

                if (!_costService.TryDeduct(refunded))
                {
                    _log?.LogWarning($"[Respec] Cost deduction failed for {profession} (actual cost {actualCost}). Rolling back reset.");
                    _resetService.UndoLastReset(skills, profession, out _);
                    return false;
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
            return true;
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
            _dialog.Dismissed += ClearDialogState;
        }

        private void ShowProfessionDialog(Skills skills, ProfessionType profession, int estimatedPoints, int cost)
        {
            _dialogSkills = skills;
            _dialogProfession = profession;
            _dialogEstimatedPoints = estimatedPoints;
            _dialogEstimatedCost = cost;
            _dialogOnConfirm = () =>
            {
                ClearDialogState();
                PerformReset(skills, profession, estimatedPoints);
            };
            _dialog.Show(
                ModLocalization.T("respec.dialog.title.profession", ProfessionUiMap.GetDisplayName(profession)),
                BuildConfirmBody(profession, estimatedPoints, cost),
                _dialogOnConfirm);
        }

        private void RefreshOpenDialog()
        {
            if (_dialog == null || !_dialog.gameObject.activeInHierarchy || _dialogOnConfirm == null || _dialogSkills == null)
                return;

            if (!_dialogProfession.HasValue)
                return;

            var profession = _dialogProfession.Value;
            _dialog.Show(
                ModLocalization.T("respec.dialog.title.profession", ProfessionUiMap.GetDisplayName(profession)),
                BuildConfirmBody(profession, _dialogEstimatedPoints, _dialogEstimatedCost),
                _dialogOnConfirm);
        }

        private void ClearDialogState()
        {
            _dialogSkills = null;
            _dialogOnConfirm = null;
            _dialogProfession = null;
        }

        private string BuildConfirmBody(ProfessionType profession, int pointsRefunded, int cost)
        {
            string professionName = ProfessionUiMap.GetDisplayName(profession);
            string line1 = pointsRefunded > 1
                ? ModLocalization.T("respec.dialog.body.refund_points", professionName, pointsRefunded)
                : pointsRefunded == 1
                    ? ModLocalization.T("respec.dialog.body.refund_one", professionName)
                    : ModLocalization.T("respec.dialog.body.no_refund", professionName);
            string costLine = cost > 0
                ? ModLocalization.T("respec.dialog.body.cost", _costService.CostLabel(pointsRefunded))
                : string.Empty;
            string undoLine = _config.EnableUndo.Value
                ? ModLocalization.T("respec.dialog.body.undo_enabled")
                : ModLocalization.T("respec.dialog.body.undo_disabled");
            return line1 + costLine + undoLine;
        }

        private static bool IsShiftHeld()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        private int ResolveEstimatedRefund(Skills skills, ProfessionType profession)
        {
            if (_resetService.TryEstimateExactRefund(skills, profession, out int exact))
                return Mathf.Max(0, exact);
            return Mathf.Max(0, _resetService.GetAllocatedPoints(profession));
        }
    }
}
