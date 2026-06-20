using System;
using System.Collections.Generic;
using HavensAlmanac.Data;
using SunhavenMods.Shared;
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

        // Currency balances are persistent state rather than daily-relevant
        // information; the briefing never reminds about vault contents.
        public bool HasBriefingContent => false;

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
                GUILayout.Label(ModLocalization.T("almanac.provider.vault.empty"));
                return;
            }

            GUILayout.Label(ModLocalization.T("almanac.provider.vault.stored", _currencyCount));
            GUILayout.Space(4);

            int shown = 0;
            foreach (var kvp in _currencies)
            {
                if (shown >= 10)
                {
                    GUILayout.Label(ModLocalization.T("almanac.provider.vault.more", _currencyCount - 10));
                    break;
                }
                GUILayout.Label($"  {kvp.Key}: {kvp.Value}");
                shown++;
            }
        }

        public bool DrawBriefingSection()
        {
            if (_currencyCount == 0) return false;

            string currencyWord = _currencyCount == 1
                ? ModLocalization.T("almanac.provider.vault.currency")
                : ModLocalization.T("almanac.provider.vault.currencies");
            GUILayout.Label(ModLocalization.T("almanac.provider.vault.briefing", _currencyCount, currencyWord));
            return true;
        }
    }
}
