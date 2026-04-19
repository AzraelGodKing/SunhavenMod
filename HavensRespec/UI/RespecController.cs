using System.Collections.Generic;
using BepInEx.Logging;
using HavensRespec.Config;
using HavensRespec.Patches;
using HavensRespec.Services;
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
            // Charge the estimated cost up front; if the actual refund differs (rare), the
            // discrepancy is logged but we don't try to reconcile the cost mid-flow since
            // costs scale on estimatedPoints which was shown in the dialog.
            if (estimatedPoints > 0 && !_costService.TryDeduct(estimatedPoints))
            {
                _log?.LogWarning($"[Respec] Cost deduction failed for {profession}; aborting reset.");
                return;
            }

            if (!_resetService.ResetProfession(skills, profession, out var refunded))
            {
                _log?.LogWarning($"[Respec] Reset of {profession} failed.");
                return;
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
            if (!_resetService.UndoLastReset(skills, profession))
                return;
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
        }

        private string BuildConfirmBody(ProfessionType profession, int pointsRefunded, int cost)
        {
            string line1 = pointsRefunded > 0
                ? $"This will clear every allocated node in {profession} and refund {pointsRefunded} skill point{(pointsRefunded == 1 ? string.Empty : "s")}."
                : $"This will clear every allocated node in {profession}.";
            string costLine = cost > 0
                ? $"\n\nCost: {_costService.CostLabel(pointsRefunded)}"
                : string.Empty;
            string undoLine = _config.EnableUndo.Value
                ? "\n\nYou can press \"Undo\" to restore your previous allocation until the game closes."
                : "\n\nUndo is disabled in config — this cannot be reversed.";
            return line1 + costLine + undoLine;
        }

        private static bool IsShiftHeld()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }
    }
}
