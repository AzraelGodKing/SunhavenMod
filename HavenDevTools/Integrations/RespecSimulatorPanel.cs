using System;
using System.Reflection;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavenDevTools.Integrations
{
    /// <summary>
    /// Suite-tab dry-run respec preview via <c>HavensRespec.DevTools.RespecDevToolsApi</c> (soft dependency).
    /// </summary>
    internal static class RespecSimulatorPanel
    {
        private static Type _apiType;
        private static int _selectedProfessionIndex;
        private static string _statusMessage = string.Empty;

        public static void Draw(GUIStyle box, GUIStyle button, GUIStyle label, GUIStyle sectionHeader)
        {
            GUILayout.BeginVertical(box);
            GUILayout.Label(ModLocalization.T("devtools.respec.simulator.title"), sectionHeader);

            if (!Plugin.HasHavensRespec)
            {
                GUILayout.Label(ModLocalization.T("azrael.respec.unavailable"), label);
                GUILayout.EndVertical();
                return;
            }

            if (!EnsureApi(out string apiError))
            {
                GUILayout.Label(apiError, label);
                GUILayout.EndVertical();
                return;
            }

            if (!InvokeStaticBool("IsReady"))
            {
                GUILayout.Label(ModLocalization.T("devtools.respec.simulator.not_ready"), label);
                GUILayout.EndVertical();
                return;
            }

            int professionCount = InvokeStaticInt("ProfessionCount");
            if (professionCount <= 0)
            {
                GUILayout.Label(ModLocalization.T("devtools.respec.simulator.not_ready"), label);
                GUILayout.EndVertical();
                return;
            }

            var professionNames = new string[professionCount];
            for (int i = 0; i < professionCount; i++)
                professionNames[i] = InvokeStaticStringMethod("GetProfessionName", i);

            int activeIndex = InvokeStaticIntMethod("GetActiveProfessionIndex");
            if (activeIndex >= 0 && activeIndex < professionCount)
                _selectedProfessionIndex = activeIndex;

            _selectedProfessionIndex = Mathf.Clamp(_selectedProfessionIndex, 0, professionCount - 1);
            _selectedProfessionIndex = GUILayout.SelectionGrid(_selectedProfessionIndex, professionNames, 2, button);
            GUILayout.Space(6);

            if (InvokeHasPending(
                    out int pendingIndex,
                    out int refunded,
                    out int cost,
                    out bool canAfford,
                    out string costLabel))
            {
                DrawPendingState(button, label, professionNames, pendingIndex, refunded, cost, canAfford, costLabel);
            }
            else
            {
                DrawEstimateState(button, label);
            }

            if (!string.IsNullOrEmpty(_statusMessage))
                GUILayout.Label(_statusMessage, label);

            GUILayout.EndVertical();
        }

        private static void DrawEstimateState(GUIStyle button, GUIStyle label)
        {
            if (TryGetEstimate(_selectedProfessionIndex, out int refund, out int cost, out bool canAfford, out string costLabel))
            {
                GUILayout.Label(ModLocalization.T("devtools.respec.simulator.estimate_refund", refund), label);
                GUILayout.Label(
                    cost > 0
                        ? ModLocalization.T("devtools.respec.simulator.estimate_cost", costLabel)
                        : ModLocalization.T("devtools.respec.simulator.estimate_free"),
                    label);
                if (cost > 0 && !canAfford)
                    GUILayout.Label(ModLocalization.T("devtools.respec.simulator.cannot_afford"), label);
            }

            GUILayout.Space(4);
            if (GUILayout.Button(ModLocalization.T("devtools.respec.simulator.simulate"), button))
            {
                if (InvokeTrySimulate(_selectedProfessionIndex, out string error))
                    _statusMessage = ModLocalization.T("devtools.respec.simulator.started");
                else
                    _statusMessage = string.IsNullOrEmpty(error)
                        ? ModLocalization.T("devtools.respec.simulator.failed")
                        : error;
            }
        }

        private static void DrawPendingState(
            GUIStyle button,
            GUIStyle label,
            string[] professionNames,
            int pendingIndex,
            int refunded,
            int cost,
            bool canAfford,
            string costLabel)
        {
            string professionName = pendingIndex >= 0 && pendingIndex < professionNames.Length
                ? professionNames[pendingIndex]
                : "?";

            GUILayout.Label(ModLocalization.T("devtools.respec.simulator.pending_intro", professionName), label);
            GUILayout.Label(ModLocalization.T("devtools.respec.simulator.pending_refund", refunded), label);
            GUILayout.Label(
                cost > 0
                    ? ModLocalization.T("devtools.respec.simulator.pending_cost", costLabel)
                    : ModLocalization.T("devtools.respec.simulator.estimate_free"),
                label);

            if (cost > 0 && !canAfford)
                GUILayout.Label(ModLocalization.T("devtools.respec.simulator.cannot_afford"), label);

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUI.enabled = canAfford || cost <= 0;
            if (GUILayout.Button(ModLocalization.T("devtools.respec.simulator.apply"), button))
            {
                if (InvokeTryCommit(out string error))
                    _statusMessage = ModLocalization.T("devtools.respec.simulator.applied");
                else
                    _statusMessage = string.IsNullOrEmpty(error)
                        ? ModLocalization.T("devtools.respec.simulator.failed")
                        : error;
            }
            GUI.enabled = true;

            if (GUILayout.Button(ModLocalization.T("devtools.respec.simulator.revert"), button))
            {
                if (InvokeTryCancel(out string error))
                    _statusMessage = ModLocalization.T("devtools.respec.simulator.reverted");
                else
                    _statusMessage = string.IsNullOrEmpty(error)
                        ? ModLocalization.T("devtools.respec.simulator.failed")
                        : error;
            }
            GUILayout.EndHorizontal();
        }

        private static bool EnsureApi(out string error)
        {
            error = null;
            if (_apiType != null)
                return true;

            _apiType = ReflectionHelper.FindType("RespecDevToolsApi", "HavensRespec.DevTools");
            if (_apiType == null)
            {
                error = ModLocalization.T("devtools.respec.simulator.api_missing");
                return false;
            }

            return true;
        }

        private static bool InvokeStaticBool(string memberName)
        {
            try
            {
                return ReflectionHelper.GetStaticValue(_apiType, memberName) is bool b && b;
            }
            catch
            {
                return false;
            }
        }

        private static int InvokeStaticInt(string memberName)
        {
            try
            {
                return ReflectionHelper.GetStaticValue(_apiType, memberName) is int i ? i : -1;
            }
            catch
            {
                return -1;
            }
        }

        private static int InvokeStaticIntMethod(string methodName, params object[] args)
        {
            try
            {
                object result = ReflectionHelper.InvokeStaticMethod(_apiType, methodName, args);
                return result is int i ? i : -1;
            }
            catch
            {
                return -1;
            }
        }

        private static string InvokeStaticStringMethod(string methodName, params object[] args)
        {
            try
            {
                return ReflectionHelper.InvokeStaticMethod(_apiType, methodName, args) as string ?? "?";
            }
            catch
            {
                return "?";
            }
        }

        private static bool TryGetEstimate(int professionIndex, out int refund, out int cost, out bool canAfford, out string costLabel)
        {
            refund = 0;
            cost = 0;
            canAfford = true;
            costLabel = string.Empty;

            try
            {
                var method = _apiType.GetMethod("TryGetEstimate", ReflectionHelper.AllBindingFlags);
                if (method == null)
                    return false;

                object[] args = { professionIndex, 0, 0, true, string.Empty };
                bool ok = method.Invoke(null, args) is bool b && b;
                if (!ok)
                    return false;

                refund = (int)args[1];
                cost = (int)args[2];
                canAfford = (bool)args[3];
                costLabel = args[4] as string ?? string.Empty;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool InvokeTrySimulate(int professionIndex, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var method = _apiType.GetMethod("TrySimulate", ReflectionHelper.AllBindingFlags);
                if (method == null)
                    return false;

                object[] args = { professionIndex, null };
                bool ok = method.Invoke(null, args) is bool b && b;
                errorMessage = args[1] as string;
                return ok;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private static bool InvokeHasPending(
            out int professionIndex,
            out int refunded,
            out int cost,
            out bool canAfford,
            out string costLabel)
        {
            professionIndex = -1;
            refunded = 0;
            cost = 0;
            canAfford = true;
            costLabel = string.Empty;

            try
            {
                var method = _apiType.GetMethod("HasPendingSimulation", ReflectionHelper.AllBindingFlags);
                if (method == null)
                    return false;

                object[] args = { -1, 0, 0, true, string.Empty };
                bool ok = method.Invoke(null, args) is bool b && b;
                if (!ok)
                    return false;

                professionIndex = (int)args[0];
                refunded = (int)args[1];
                cost = (int)args[2];
                canAfford = (bool)args[3];
                costLabel = args[4] as string ?? string.Empty;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool InvokeTryCommit(out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var method = _apiType.GetMethod("TryCommitSimulation", ReflectionHelper.AllBindingFlags);
                if (method == null)
                    return false;

                object[] args = { null };
                bool ok = method.Invoke(null, args) is bool b && b;
                errorMessage = args[0] as string;
                return ok;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private static bool InvokeTryCancel(out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var method = _apiType.GetMethod("TryCancelSimulation", ReflectionHelper.AllBindingFlags);
                if (method == null)
                    return false;

                object[] args = { null };
                bool ok = method.Invoke(null, args) is bool b && b;
                errorMessage = args[0] as string;
                return ok;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
