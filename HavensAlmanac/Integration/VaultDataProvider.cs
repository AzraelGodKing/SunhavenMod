using System;
using System.Collections.Generic;
using HavensAlmanac.Data;
using TheVault;
using TheVault.Vault;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    public class VaultDataProvider : IModDataProvider
    {
        public string ModName => "Vault";
        public string ModIcon => "\u26BF";

        private int _currencyCount;
        private Dictionary<string, int> _currencies = new Dictionary<string, int>();
        private string _hudSummary = "Loading...";
        private bool _isReady;

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;

        public void Refresh()
        {
            try
            {
                var vm = TheVault.Plugin.GetVaultManager();
                if (vm == null) { _isReady = false; return; }

                _currencies = vm.GetAllNonZeroCurrencies() ?? new Dictionary<string, int>();
                _currencyCount = _currencies.Count;

                _hudSummary = _currencyCount == 0
                    ? "Vault empty"
                    : $"{_currencyCount} currenc{(_currencyCount != 1 ? "ies" : "y")} stored";
                _isReady = true;
            }
            catch (Exception ex)
            {
                _hudSummary = "Error";
                _isReady = false;
                HavensAlmanac.Plugin.Log?.LogWarning($"[VaultProvider] Refresh error: {ex.Message}");
            }
        }

        public void DrawDashboardSection()
        {
            if (_currencyCount == 0)
            {
                GUILayout.Label("Vault is empty.");
                return;
            }

            GUILayout.Label($"Stored Currencies ({_currencyCount}):");
            GUILayout.Space(4);

            int shown = 0;
            foreach (var kvp in _currencies)
            {
                if (shown >= 10)
                {
                    GUILayout.Label($"  ... and {_currencyCount - 10} more");
                    break;
                }
                GUILayout.Label($"  {kvp.Key}: {kvp.Value}");
                shown++;
            }
        }

        public bool DrawBriefingSection()
        {
            if (_currencyCount == 0) return false;

            GUILayout.Label($"Vault holds {_currencyCount} currenc{(_currencyCount != 1 ? "ies" : "y")}.");
            return true;
        }
    }
}
