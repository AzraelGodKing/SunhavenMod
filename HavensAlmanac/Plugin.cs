using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HavensAlmanac.Config;
using HavensAlmanac.Data;
using HavensAlmanac.UI;
using SunhavenMods.Shared;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace HavensAlmanac
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.azraelgodking.sunhaventodo", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.squirrelsbirthdayreminder", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.sunhavenmuseumutilitytracker", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.senpaischest", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.thevault", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.havensbirthright", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.havendevtools", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        // Static references
        private static AlmanacDataAggregator _staticAggregator;
        private static AlmanacHUD _staticHUD;
        private static AlmanacDashboard _staticDashboard;
        private static DailyBriefing _staticBriefing;

        // Persistent runner
        private static GameObject _persistentRunner;
        private static AlmanacPersistentRunner _persistentRunnerComponent;

        // Overnight hook tracking
        private static bool _overnightHooked;
        private static UnityAction _overnightCallback;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");

            AlmanacConfig.Initialize(Config);
            CreatePersistentRunner();

            _staticAggregator = new AlmanacDataAggregator();
            InitializeIntegrations();
            CreateUIComponents();
            ApplyPatches();

            SceneManager.sceneLoaded += OnSceneLoaded;

            if (AlmanacConfig.CheckForUpdates.Value)
            {
                VersionChecker.CheckForUpdate(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_VERSION, Log,
                    result => result.NotifyUpdateAvailable(Log));
            }

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded with {_staticAggregator.InstalledModCount} mod(s) detected");

            if (_staticAggregator.InstalledModCount == 0)
                Log.LogWarning("No supported mods detected! Haven's Almanac requires at least one other mod to be useful.");
        }

        private void CreatePersistentRunner()
        {
            if (_persistentRunner != null && _persistentRunnerComponent != null) return;

            _persistentRunner = new GameObject("HavensAlmanac_PersistentRunner");
            DontDestroyOnLoad(_persistentRunner);
            _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
            _persistentRunnerComponent = _persistentRunner.AddComponent<AlmanacPersistentRunner>();
            Log.LogInfo("[PersistentRunner] Created");
        }

        private void InitializeIntegrations()
        {
            var pluginInfos = BepInEx.Bootstrap.Chainloader.PluginInfos;

            TryRegisterProvider(pluginInfos, "com.azraelgodking.sunhaventodo",
                () => new Integration.TodoDataProvider(), "SunhavenTodo");

            TryRegisterProvider(pluginInfos, "com.azraelgodking.squirrelsbirthdayreminder",
                () => new Integration.BirthdayDataProvider(), "BirthdayReminder");

            TryRegisterProvider(pluginInfos, "com.azraelgodking.sunhavenmuseumutilitytracker",
                () => new Integration.MuseumDataProvider(), "S.M.U.T.");

            TryRegisterProvider(pluginInfos, "com.azraelgodking.senpaischest",
                () => new Integration.ChestDataProvider(), "SenpaisChest");

            TryRegisterProvider(pluginInfos, "com.azraelgodking.thevault",
                () => new Integration.VaultDataProvider(), "TheVault");

            TryRegisterProvider(pluginInfos, "com.azraelgodking.havensbirthright",
                () => new Integration.BirthrightDataProvider(), "HavensBirthright");

            TryRegisterProvider(pluginInfos, "com.azraelgodking.havendevtools",
                () => new Integration.DevToolsDataProvider(), "HavenDevTools");
        }

        private void TryRegisterProvider(
            System.Collections.Generic.Dictionary<string, BepInEx.PluginInfo> pluginInfos,
            string guid, Func<IModDataProvider> factory, string displayName)
        {
            try
            {
                if (pluginInfos.ContainsKey(guid))
                {
                    _staticAggregator.RegisterProvider(factory());
                    Log.LogInfo($"[Integration] {displayName} detected and registered");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[Integration] Failed to load {displayName} provider: {ex.Message}");
            }
        }

        private void CreateUIComponents()
        {
            try
            {
                // HUD
                var hudObj = new GameObject("HavensAlmanac_HUD");
                DontDestroyOnLoad(hudObj);
                _staticHUD = hudObj.AddComponent<AlmanacHUD>();
                _staticHUD.Initialize(_staticAggregator);

                // Wire position persistence
                _staticHUD.OnPositionChanged = (x, y) =>
                {
                    AlmanacConfig.StaticHUDPositionX = x;
                    AlmanacConfig.StaticHUDPositionY = y;
                    AlmanacConfig.HUDPositionX?.SetSerializedValue(x.ToString());
                    AlmanacConfig.HUDPositionY?.SetSerializedValue(y.ToString());
                };

                // Dashboard
                var dashObj = new GameObject("HavensAlmanac_Dashboard");
                DontDestroyOnLoad(dashObj);
                _staticDashboard = dashObj.AddComponent<AlmanacDashboard>();
                _staticDashboard.Initialize(_staticAggregator);

                // Daily Briefing
                var briefObj = new GameObject("HavensAlmanac_Briefing");
                DontDestroyOnLoad(briefObj);
                _staticBriefing = briefObj.AddComponent<DailyBriefing>();
                _staticBriefing.Initialize(_staticAggregator);
            }
            catch (Exception ex)
            {
                Log.LogError($"[UI] Error creating UI components: {ex}");
            }
        }

        public static void EnsureUIComponentsExist()
        {
            try
            {
                if (_persistentRunner == null || _persistentRunnerComponent == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating PersistentRunner...");
                    _persistentRunner = new GameObject("HavensAlmanac_PersistentRunner");
                    UnityEngine.Object.DontDestroyOnLoad(_persistentRunner);
                    _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
                    _persistentRunnerComponent = _persistentRunner.AddComponent<AlmanacPersistentRunner>();
                }

                if (_staticHUD == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating HUD...");
                    var hudObj = new GameObject("HavensAlmanac_HUD");
                    UnityEngine.Object.DontDestroyOnLoad(hudObj);
                    _staticHUD = hudObj.AddComponent<AlmanacHUD>();
                    _staticHUD.Initialize(_staticAggregator);
                }

                if (_staticDashboard == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating Dashboard...");
                    var dashObj = new GameObject("HavensAlmanac_Dashboard");
                    UnityEngine.Object.DontDestroyOnLoad(dashObj);
                    _staticDashboard = dashObj.AddComponent<AlmanacDashboard>();
                    _staticDashboard.Initialize(_staticAggregator);
                }

                if (_staticBriefing == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating Briefing...");
                    var briefObj = new GameObject("HavensAlmanac_Briefing");
                    UnityEngine.Object.DontDestroyOnLoad(briefObj);
                    _staticBriefing = briefObj.AddComponent<DailyBriefing>();
                    _staticBriefing.Initialize(_staticAggregator);
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnsureUI] Error: {ex.Message}");
            }
        }

        #region Harmony Patches

        private void ApplyPatches()
        {
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);

            try
            {
                var playerType = AccessTools.TypeByName("Wish.Player");
                if (playerType != null)
                {
                    var initMethod = AccessTools.Method(playerType, "InitializeAsOwner");
                    if (initMethod != null)
                    {
                        var patchMethod = AccessTools.Method(typeof(Plugin), nameof(OnPlayerInitialized));
                        _harmony.Patch(initMethod, postfix: new HarmonyMethod(patchMethod));
                        Log.LogInfo("Applied player initialization patch");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to apply patches: {ex.Message}");
            }
        }

        private static void OnPlayerInitialized(object __instance)
        {
            try
            {
                Log?.LogInfo("[Almanac] Player initialized");

                EnsureUIComponentsExist();

                // Refresh all data
                _staticAggregator?.RefreshAll();

                // Reset and re-hook overnight event
                ResetOvernightHook();
                TryHookOvernightEvent();

                // Show daily briefing
                _staticBriefing?.ShowBriefing();
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error in OnPlayerInitialized: {ex.Message}");
            }
        }

        #endregion

        #region Overnight Hook

        public static void ResetOvernightHook()
        {
            _overnightHooked = false;
            _overnightCallback = null;
        }

        public static void TryHookOvernightEvent()
        {
            if (_overnightHooked) return;

            try
            {
                // Try DayCycle.OnDayStart first (most reliable)
                var dayCycleType = AccessTools.TypeByName("Wish.DayCycle");
                if (dayCycleType != null)
                {
                    var onDayStartField = AccessTools.Field(dayCycleType, "OnDayStart");
                    if (onDayStartField != null)
                    {
                        var currentAction = onDayStartField.GetValue(null) as UnityAction;
                        _overnightCallback = OnOvernightComplete;

                        if (currentAction != null)
                        {
                            currentAction += _overnightCallback;
                            onDayStartField.SetValue(null, currentAction);
                        }
                        else
                        {
                            onDayStartField.SetValue(null, _overnightCallback);
                        }

                        _overnightHooked = true;
                        Log?.LogInfo("Hooked into DayCycle.OnDayStart");
                        return;
                    }
                }

                // Fallback: UIHandler.OnCompleteOvernight
                var uiHandlerType = AccessTools.TypeByName("Wish.UIHandler");
                if (uiHandlerType == null) return;

                var singletonBaseType = AccessTools.TypeByName("Wish.SingletonBehaviour`1");
                if (singletonBaseType == null) return;

                var genericType = singletonBaseType.MakeGenericType(uiHandlerType);
                var instanceProp = AccessTools.Property(genericType, "Instance");
                var uiHandler = instanceProp?.GetValue(null);
                if (uiHandler == null) return;

                var overnightField = AccessTools.Field(uiHandlerType, "OnCompleteOvernight");
                if (overnightField != null)
                {
                    var currentAction = overnightField.GetValue(uiHandler) as UnityAction;
                    _overnightCallback = OnOvernightComplete;

                    if (currentAction != null)
                    {
                        currentAction += _overnightCallback;
                        overnightField.SetValue(uiHandler, currentAction);
                    }
                    else
                    {
                        overnightField.SetValue(uiHandler, _overnightCallback);
                    }

                    _overnightHooked = true;
                    Log?.LogInfo("Hooked into UIHandler.OnCompleteOvernight");
                }
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Failed to hook overnight event: {ex.Message}");
            }
        }

        private static void OnOvernightComplete()
        {
            Log?.LogInfo("[Almanac] Day started - refreshing data and showing briefing");

            _staticAggregator?.RefreshAll();
            _staticBriefing?.ShowBriefing();
        }

        #endregion

        #region Scene Management

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu" || scene.name == "Bootstrap")
            {
                Log.LogInfo("[Almanac] Main menu detected - hiding UI");
                _staticHUD?.Hide();
                _staticDashboard?.Hide();
                _staticBriefing?.Hide();
                return;
            }

            EnsureUIComponentsExist();
            TryHookOvernightEvent();
        }

        #endregion

        #region Static Accessors

        internal static AlmanacDataAggregator GetDataAggregator() => _staticAggregator;
        internal static AlmanacHUD GetAlmanacHUD() => _staticHUD;
        internal static AlmanacDashboard GetAlmanacDashboard() => _staticDashboard;
        internal static DailyBriefing GetDailyBriefing() => _staticBriefing;

        #endregion

        private void OnDestroy()
        {
            Log?.LogInfo("Plugin OnDestroy called - static references preserved");
        }
    }

    /// <summary>
    /// Hidden MonoBehaviour that survives game cleanup. Handles hotkey detection.
    /// </summary>
    public class AlmanacPersistentRunner : MonoBehaviour
    {
        private void Update()
        {
            DetectHotkeys();
        }

        private void DetectHotkeys()
        {
            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            // Dashboard toggle: Ctrl+F5 (or just F5 if ctrl not required)
            if (Input.GetKeyDown(AlmanacConfig.StaticDashboardToggleKey))
            {
                if (!AlmanacConfig.StaticDashboardRequireCtrl || ctrlPressed)
                {
                    Plugin.EnsureUIComponentsExist();
                    Plugin.GetAlmanacDashboard()?.Toggle();
                }
            }

            // HUD toggle: F4
            if (Input.GetKeyDown(AlmanacConfig.StaticHUDToggleKey))
            {
                Plugin.EnsureUIComponentsExist();
                Plugin.GetAlmanacHUD()?.Toggle();
            }
        }

        private void OnDestroy()
        {
            Plugin.Log?.LogWarning("[PersistentRunner] OnDestroy called - this should NOT happen!");
        }
    }
}
