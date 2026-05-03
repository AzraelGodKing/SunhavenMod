using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using UnityEngine;
using Wish;

namespace TrinketFortune
{
    /// <summary>
    /// Resolves donation state via SMUT when available. Uses reflection to avoid hard dependency.
    /// </summary>
    public static class DonationHelper
    {
        private const string SmutGuid = "com.azraelgodking.sunhavenmuseumutilitytracker";

        private static object _donationManager;
        private static MethodInfo _hasDonatedByGameId;
        private static float _lastResolveAttemptRealtime = -100f;
        private const float ResolveRetrySeconds = 10f;

        public static bool IsAvailable
        {
            get
            {
                EnsureResolved();
                return _donationManager != null && _hasDonatedByGameId != null;
            }
        }

        static DonationHelper()
        {
            EnsureResolved(force: true);
        }

        private static void EnsureResolved(bool force = false)
        {
            if (!force && _donationManager != null && _hasDonatedByGameId != null)
                return;

            float now = Time.realtimeSinceStartup;
            if (!force && now - _lastResolveAttemptRealtime < ResolveRetrySeconds)
                return;
            _lastResolveAttemptRealtime = now;

            if (Chainloader.PluginInfos == null || !Chainloader.PluginInfos.TryGetValue(SmutGuid, out var smutInfo) || smutInfo?.Instance == null)
                return;
            try
            {
                var pluginType = smutInfo.Instance.GetType();

                var getDm = pluginType.GetMethod("GetDonationManager", BindingFlags.Public | BindingFlags.Static);
                if (getDm == null) return;

                _donationManager = getDm.Invoke(null, null);
                if (_donationManager == null) return;

                _hasDonatedByGameId = _donationManager.GetType().GetMethod("HasDonatedByGameId",
                    BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"DonationHelper: SMUT bind failed: {ex.Message}");
            }
        }

        public static bool HasDonatedByGameId(int gameItemId)
        {
            EnsureResolved();
            if (_donationManager == null || _hasDonatedByGameId == null)
                return false;
            try
            {
                return (bool)_hasDonatedByGameId.Invoke(_donationManager, new object[] { gameItemId });
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"DonationHelper: HasDonatedByGameId failed for {gameItemId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Pick an item from fishingMuseumItems, biased toward unowned ones when Trinket Fortune is enabled.
        /// Returns 0 to fall back to vanilla selection.
        /// </summary>
        public static int PickBiasedFishingMuseumItem()
        {
            if (!Config.Enabled.Value) return 0;
            var items = FishingRod.fishingMuseumItems;
            if (items == null || items.Count == 0) return 0;

            if (!IsAvailable)
                return 0; // let vanilla run

            var unowned = items.Where(id => !HasDonatedByGameId(id)).ToList();
            if (unowned.Count == 0)
                return items[UnityEngine.Random.Range(0, items.Count)];

            float aquariumPct = GetAquariumProgress();
            if (aquariumPct < Config.MinimumMuseumProgress.Value * 100f)
                return items[UnityEngine.Random.Range(0, items.Count)];

            float bonusChance = Mathf.Min(
                Config.MuseumProgressBonusPercent.Value * (aquariumPct / 10f),
                Config.MaxBonusChancePercent.Value);
            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll < bonusChance)
                return unowned[UnityEngine.Random.Range(0, unowned.Count)];

            return items[UnityEngine.Random.Range(0, items.Count)];
        }

        private static float GetAquariumProgress()
        {
            try
            {
                int total = 0;
                int donated = 0;
                var gameSaveType = Type.GetType("Wish.GameSave, SunHaven.Core");
                if (gameSaveType == null) return 0f;
                var instanceProp = gameSaveType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp == null) return 0f;
                var gs = instanceProp.GetValue(null);
                if (gs == null) return 0f;
                var getProgress = gameSaveType.GetMethod("GetProgressIntWorld", new[] { typeof(string) });
                if (getProgress == null) return 0f;

                foreach (var tuple in MuseumCurator.aquaticMuseumProgress)
                {
                    var key = tuple.Item1 + "complete";
                    int required = tuple.Item2;
                    int complete = (int)getProgress.Invoke(gs, new object[] { key });
                    total += required;
                    donated += complete;
                }
                return total > 0 ? (donated / (float)total) * 100f : 0f; // 0-100
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"DonationHelper: aquarium progress fallback used due to reflection error: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// Pick a fish from the pool, biased toward unowned aquarium fish when possible.
        /// Returns null to keep vanilla result.
        /// </summary>
        public static FishData PickBiasedFish(RandomFishArray array, float level)
        {
            if (!Config.Enabled.Value || array == null || array.Length == 0) return null;
            if (!IsAvailable) return null;

            var unowned = new List<FishData>();
            for (int i = 0; i < array.Length; i++)
            {
                var loot = array.drops[i];
                if (loot?.fish == null) continue;
                int id = loot.fish.id;
                if (!HasDonatedByGameId(id))
                    unowned.Add(loot.fish);
            }
            if (unowned.Count == 0) return null;

            float aquariumPct = GetAquariumProgress();
            if (aquariumPct < Config.MinimumMuseumProgress.Value * 100f) return null;

            float bonusChance = Mathf.Min(
                Config.MuseumProgressBonusPercent.Value * (aquariumPct / 10f),
                Config.MaxBonusChancePercent.Value);
            if (UnityEngine.Random.Range(0f, 100f) >= bonusChance) return null;

            return unowned[UnityEngine.Random.Range(0, unowned.Count)];
        }
    }
}
