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
        private bool _dialogIsResetAll;
        private ProfessionType? _dialogProfession;
        private int _dialogEstimatedPoints;
        private int _dialogEstimatedCost;

        private PendingSimulation _pendingSimulation;

        internal sealed class PendingSimulationInfo
        {
            public ProfessionType Profession;
            public int RefundedPoints;
            public int Cost;
            public bool CanAfford;
            public string CostLabel;
        }

        private sealed class PendingSimulation
        {
            public Skills Skills;
            public ProfessionType Profession;
            public int RefundedPoints;
        }

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
            CancelPendingSimulation(silent: true);
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
            var skills = TryResolveSkills();
            if (skills == null)
                return;
            BeginResetFlow(skills, profession, bypassConfirm);
        }

        public void TryUndoCurrentTab(ProfessionType profession)
        {
            var skills = TryResolveSkills();
            if (skills == null)
                return;
            PerformUndo(skills, profession);
        }

        public bool HasUndo(ProfessionType profession) => _resetService.HasUndo(profession);

        /// <summary>
        /// Resolve which profession panel is currently open from the cached Skills instance.
        /// </summary>
        public ProfessionType? TryGetActiveProfessionTab()
        {
            var skills = TryResolveSkills();
            if (skills == null)
                return null;

            try
            {
                foreach (var profession in ProfessionUiMap.OrderedProfessions)
                {
                    string fieldName = ProfessionUiMap.ResolvePanelFieldName(profession);
                    var panel = fieldName == null ? null : AccessTools.Field(typeof(Skills), fieldName)?.GetValue(skills) as Component;
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

        internal Skills TryResolveSkills()
        {
            if (_activeSkills != null)
                return _activeSkills;
            return UnityEngine.Object.FindObjectOfType<Skills>();
        }

        internal bool TryGetEstimate(ProfessionType profession, out int refund, out int cost, out bool canAfford, out string costLabel)
        {
            refund = 0;
            cost = 0;
            canAfford = true;
            costLabel = string.Empty;

            var skills = TryResolveSkills();
            if (skills == null)
                return false;

            refund = ResolveEstimatedRefund(skills, profession);
            cost = _costService.CalculateCost(refund);
            canAfford = cost <= 0 || _costService.CanAfford(refund, out _, out _);
            costLabel = _costService.CostLabel(refund);
            return true;
        }

        internal bool TrySimulateProfession(ProfessionType profession, out string errorMessage)
        {
            errorMessage = null;
            var skills = TryResolveSkills();
            if (skills == null)
            {
                errorMessage = "Open the Skills panel in-game first.";
                return false;
            }

            if (_pendingSimulation != null)
            {
                errorMessage = "A simulation is already pending. Apply or revert it first.";
                return false;
            }

            int estimatedPoints = ResolveEstimatedRefund(skills, profession);
            if (estimatedPoints <= 0)
            {
                errorMessage = "No allocated skill points to refund for that profession.";
                return false;
            }

            if (!_resetService.ResetProfession(skills, profession, out var refunded))
            {
                errorMessage = "Dry-run reset failed.";
                return false;
            }

            _pendingSimulation = new PendingSimulation
            {
                Skills = skills,
                Profession = profession,
                RefundedPoints = refunded
            };

            if (_injectors.TryGetValue(profession, out var injector))
                injector.SetUndoVisible(_config.EnableUndo.Value);

            _log?.LogInfo($"[Respec] DevTools simulation started for {profession}; refunded {refunded} point(s) (preview only).");
            return true;
        }

        internal bool TryGetPendingSimulation(out PendingSimulationInfo info)
        {
            info = null;
            if (_pendingSimulation == null)
                return false;

            var pending = _pendingSimulation;
            int cost = _costService.CalculateCost(pending.RefundedPoints);
            info = new PendingSimulationInfo
            {
                Profession = pending.Profession,
                RefundedPoints = pending.RefundedPoints,
                Cost = cost,
                CanAfford = cost <= 0 || _costService.CanAfford(pending.RefundedPoints, out _, out _),
                CostLabel = _costService.CostLabel(pending.RefundedPoints)
            };
            return true;
        }

        internal bool TryCommitPendingSimulation(out string errorMessage)
        {
            errorMessage = null;
            if (_pendingSimulation == null)
            {
                errorMessage = "No simulation pending.";
                return false;
            }

            var pending = _pendingSimulation;
            if (!CommitAfterReset(pending.Skills, pending.Profession, pending.RefundedPoints))
            {
                CancelPendingSimulation(silent: true);
                errorMessage = "Could not apply reset (cost check or deduction failed). Simulation reverted.";
                return false;
            }

            _log?.LogInfo($"[Respec] Simulation applied for {pending.Profession}; refunded {pending.RefundedPoints} point(s).");
            ClearPendingSimulation();
            return true;
        }

        internal bool TryCancelPendingSimulation(out string errorMessage)
        {
            errorMessage = null;
            if (_pendingSimulation == null)
            {
                errorMessage = "No simulation pending.";
                return false;
            }

            CancelPendingSimulation();
            return true;
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
            if (!injector.TryAttach(panel, _config.EnableResetAll.Value))
                return;

            injector.OnResetClicked += () => BeginResetFlow(skills, profession, bypassConfirm: _config.ShiftSkipsConfirmation.Value && IsShiftHeld());
            if (_config.EnableResetAll.Value)
                injector.OnResetAllClicked += () => BeginResetAllFlow(skills, _config.ShiftSkipsConfirmation.Value && IsShiftHeld());
            injector.OnUndoClicked += () =>
            {
                if (_pendingSimulation != null && _pendingSimulation.Profession == profession)
                    CancelPendingSimulation();
                else
                    PerformUndo(skills, profession);
            };
            injector.SetUndoVisible(_config.EnableUndo.Value && (_resetService.HasUndo(profession) || (_pendingSimulation != null && _pendingSimulation.Profession == profession)));
            _injectors[profession] = injector;

            EnsureDialog(panel);
        }

        private void BeginResetFlow(Skills skills, ProfessionType profession, bool bypassConfirm)
        {
            if (_pendingSimulation != null)
            {
                _log?.LogWarning("[Respec] Revert the pending simulation before performing a real reset.");
                return;
            }

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

            if (!CommitAfterReset(skills, profession, refunded))
                return false;

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

        private bool CommitAfterReset(Skills skills, ProfessionType profession, int refunded)
        {
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

            return true;
        }

        private void PerformUndo(Skills skills, ProfessionType profession)
        {
            if (_pendingSimulation != null && _pendingSimulation.Profession == profession)
            {
                CancelPendingSimulation();
                return;
            }

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
            if (_pendingSimulation != null)
            {
                _log?.LogWarning("[Respec] Revert the pending simulation before Reset All.");
                return;
            }

            int totalEstimated = 0;
            int totalEstimatedCost = 0;
            foreach (var profession in ProfessionUiMap.OrderedProfessions)
            {
                int estimate = ResolveEstimatedRefund(skills, profession);
                totalEstimated += estimate;
                totalEstimatedCost += _costService.CalculateCost(estimate);
            }

            if (totalEstimatedCost > 0 && !_costService.CanAfford(totalEstimated, out var totalBalance, out _))
            {
                _log?.LogWarning($"[Respec] Cannot afford Reset All preflight: total cost={totalEstimatedCost}, balance={totalBalance}.");
                return;
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

            ShowResetAllDialog(skills, totalEstimated, totalEstimatedCost);
        }

        private void PerformResetAll(Skills skills)
        {
            int processed = 0;
            var successful = new List<ProfessionType>();
            foreach (var profession in ProfessionUiMap.OrderedProfessions)
            {
                bool ok = PerformReset(skills, profession, ResolveEstimatedRefund(skills, profession));
                if (!ok)
                {
                    for (int i = successful.Count - 1; i >= 0; i--)
                        PerformUndo(skills, successful[i]);

                    _log?.LogWarning($"[Respec] Reset All aborted on {profession}; rolled back {successful.Count} profession(s).");
                    return;
                }

                successful.Add(profession);
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

            Canvas canvas = sceneAnchor.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null)
                return;

            _dialog = ConfirmResetDialog.BuildUnder(canvas.transform);
            _dialog.Dismissed += ClearDialogState;
        }

        private void CancelPendingSimulation(bool silent = false)
        {
            if (_pendingSimulation == null)
                return;

            var pending = _pendingSimulation;
            if (_resetService.UndoLastReset(pending.Skills, pending.Profession, out _))
            {
                if (!silent)
                    _log?.LogInfo($"[Respec] Simulation reverted for {pending.Profession}.");
            }
            else if (!silent)
            {
                _log?.LogWarning($"[Respec] Failed to revert simulation for {pending.Profession}.");
            }

            ClearPendingSimulation();
        }

        private void ClearPendingSimulation()
        {
            if (_pendingSimulation == null)
                return;

            var profession = _pendingSimulation.Profession;
            _pendingSimulation = null;

            if (_injectors.TryGetValue(profession, out var injector))
                injector.SetUndoVisible(_config.EnableUndo.Value && _resetService.HasUndo(profession));
        }

        private void ShowProfessionDialog(Skills skills, ProfessionType profession, int estimatedPoints, int cost)
        {
            _dialogSkills = skills;
            _dialogIsResetAll = false;
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

        private void ShowResetAllDialog(Skills skills, int totalEstimated, int totalEstimatedCost)
        {
            _dialogSkills = skills;
            _dialogIsResetAll = true;
            _dialogProfession = null;
            _dialogEstimatedPoints = totalEstimated;
            _dialogEstimatedCost = totalEstimatedCost;
            _dialogOnConfirm = () =>
            {
                ClearDialogState();
                PerformResetAll(skills);
            };
            _dialog.Show(
                ModLocalization.T("respec.dialog.title.reset_all"),
                BuildResetAllBody(totalEstimated, totalEstimatedCost),
                _dialogOnConfirm);
        }

        private void RefreshOpenDialog()
        {
            if (_dialog == null || !_dialog.gameObject.activeInHierarchy || _dialogOnConfirm == null || _dialogSkills == null)
                return;

            if (_dialogIsResetAll)
            {
                _dialog.Show(
                    ModLocalization.T("respec.dialog.title.reset_all"),
                    BuildResetAllBody(_dialogEstimatedPoints, _dialogEstimatedCost),
                    _dialogOnConfirm);
            }
            else if (_dialogProfession.HasValue)
            {
                var profession = _dialogProfession.Value;
                _dialog.Show(
                    ModLocalization.T("respec.dialog.title.profession", ProfessionUiMap.GetDisplayName(profession)),
                    BuildConfirmBody(profession, _dialogEstimatedPoints, _dialogEstimatedCost),
                    _dialogOnConfirm);
            }
        }

        private void ClearDialogState()
        {
            _dialogSkills = null;
            _dialogOnConfirm = null;
            _dialogProfession = null;
            _dialogIsResetAll = false;
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

        private string BuildResetAllBody(int totalEstimated, int totalEstimatedCost)
        {
            string body = ModLocalization.T("respec.dialog.body.reset_all.intro", totalEstimated);
            body += totalEstimatedCost > 0
                ? ModLocalization.T("respec.dialog.body.reset_all.cost", _costService.CostLabel(totalEstimated))
                : ModLocalization.T("respec.dialog.body.reset_all.free");
            body += _config.EnableUndo.Value
                ? ModLocalization.T("respec.dialog.body.reset_all.undo_per_prof")
                : ModLocalization.T("respec.dialog.body.reset_all.undo_disabled");
            return body;
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
