using System;
using System.Reflection;
using CropOptimizer.Data;
using CropOptimizer.Patches;
using HarmonyLib;
using UnityEngine;

namespace CropOptimizer.UI
{
    /// <summary>
    /// Experimental: find <see cref="Wish.Crop"/> near the mouse in world space for hover tooltips.
    /// </summary>
    internal static class CropHoverQuery
    {
        private static Type _cropType;
        private static UnityEngine.Object[] _cachedCrops;
        private static float _nextCacheTime;

        private static readonly string[] WaterMemberNames =
        {
            "isWatered", "IsWatered", "watered", "Watered", "needsWater", "NeedsWater",
            "water", "Water", "hasWater", "HasWater"
        };

        private static readonly string[] FertilizerMemberNames =
        {
            "fertilizer", "Fertilizer", "fertilized", "Fertilized", "hasFertilizer", "HasFertilizer",
            "fertilizerType", "FertilizerType", "soilFertility", "SoilFertility"
        };

        private static Type CropType => _cropType ??= AccessTools.TypeByName("Wish.Crop");

        /// <summary>
        /// Prefer <see cref="Camera.main"/>; many scenes never tag the farm camera as MainCamera.
        /// </summary>
        public static Camera ResolveGameplayCamera()
        {
            if (Camera.main != null && Camera.main.enabled)
                return Camera.main;

            Camera[] cams = UnityEngine.Object.FindObjectsOfType<Camera>();
            Camera best = null;
            foreach (Camera c in cams)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy)
                    continue;
                if (best == null || c.depth > best.depth)
                    best = c;
            }

            return best;
        }

        /// <summary>
        /// Maps mouse to the XY plane at <paramref name="planeZ"/> (crop layer). Raw
        /// <see cref="Camera.ScreenToWorldPoint"/> with <c>mouse.z == 0</c> is wrong for orthographic cameras.
        /// </summary>
        public static bool TryMouseWorldOnPlane(Camera camera, float planeZ, out Vector3 world)
        {
            world = default;
            if (camera == null)
                return false;

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
            if (plane.Raycast(ray, out float enter))
            {
                world = ray.GetPoint(enter);
                return true;
            }

            // Fallback: orthographic depth hack when ray is parallel to the farm plane
            Vector3 mp = Input.mousePosition;
            mp.z = Mathf.Max(0.01f, Mathf.Abs(camera.transform.position.z));
            world = camera.ScreenToWorldPoint(mp);
            world.z = planeZ;
            return true;
        }

        public static bool TryGetClosestCropNearMouse(Camera camera, float maxWorldDistance, out Component crop)
        {
            crop = null;
            Type ct = CropType;
            if (ct == null || camera == null)
                return false;

            RefreshCropCache();
            if (_cachedCrops == null || _cachedCrops.Length == 0)
                return false;

            float planeZ = 0f;
            foreach (UnityEngine.Object o in _cachedCrops)
            {
                if (o is Component mb && mb != null)
                {
                    planeZ = mb.transform.position.z;
                    break;
                }
            }

            if (!TryMouseWorldOnPlane(camera, planeZ, out Vector3 worldOnPlane))
                return false;

            Vector2 mouse2 = new Vector2(worldOnPlane.x, worldOnPlane.y);
            float maxSq = maxWorldDistance * maxWorldDistance;
            float bestSq = maxSq;
            Component best = null;

            foreach (UnityEngine.Object o in _cachedCrops)
            {
                if (o is not Component mb || mb == null)
                    continue;
                Vector2 p = new Vector2(mb.transform.position.x, mb.transform.position.y);
                float sq = (p - mouse2).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = mb;
                }
            }

            if (best == null)
                return false;
            crop = best;
            return true;
        }

        private static void RefreshCropCache()
        {
            float now = Time.unscaledTime;
            if (_cachedCrops != null && now < _nextCacheTime)
                return;
            _nextCacheTime = now + 0.2f;
            Type ct = CropType;
            if (ct == null)
            {
                _cachedCrops = Array.Empty<UnityEngine.Object>();
                return;
            }

            _cachedCrops = UnityEngine.Object.FindObjectsOfType(ct);
        }

        public static string FormatWaterGuess(object cropInstance)
        {
            return FormatMemberGuess(cropInstance, WaterMemberNames);
        }

        public static string FormatFertilizerGuess(object cropInstance)
        {
            return FormatMemberGuess(cropInstance, FertilizerMemberNames);
        }

        private static bool _dumpedCropMembers;
        private static bool _loggedTileProbe;

        private static void LogTileDebugOnce(Component crop, Vector2Int tile)
        {
            if (_loggedTileProbe) return;
            _loggedTileProbe = true;
            try { Plugin.Log?.LogInfo(CropTileReflection.BuildDebugSnapshot(crop, tile)); } catch { }
        }

        /// <summary>When <c>Debug.DebugLogging</c> is on, log <see cref="Wish.Crop"/>'s public+private
        /// fields/props once so we can hardcode the right names for water / growth / quality.</summary>
        private static void DumpCropMembersOnce(object cropInstance)
        {
            if (_dumpedCropMembers || cropInstance == null)
                return;
            _dumpedCropMembers = true;

            try
            {
                var cfg = Plugin.Instance?.Config as BepInEx.Configuration.ConfigFile;
                bool debug = Plugin.Instance != null && IsDebugLogEnabled();
                if (!debug)
                    return;

                var log = Plugin.Log;
                if (log == null)
                    return;

                Type t = cropInstance.GetType();
                log.LogInfo($"[HoverDebug] Dumping members of {t.FullName}");
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
                foreach (var fi in t.GetFields(flags))
                    log.LogInfo($"[HoverDebug]   field  {fi.FieldType.Name} {fi.Name}");
                foreach (var pi in t.GetProperties(flags))
                    log.LogInfo($"[HoverDebug]   prop   {pi.PropertyType.Name} {pi.Name}");
            }
            catch
            {
            }
        }

        private static bool IsDebugLogEnabled()
        {
            try
            {
                return Plugin.Instance != null && Plugin.IsDebugLoggingEnabled;
            }
            catch
            {
                return false;
            }
        }

        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        private static string FormatMemberGuess(object instance, string[] names)
        {
            if (instance == null)
                return "?";

            try
            {
                for (Type t = instance.GetType(); t != null; t = t.BaseType)
                {
                    foreach (string name in names)
                    {
                        FieldInfo fi = null;
                        PropertyInfo pi = null;
                        try { fi = t.GetField(name, MemberFlags); } catch { }
                        try { pi = fi == null ? t.GetProperty(name, MemberFlags) : null; } catch { }
                        if (fi == null && pi == null)
                            continue;
                        object raw = fi != null ? fi.GetValue(instance) : pi.GetValue(instance, null);
                        if (raw == null)
                            continue;
                        return FormatPrimitiveGuess(raw);
                    }
                }
            }
            catch
            {
            }

            return "?";
        }

        private static string FormatPrimitiveGuess(object raw)
        {
            if (raw is bool b)
                return b ? "yes" : "no";
            if (raw is int i)
                return i != 0 ? $"yes ({i})" : "no";
            if (raw is float f)
                return Math.Abs(f) > 0.0001f ? $"yes ({f:0.##})" : "no";
            if (raw is double d)
                return Math.Abs(d) > 0.0001 ? $"yes ({d:0.##})" : "no";

            string s = raw.ToString();
            if (string.IsNullOrWhiteSpace(s))
                return "?";
            if (s.Length > 48)
                return s.Substring(0, 45) + "...";
            return s;
        }

        public static string BuildTooltipLines(Component crop, CropForecast forecast)
        {
            if (crop == null)
                return string.Empty;

            object inst = crop;
            DumpCropMembersOnce(inst);
            var lines = new System.Collections.Generic.List<string>();

            int itemId = 0;
            string cropTitle = "Crop";
            if (CropGrowthPatch.TryGetTooltipHarvestItemId(inst, out itemId) && itemId > 0)
            {
                if (CropGrowthPatch.TryGetItemDisplayName(itemId, out string name) && !string.IsNullOrWhiteSpace(name))
                    cropTitle = name;
                else
                    cropTitle = $"Item #{itemId}";
            }

            bool fullyGrown = false;
            CropGrowthPatch.TryGetTooltipFullyGrown(inst, out fullyGrown);
            lines.Add(fullyGrown ? $"{cropTitle} (ready to harvest)" : cropTitle);

            if (CropGrowthPatch.TryGetTooltipQualityInfo(inst, out string qualityLabel, out float qMul) && !string.IsNullOrEmpty(qualityLabel))
                lines.Add($"Quality: {qualityLabel} (×{qMul:0.##})");

            if (CropGrowthPatch.TryGetTooltipGrowthStageInfo(inst, out string stageText, out _) && !string.IsNullOrEmpty(stageText))
                lines.Add($"Growth: {stageText}");

            if (fullyGrown)
                lines.Add("Ready now");
            else if (CropGrowthPatch.TryGetTooltipEtaHours(inst, out float liveEta, out bool reflOk) && reflOk)
                lines.Add($"Ready in ~{Mathf.Max(0f, liveEta):0.#} h");
            else if (forecast != null && forecast.TryGetState(crop.GetInstanceID(), out CropForecast.CropState st))
                lines.Add($"Ready in ~{Mathf.Max(0f, st.NextHarvestEtaHours):0.#} h (cached)");
            else
                lines.Add("ETA: unknown (grow once to calibrate)");

            if (CropGrowthPatch.TryGetTooltipProjectedGold(inst, out int gold, out _) && gold > 0)
                lines.Add($"~{gold}g at shop");

            // Fertilizer / mana come directly from Wish.Crop properties.
            if (CropGrowthPatch.TryGetTooltipFertilized(inst, out bool fertilized))
                lines.Add($"Fertilized: {(fertilized ? "yes" : "no")}");
            if (CropGrowthPatch.TryGetTooltipManaInfused(inst, out bool manaInfused) && manaInfused)
                lines.Add("Mana infused");

            // Tile state (watered/hoed) lives on TileManager. The water tilemap is queried with the
            // crop's world position directly; farmingData / IsHoed use the nearest tile key.
            string tileState = null;
            Vector2Int tile = default;
            Vector2Int matchedTile = default;
            bool haveTile = CropTileReflection.TryGetTileCoordForCrop(crop, out tile);
            if (haveTile)
            {
                tileState = CropTileReflection.DescribeFarmingTileState(tile, crop.transform.position, true, out matchedTile);
                string waterLine = string.IsNullOrEmpty(tileState)
                    ? "Water: ?"
                    : $"Water: {tileState}" + (matchedTile != tile ? $" (neighbor {matchedTile.x},{matchedTile.y})" : string.Empty);
                lines.Add(waterLine);
                lines.Add($"Tile: ({tile.x}, {tile.y})");

                if (IsDebugLogEnabled())
                    LogTileDebugOnce(crop, tile);
            }
            else
            {
                // Last-ditch guess off the crop object itself (rare).
                lines.Add($"Water (fallback guess): {FormatWaterGuess(inst)}");
            }

            if (itemId > 0)
                CropGrowthPatch.AppendItemExtraLines(itemId, lines);

            return string.Join("\n", lines);
        }
    }
}
