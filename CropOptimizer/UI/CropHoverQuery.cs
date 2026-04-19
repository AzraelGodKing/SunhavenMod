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

        private static Camera _cachedGameplayCamera;
        private static float _nextGameplayCameraSearchTime;

        private static Vector3 _lastHoverMouseScreen;
        private static Component _lastHoverCrop;
        private static float _nextHoverFullScanTime;

        /// <summary>How often we refresh the crop list from the scene (large farms: avoid hammering <c>FindObjectsOfType</c>).</summary>
        private const float CropCacheRefreshSeconds = 0.45f;

        /// <summary>When the mouse is stable, skip the O(n) closest-crop scan for a short window.</summary>
        private const float HoverRescanMinInterval = 0.055f;

        private const float MouseMoveSkipScanPxSq = 9f;

        /// <summary>After a successful camera search, wait before scanning all cameras again (avoids per-frame <c>FindObjectsOfType&lt;Camera&gt;</c>).</summary>
        private const float GameplayCameraSearchCooldown = 2f;

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
            {
                _cachedGameplayCamera = Camera.main;
                return Camera.main;
            }

            if (_cachedGameplayCamera != null && _cachedGameplayCamera.enabled && _cachedGameplayCamera.gameObject.activeInHierarchy)
                return _cachedGameplayCamera;

            float now = Time.unscaledTime;
            if (now < _nextGameplayCameraSearchTime)
                return _cachedGameplayCamera;

            Camera[] cams = UnityEngine.Object.FindObjectsOfType<Camera>();
            Camera best = null;
            foreach (Camera c in cams)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy)
                    continue;
                if (best == null || c.depth > best.depth)
                    best = c;
            }

            _cachedGameplayCamera = best;
            // Retry sooner when no camera yet (e.g. load-in); back off when we have one.
            _nextGameplayCameraSearchTime = now + (best != null ? GameplayCameraSearchCooldown : 0.25f);
            return best;
        }

        /// <summary>Call on scene/game transitions so we do not keep a stale camera reference.</summary>
        public static void InvalidateGameplayCameraCache()
        {
            _cachedGameplayCamera = null;
            _nextGameplayCameraSearchTime = 0f;
        }

        /// <summary>Clears hover fast-path state (e.g. after load) so we never reference destroyed crops.</summary>
        public static void InvalidateHoverAssist()
        {
            _lastHoverCrop = null;
            _nextHoverFullScanTime = 0f;
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

            Vector3 mouseScreen = Input.mousePosition;
            float now = Time.unscaledTime;
            float maxSq = maxWorldDistance * maxWorldDistance;
            Vector2 mouse2 = new Vector2(worldOnPlane.x, worldOnPlane.y);

            bool mouseStable = (mouseScreen - _lastHoverMouseScreen).sqrMagnitude <= MouseMoveSkipScanPxSq;
            _lastHoverMouseScreen = mouseScreen;

            // Fast path: between full scans, if the pointer barely moved and the last crop is still closest in range, skip O(n).
            if (mouseStable && now < _nextHoverFullScanTime && _lastHoverCrop != null)
            {
                Component last = _lastHoverCrop;
                if (last != null)
                {
                    Vector2 p = new Vector2(last.transform.position.x, last.transform.position.y);
                    if ((p - mouse2).sqrMagnitude < maxSq)
                    {
                        crop = last;
                        return true;
                    }
                }
            }

            _nextHoverFullScanTime = now + HoverRescanMinInterval;

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
            {
                _lastHoverCrop = null;
                return false;
            }
            crop = best;
            _lastHoverCrop = best;
            return true;
        }

        private static void RefreshCropCache()
        {
            float now = Time.unscaledTime;
            if (_cachedCrops != null && now < _nextCacheTime)
                return;
            _nextCacheTime = now + CropCacheRefreshSeconds;
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

        /// <summary>Builds a structured <see cref="TooltipContent"/> for the uGUI hover card.</summary>
        public static TooltipContent BuildTooltipContent(Component crop, CropForecast forecast)
        {
            if (crop == null) return null;

            object inst = crop;
            DumpCropMembersOnce(inst);

            var content = new TooltipContent();

            int itemId = 0;
            string cropTitle = "Crop";
            if (CropGrowthPatch.TryGetTooltipHarvestItemId(inst, out itemId) && itemId > 0)
            {
                if (CropGrowthPatch.TryGetItemDisplayName(itemId, out string name) && !string.IsNullOrWhiteSpace(name))
                    cropTitle = name;
                else
                    cropTitle = $"Item #{itemId}";
            }
            content.Title = cropTitle;

            bool fullyGrown = false;
            CropGrowthPatch.TryGetTooltipFullyGrown(inst, out fullyGrown);
            if (fullyGrown) content.HeaderTag = "ready to harvest";

            // Rich-text accent colors are tuned to read on the dark panel fill:
            //   #F7D982 = warm gold (numbers / emphasis), #B8A078 = muted cream (secondary notes).
            const string accentGold = "#F7D982";
            const string mutedCream = "#B8A078";

            if (CropGrowthPatch.TryGetTooltipQualityInfo(inst, out string qualityLabel, out float qMul) && !string.IsNullOrEmpty(qualityLabel))
            {
                content.QualityColor = QualityColorFor(qualityLabel);
                content.Rows.Add(RowSpec.Make(UiStyle.IconKind.Quality, content.QualityColor,
                    $"Quality: <b>{qualityLabel}</b> <color={mutedCream}>(×{qMul:0.##})</color>"));
            }

            if (CropGrowthPatch.TryGetTooltipGrowthStageInfo(inst, out string stageText, out _) && !string.IsNullOrEmpty(stageText))
                content.Rows.Add(RowSpec.Make(UiStyle.IconKind.Sprout, UiStyle.Sprout, $"Growth: <b>{stageText}</b>"));

            if (fullyGrown)
                content.Rows.Add(RowSpec.Make(UiStyle.IconKind.Ready, UiStyle.Fertilizer, "<b><color=" + accentGold + ">Ready now</color></b>"));
            else if (CropGrowthPatch.TryGetTooltipEtaHours(inst, out float liveEta, out bool reflOk) && reflOk)
                content.Rows.Add(RowSpec.Make(UiStyle.IconKind.Clock, UiStyle.Clock, $"Ready in <b><color={accentGold}>~{Mathf.Max(0f, liveEta):0.#} h</color></b>"));
            else if (forecast != null && forecast.TryGetState(crop.GetInstanceID(), out CropForecast.CropState st))
                content.Rows.Add(RowSpec.Make(UiStyle.IconKind.Clock, UiStyle.Clock, $"Ready in <b><color={accentGold}>~{Mathf.Max(0f, st.NextHarvestEtaHours):0.#} h</color></b> <color={mutedCream}>(cached)</color>"));
            else
                content.Rows.Add(RowSpec.Make(UiStyle.IconKind.Clock, UiStyle.Clock, $"<color={mutedCream}>ETA unknown — grow once to calibrate</color>"));

            if (CropGrowthPatch.TryGetTooltipProjectedGold(inst, out int gold, out _) && gold > 0)
                content.Rows.Add(RowSpec.Make(UiStyle.IconKind.Coin, UiStyle.Coin, $"<b><color={accentGold}>~{gold:N0}g</color></b> at shop"));

            // Water (from the water tilemap; fallback to "?" if nothing resolves).
            Vector2Int tile = default;
            bool haveTile = CropTileReflection.TryGetTileCoordForCrop(crop, out tile);
            string tileState = null;
            if (haveTile)
            {
                tileState = CropTileReflection.DescribeFarmingTileState(tile, crop.transform.position, true, out _);
                if (IsDebugLogEnabled()) LogTileDebugOnce(crop, tile);
            }

            (string waterText, Color32 waterColor) = DescribeWaterState(tileState);
            content.Rows.Add(RowSpec.Make(UiStyle.IconKind.Water, waterColor, waterText));

            if (CropGrowthPatch.TryGetTooltipFertilized(inst, out bool fertilized))
            {
                string label = fertilized ? "<b>Fertilized</b>" : "Not fertilized";
                content.Rows.Add(RowSpec.Make(UiStyle.IconKind.Fertilizer,
                    fertilized ? UiStyle.Fertilizer : (Color32)new Color32(0x9A, 0x88, 0x60, 0xFF), label));
            }

            if (CropGrowthPatch.TryGetTooltipManaInfused(inst, out bool manaInfused) && manaInfused)
                content.Rows.Add(RowSpec.Make(UiStyle.IconKind.Mana, UiStyle.Mana, "<b>Mana infused</b>"));

            if (haveTile)
                content.Rows.Add(RowSpec.Make(UiStyle.IconKind.Tile, UiStyle.Tile,
                    $"<color={mutedCream}>Tile ({tile.x}, {tile.y})</color>"));

            if (itemId > 0)
            {
                var extras = new System.Collections.Generic.List<string>();
                CropGrowthPatch.AppendItemExtraLines(itemId, extras);
                if (extras.Count > 0)
                    content.Extras = string.Join(" · ", extras);
            }

            return content;
        }

        private static Color32 QualityColorFor(string label)
        {
            if (string.IsNullOrEmpty(label)) return UiStyle.QualityNormal;
            string l = label.ToLowerInvariant();
            if (l.Contains("gold") || l.Contains("iridium")) return UiStyle.QualityGold;
            if (l.Contains("silver")) return UiStyle.QualitySilver;
            return UiStyle.QualityNormal;
        }

        private static (string text, Color32 color) DescribeWaterState(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return ("Water: <color=#B8A078>unknown</color>", UiStyle.Water);
            string r = raw.ToLowerInvariant();
            if (r.Contains("water"))
                return ("<b><color=#8AD4FF>Watered</color></b>", UiStyle.Water);
            if (r.Contains("hoed"))
                return ("Hoed <color=#B8A078>(dry — needs water)</color>", new Color32(0xC9, 0xA0, 0x70, 0xFF));
            return ($"Water: {raw}", UiStyle.Water);
        }
    }
}
