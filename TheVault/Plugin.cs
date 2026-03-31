using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using TheVault.Integration;
using TheVault.Modding;
using TheVault.Patches;
using TheVault.UI;
using TheVault.Vault;
using HarmonyLib;
using SunhavenMods.Shared;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wish;

namespace TheVault
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
        public static ConfigFile ConfigFile { get; private set; }

        // Static references that survive Plugin destruction
        // (Unity's null-conditional returns null for destroyed MonoBehaviours)
        private static VaultManager _staticVaultManager;
        private static VaultSaveSystem _staticSaveSystem;
        private static VaultUI _staticVaultUI;
        // uGUI panel disabled (legacy-only).
        // private static VaultUguiPanel _staticVaultUgui;
        private static VaultHUD _staticVaultHUD;

        // Static config values for PersistentRunner to use for hotkey detection
        internal static KeyCode StaticToggleKey = KeyCode.V;
        internal static bool StaticRequireCtrl = true;
        internal static KeyCode StaticAltToggleKey = KeyCode.F8;
        internal static KeyCode StaticHUDToggleKey = KeyCode.F7;

        private Harmony _harmony;
        private VaultManager _vaultManager;
        private VaultSaveSystem _saveSystem;
        private VaultUI _vaultUI;
        // private VaultUguiPanel _vaultUgui;
        private VaultHUD _vaultHUD;

        // Configuration
        private ConfigEntry<KeyCode> _toggleKey;
        private ConfigEntry<bool> _requireCtrlModifier;
        private ConfigEntry<KeyCode> _altToggleKey;
        private ConfigEntry<bool> _enableHUD;
        private ConfigEntry<string> _hudPosition;
        private ConfigEntry<float> _hudScale;
        private ConfigEntry<bool> _hudCompactMode;
        private ConfigEntry<string> _hudDensity;
        private ConfigEntry<KeyCode> _hudToggleKey;
        private ConfigEntry<float> _windowScale;
        private ConfigEntry<bool> _enableAutoSave;
        private ConfigEntry<float> _autoSaveInterval;
        private ConfigEntry<bool> _checkForUpdates;
        private ConfigEntry<bool> _useLegacyImguiVault;

        /// <summary>
        /// Full vault inspector (zeros, Debug tab, full HUD). Controlled by Haven Dev Tools config / UI, not TheVault.cfg.
        /// </summary>
        private static bool _debugFullVaultInspector;

        // Backup menu detection via polling (in case SceneManager.sceneLoaded stops working)
        private string _lastKnownScene = "";
        private bool _wasInMenuScene = true; // Start as true since game starts at menu
        private float _sceneCheckTimer = 0f;
        private const float SCENE_CHECK_INTERVAL = 0.5f; // Check every 0.5 seconds

        // Heartbeat for debugging - proves plugin is still running
        private float _heartbeatTimer = 0f;
        private const float HEARTBEAT_INTERVAL = 30f; // Log every 30 seconds to prove plugin is alive
        private int _heartbeatCount = 0;

        // Separate persistent object that survives game's UIHandler.UnloadGame cleanup
        private static GameObject _persistentRunner;
        private static PersistentUpdateRunner _updateRunner;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            ConfigFile = Config;

            // NOTE: DontDestroyOnLoad on this gameObject doesn't help because
            // the game's UIHandler.UnloadGame explicitly destroys UI objects.
            // We use a separate hidden persistent runner instead.

            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");
            VaultSaveCompatibility.WarnIfBreakingRelease(Log);

            // Create a hidden persistent runner that survives the game's cleanup
            CreatePersistentRunner();

            try
            {
                // Initialize configuration
                InitializeConfig();
                SubscribeConfigChanged();

                // Initialize vault system
                // Store in both instance and static fields so they survive Plugin destruction
                _vaultManager = new VaultManager();
                _saveSystem = new VaultSaveSystem(_vaultManager);
                _staticVaultManager = _vaultManager;
                _staticSaveSystem = _saveSystem;

                // Create UI GameObject (RectTransform stretched to full screen for uGUI Canvas)
                var uiObject = CreateTheVaultUiHost();
                DontDestroyOnLoad(uiObject);
                _vaultUI = uiObject.AddComponent<VaultUI>();
                _vaultUI.Initialize(_vaultManager);
                _vaultUI.SetScale(Mathf.Clamp(_windowScale.Value, 0.5f, 2.5f));
                _vaultUI.SetToggleKey(_toggleKey.Value, _requireCtrlModifier.Value);
                _vaultUI.SetAltToggleKey(_altToggleKey.Value);
                _staticVaultUI = _vaultUI;

                // Legacy-only: uGUI panel is no longer created by default.
                // Keep the old IMGUI window + HUD path (player preference).
                ApplyLegacyVaultUiMode();

                // Store config values for PersistentRunner hotkey detection
                StaticToggleKey = _toggleKey.Value;
                StaticRequireCtrl = _requireCtrlModifier.Value;
                StaticAltToggleKey = _altToggleKey.Value;
                StaticHUDToggleKey = _hudToggleKey.Value;

                // Create HUD for persistent display
                _vaultHUD = uiObject.AddComponent<VaultHUD>();
                _vaultHUD.Initialize(_vaultManager);
                _vaultHUD.SetEnabled(_enableHUD.Value);
                _vaultHUD.SetPosition(ParseHUDPosition(_hudPosition.Value));
                _vaultHUD.SetScale(Mathf.Clamp(_hudScale.Value, 0.5f, 3f));
                _vaultHUD.SetHudDensity(GetResolvedHudDensity());
                _staticVaultHUD = _vaultHUD;

                VaultModApiBridge.Instance = new VaultModApiAdapter();

                // Initialize icon cache for UI icons
                SunhavenMods.Shared.IconCache.Initialize(Log);
                RegisterIconCacheCurrencies();

                // Register item-to-currency mappings for deposit/withdraw
                RegisterItemMappings();

                // Apply Harmony patches
                _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
                ApplyPatches();

                // Patch GameSave class for character loading detection
                PatchGameSave();

                // Subscribe to scene loading as a backup trigger for vault loading
                // This is more reliable than patching game-specific methods that may not exist
                SceneManager.sceneLoaded += OnSceneLoaded;
                Log.LogInfo("Subscribed to SceneManager.sceneLoaded for vault loading");

                // Check for updates
                if (_checkForUpdates.Value)
                {
                    VersionChecker.CheckForUpdate(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_VERSION, Log,
                        result => result.NotifyUpdateAvailable(Log));
                }

                Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully!");
                Log.LogInfo($"Press {(_requireCtrlModifier.Value ? "Ctrl+" : "")}{_toggleKey.Value} or {_altToggleKey.Value} to open the vault");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to load {PluginInfo.PLUGIN_NAME}: {ex}");
            }
        }

        /// <summary>
        /// Creates a hidden GameObject that persists across scene loads AND survives
        /// the game's UIHandler.UnloadGame cleanup that destroys regular GameObjects.
        /// </summary>
        private void CreatePersistentRunner()
        {
            if (_persistentRunner != null)
            {
                Log.LogInfo("PersistentRunner already exists");
                return;
            }

            // Create a new hidden GameObject
            _persistentRunner = new GameObject("TheVault_PersistentRunner");

            // Mark it to survive scene changes
            DontDestroyOnLoad(_persistentRunner);

            // Hide it from the game's cleanup routines and hierarchy
            _persistentRunner.hideFlags = HideFlags.HideAndDontSave;

            // Add the update runner component
            _updateRunner = _persistentRunner.AddComponent<PersistentUpdateRunner>();

            Log.LogInfo("Created hidden PersistentRunner that survives game cleanup");
        }

        /// <summary>
        /// uGUI needs a <see cref="RectTransform"/> that spans the overlay; a plain Transform clips to a tiny default rect.
        /// </summary>
        private static GameObject CreateTheVaultUiHost()
        {
            var go = new GameObject("TheVault_UI", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            return go;
        }

        /// <summary>
        /// Ensures UI components exist and recreates them if they were destroyed by the game's cleanup.
        /// Called from PlayerPatches when a character loads.
        /// </summary>
        public static void EnsureUIComponentsExist()
        {
            try
            {
                // Check if PersistentRunner was destroyed and recreate it
                if (_persistentRunner == null || _updateRunner == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating PersistentRunner...");
                    _persistentRunner = new GameObject("TheVault_PersistentRunner");
                    UnityEngine.Object.DontDestroyOnLoad(_persistentRunner);
                    _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
                    _updateRunner = _persistentRunner.AddComponent<PersistentUpdateRunner>();
                    Log?.LogInfo("[EnsureUI] PersistentRunner recreated");
                }

                // Check if VaultUI was destroyed and recreate it
                if (_staticVaultUI == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating VaultUI...");
                    var uiObject = CreateTheVaultUiHost();
                    UnityEngine.Object.DontDestroyOnLoad(uiObject);
                    // NOTE: Do NOT use HideFlags.HideAndDontSave on VaultUI!
                    // That flag prevents Unity's OnGUI from being called, which breaks the UI rendering.
                    // Only PersistentRunner needs HideFlags (it only uses Update, not OnGUI).

                    _staticVaultUI = uiObject.AddComponent<VaultUI>();
                    _staticVaultUI.Initialize(_staticVaultManager);
                    float windowScale = Instance != null ? Mathf.Clamp(Instance._windowScale.Value, 0.5f, 2.5f) : 1f;
                    _staticVaultUI.SetScale(windowScale);
                    _staticVaultUI.SetToggleKey(StaticToggleKey, StaticRequireCtrl);
                    _staticVaultUI.SetAltToggleKey(StaticAltToggleKey);

                    // Legacy-only: do not recreate uGUI panel.
                    if (Instance != null)
                        Instance.ApplyLegacyVaultUiMode();

                    _staticVaultHUD = uiObject.AddComponent<VaultHUD>();
                    _staticVaultHUD.Initialize(_staticVaultManager);
                    if (Instance != null)
                    {
                        Instance._vaultUI = _staticVaultUI;
                        // uGUI disabled
                        Instance._vaultHUD = _staticVaultHUD;
                        _staticVaultHUD.SetEnabled(Instance._enableHUD.Value);
                        _staticVaultHUD.SetPosition(ParseHUDPosition(Instance._hudPosition.Value));
                        _staticVaultHUD.SetScale(Mathf.Clamp(Instance._hudScale.Value, 0.5f, 3f));
                        _staticVaultHUD.SetHudDensity(GetResolvedHudDensity());
                    }

                    Log?.LogInfo("[EnsureUI] VaultUI and VaultHUD recreated");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnsureUI] Error recreating UI: {ex.Message}");
            }
        }

        private void InitializeConfig()
        {
            _toggleKey = Config.Bind(
                "UI",
                "ToggleKey",
                KeyCode.V,
                "Key to toggle the vault UI"
            );

            _requireCtrlModifier = Config.Bind(
                "UI",
                "RequireCtrlModifier",
                true,
                "Require Ctrl key to be held when pressing toggle key"
            );

            _altToggleKey = Config.Bind(
                "UI",
                "AltToggleKey",
                KeyCode.F8,
                "Alternative key to toggle vault UI (no modifier required). Useful for Steam Deck."
            );

            _useLegacyImguiVault = Config.Bind(
                "UI",
                "UseLegacyImguiVault",
                true,
                "Legacy-only build: keep using the classic OnGUI/IMGUI vault window. (uGUI panel has been disabled.)"
            );

            _enableHUD = Config.Bind(
                "HUD",
                "EnableHUD",
                true,
                "Show a persistent HUD bar displaying vault currency totals"
            );

            _hudPosition = Config.Bind(
                "HUD",
                "Position",
                "TopLeft",
                "HUD position: TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight"
            );

            _hudScale = Config.Bind(
                "HUD",
                "Scale",
                1.25f,
                "Scale factor for the HUD bar (1.0 = smaller, 1.25 = new default, 2.0 = very large). Use a dot as decimal separator in the config file (e.g. 1.25)."
            );

            _hudCompactMode = Config.Bind(
                "HUD",
                "CompactMode",
                false,
                "Legacy: when Density is Normal and this is true, HUD uses Compact spacing. Prefer [HUD] Density."
            );

            _hudDensity = Config.Bind(
                "HUD",
                "Density",
                "Normal",
                new ConfigDescription(
                    "HUD bar density: Normal, Compact, or Minimal.",
                    new AcceptableValueList<string>(new[] { "Normal", "Compact", "Minimal" }))
            );

            _windowScale = Config.Bind(
                "Display",
                "WindowScale",
                1.0f,
                new BepInEx.Configuration.ConfigDescription(
                    "Scale factor for the main Vault window (1.0 = default, 1.5 = 50% larger)",
                    new BepInEx.Configuration.AcceptableValueRange<float>(0.5f, 2.5f)
                )
            );

            _hudToggleKey = Config.Bind(
                "HUD",
                "ToggleKey",
                KeyCode.F7,
                "Key to toggle the HUD display on/off"
            );

            _enableAutoSave = Config.Bind(
                "Saving",
                "EnableAutoSave",
                true,
                "Automatically save vault data periodically"
            );

            _autoSaveInterval = Config.Bind(
                "Saving",
                "AutoSaveInterval",
                300f,
                "Auto-save interval in seconds (default: 5 minutes)"
            );

            _checkForUpdates = Config.Bind(
                "Updates",
                "CheckForUpdates",
                true,
                "Check for mod updates on startup"
            );

        }

        /// <summary>
        /// Subscribe to config changes so that when the user edits config in-game (e.g. ConfigurationManager or our Settings panel),
        /// we immediately update static state and UI.
        /// </summary>
        private void SubscribeConfigChanged()
        {
            void OnConfigChanged(object s, EventArgs e) => ApplyConfigToState();
            _toggleKey.SettingChanged += OnConfigChanged;
            _requireCtrlModifier.SettingChanged += OnConfigChanged;
            _altToggleKey.SettingChanged += OnConfigChanged;
            _enableHUD.SettingChanged += OnConfigChanged;
            _hudPosition.SettingChanged += OnConfigChanged;
            _hudScale.SettingChanged += OnConfigChanged;
            _hudCompactMode.SettingChanged += OnConfigChanged;
            _hudDensity.SettingChanged += OnConfigChanged;
            _hudToggleKey.SettingChanged += OnConfigChanged;
            _windowScale.SettingChanged += OnConfigChanged;
            _useLegacyImguiVault.SettingChanged += OnConfigChanged;
        }

        /// <summary>
        /// Apply current config values to static state and UI (no file reload).
        /// </summary>
        private void ApplyConfigToState()
        {
            try
            {
                StaticToggleKey = _toggleKey.Value;
                StaticRequireCtrl = _requireCtrlModifier.Value;
                StaticAltToggleKey = _altToggleKey.Value;
                StaticHUDToggleKey = _hudToggleKey.Value;

                var vaultUI = GetVaultUI();
                if (vaultUI != null)
                {
                    vaultUI.SetScale(Mathf.Clamp(_windowScale.Value, 0.5f, 2.5f));
                    vaultUI.SetToggleKey(StaticToggleKey, StaticRequireCtrl);
                    vaultUI.SetAltToggleKey(StaticAltToggleKey);
                }

                ApplyLegacyVaultUiMode();

                var vaultHUD = GetVaultHUD();
                if (vaultHUD != null)
                {
                    vaultHUD.SetEnabled(_enableHUD.Value);
                    vaultHUD.SetPosition(ParseHUDPosition(_hudPosition.Value));
                    vaultHUD.SetScale(Mathf.Clamp(_hudScale.Value, 0.5f, 3f));
                    vaultHUD.SetHudDensity(GetResolvedHudDensity());
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[The Vault] ApplyConfigToState failed: {ex.Message}");
            }
        }

        /// <summary>Sync which vault window is active (IMGUI vs uGUI).</summary>
        private void ApplyLegacyVaultUiMode()
        {
            if (_staticVaultUI == null) return;
            // Legacy-only mode: always enable the IMGUI window.
            _staticVaultUI.SetLegacyImguiEnabled(true);
            // uGUI disabled: nothing to hide.
        }

        /// <summary>Apply window scale to both vault UIs (IMGUI + uGUI).</summary>
        public void ApplyVaultWindowScaleToUi()
        {
            float s = Mathf.Clamp(_windowScale?.Value ?? 1f, 0.5f, 2.5f);
            _staticVaultUI?.SetScale(s);
        }

        /// <summary>
        /// Reload config from disk and re-apply to UI and static state.
        /// Call after the player edits the config file, or from a keybind.
        /// </summary>
        public void ReloadConfig()
        {
            try
            {
                Config.Reload();
                ApplyConfigToState();
                Log?.LogInfo("[The Vault] Config reloaded from file");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[The Vault] Config reload failed: {ex.Message}");
            }
        }

        // --- Config getters/setters for in-game Settings UI (values persist to config file) ---
        public static KeyCode GetConfigToggleKey() => Instance?._toggleKey?.Value ?? KeyCode.V;
        public static void SetConfigToggleKey(KeyCode k) { if (Instance?._toggleKey != null) Instance._toggleKey.Value = k; }
        public static bool GetConfigRequireCtrl() => Instance?._requireCtrlModifier?.Value ?? true;
        public static void SetConfigRequireCtrl(bool v) { if (Instance?._requireCtrlModifier != null) Instance._requireCtrlModifier.Value = v; }
        public static KeyCode GetConfigAltToggleKey() => Instance?._altToggleKey?.Value ?? KeyCode.F8;
        public static void SetConfigAltToggleKey(KeyCode k) { if (Instance?._altToggleKey != null) Instance._altToggleKey.Value = k; }
        public static bool GetConfigHUDEnabled() => Instance?._enableHUD?.Value ?? true;
        public static void SetConfigHUDEnabled(bool v) { if (Instance?._enableHUD != null) Instance._enableHUD.Value = v; }
        public static float GetConfigHUDScale() => Instance?._hudScale?.Value ?? 1f;
        public static void SetConfigHUDScale(float v) { if (Instance?._hudScale != null) Instance._hudScale.Value = Mathf.Clamp(v, 0.5f, 3f); }
        public static bool GetConfigHUDCompactMode() => Instance?._hudCompactMode?.Value ?? false;
        public static void SetConfigHUDCompactMode(bool v) { if (Instance?._hudCompactMode != null) Instance._hudCompactMode.Value = v; }

        public static string GetConfigHudDensity() => Instance?._hudDensity?.Value ?? "Normal";
        public static void SetConfigHudDensity(string v)
        {
            if (Instance?._hudDensity == null) return;
            if (string.IsNullOrWhiteSpace(v)) v = "Normal";
            Instance._hudDensity.Value = v;
        }

        /// <summary>
        /// Effective HUD density: <see cref="_hudDensity"/>, with legacy <see cref="_hudCompactMode"/> when density is Normal.
        /// </summary>
        public static VaultHudDensity GetResolvedHudDensity()
        {
            if (Instance == null) return VaultHudDensity.Normal;
            string raw = Instance._hudDensity?.Value?.Trim() ?? "Normal";
            if (string.Equals(raw, "Minimal", StringComparison.OrdinalIgnoreCase))
                return VaultHudDensity.Minimal;
            if (string.Equals(raw, "Compact", StringComparison.OrdinalIgnoreCase))
                return VaultHudDensity.Compact;
            if (Instance._hudCompactMode != null && Instance._hudCompactMode.Value)
                return VaultHudDensity.Compact;
            return VaultHudDensity.Normal;
        }
        public static float GetConfigWindowScale() => Instance?._windowScale?.Value ?? 1f;
        public static void SetConfigWindowScale(float v) { if (Instance?._windowScale != null) Instance._windowScale.Value = Mathf.Clamp(v, 0.5f, 2.5f); }

        public static bool GetConfigDebugFullVaultInspector() => _debugFullVaultInspector;

        /// <summary>
        /// Sets full vault inspector mode (intended for Haven Dev Tools; runtime only, no longer written to TheVault.cfg).
        /// </summary>
        public static void SetConfigDebugFullVaultInspector(bool v) => _debugFullVaultInspector = v;

        /// <summary>
        /// Register currency-to-item mappings for IconCache (used by VaultUI/VaultHUD).
        /// </summary>
        private void RegisterIconCacheCurrencies()
        {
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.SeasonalSpring, ItemIds.SpringToken);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.SeasonalSummer, ItemIds.SummerToken);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.SeasonalFall, ItemIds.FallToken);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.SeasonalWinter, ItemIds.WinterToken);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.KeyCopper, ItemIds.CopperKey);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.KeyIron, ItemIds.IronKey);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.KeyAdamant, ItemIds.AdamantKey);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.KeyMithril, ItemIds.MithrilKey);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.KeySunite, ItemIds.SuniteKey);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.KeyGlorite, ItemIds.GloriteKey);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.KeyKingsLostMine, ItemIds.KingsLostMineKey);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.SpecialCommunityToken, ItemIds.CommunityToken);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.SpecialDoubloon, ItemIds.Doubloon);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.SpecialBlackBottleCap, ItemIds.BlackBottleCap);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.SpecialRedCarnivalTicket, ItemIds.RedCarnivalTicket);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.SpecialCandyCornPieces, ItemIds.CandyCornPieces);
            SunhavenMods.Shared.IconCache.RegisterCurrency(VaultCurrencyIds.SpecialManaShard, ItemIds.ManaShard);
        }

        /// <summary>
        /// Register mappings between Sun Haven item IDs and vault currency IDs.
        /// Auto-deposit is enabled so items are automatically converted to vault currency when picked up.
        /// Item IDs are defined in ItemIds.cs for maintainability.
        /// </summary>
        private void RegisterItemMappings()
        {
            // Enable auto-deposit globally
            ItemPatches.AutoDepositEnabled = true;

            // Seasonal Tokens - auto-deposit enabled
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.SpringToken, "seasonal_Spring", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.SummerToken, "seasonal_Summer", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.WinterToken, "seasonal_Winter", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.FallToken, "seasonal_Fall", autoDeposit: true);

            // Keys - auto-deposit enabled
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.CopperKey, "key_copper", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.IronKey, "key_iron", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.AdamantKey, "key_adamant", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.MithrilKey, "key_mithril", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.SuniteKey, "key_sunite", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.GloriteKey, "key_glorite", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.KingsLostMineKey, "key_kingslostmine", autoDeposit: true);

            // Special currencies - auto-deposit enabled
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.CommunityToken, "special_communitytoken", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.Doubloon, "special_doubloon", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.BlackBottleCap, "special_blackbottlecap", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.RedCarnivalTicket, "special_redcarnivalticket", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.CandyCornPieces, "special_candycornpieces", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(ItemIds.ManaShard, "special_manashard", autoDeposit: true);

            // Eagerly build pickup hot-path cache so first pickup doesn't lag
            ItemPatches.InitializePickupCache();

            Log.LogInfo("Registered item-to-currency mappings with auto-deposit enabled");
        }

        private void ApplyPatches()
        {
            try
            {
                // Patch player initialization for loading vault data
                var playerType = typeof(Player);

                PatchMethod(playerType, "InitializeAsOwner",
                    typeof(PlayerPatches), "OnPlayerInitialized");

                // Patch shop purchase methods for vault currency checks (game uses Wish.Shop, not ShopMenu)
                PatchShopBuyItem();

                // Patch save/load for vault persistence (game uses GameSave.SaveGame / LoadGame, not SaveLoadManager)
                PatchGameSaveSaveLoad();

                // Patch return to menu for state reset (MainMenuController.HomeMenu when entering main menu)
                PatchMethod(typeof(MainMenuController), "HomeMenu",
                    typeof(SaveLoadPatches), "OnReturnToMenu");

                var titleType = AccessTools.TypeByName("Wish.TitleScreen");
                if (titleType != null)
                {
                    PatchMethod(titleType, "Start",
                        typeof(SaveLoadPatches), "OnReturnToMenu");
                }

                // Patch item pickup for auto-deposit
                PatchItemPickup(playerType);

                // Log results
                var patchedMethods = _harmony.GetPatchedMethods();
                int count = 0;
                foreach (var method in patchedMethods)
                {
                    Log.LogInfo($"Patched: {method.DeclaringType?.Name}.{method.Name}");
                    count++;
                }
                Log.LogInfo($"Total methods patched: {count}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Harmony patching failed: {ex}");
            }
        }

        private void PatchMethod(Type targetType, string methodName, Type patchType, string patchMethodName, Type[] parameters = null)
        {
            try
            {
                var original = parameters == null
                    ? AccessTools.Method(targetType, methodName)
                    : AccessTools.Method(targetType, methodName, parameters);

                if (original == null)
                {
                    Log.LogWarning($"Could not find method {targetType.Name}.{methodName}");
                    return;
                }

                var postfix = AccessTools.Method(patchType, patchMethodName);
                if (postfix == null)
                {
                    Log.LogWarning($"Could not find patch method {patchType.Name}.{patchMethodName}");
                    return;
                }

                _harmony.Patch(original, postfix: new HarmonyMethod(postfix));
                Log.LogInfo($"Successfully patched {targetType.Name}.{methodName}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to patch {targetType.Name}.{methodName}: {ex.Message}");
            }
        }

        private void PatchMethodPrefix(Type targetType, string methodName, Type patchType, string patchMethodName, Type[] parameters = null)
        {
            try
            {
                var original = parameters == null
                    ? AccessTools.Method(targetType, methodName)
                    : AccessTools.Method(targetType, methodName, parameters);

                if (original == null)
                {
                    Log.LogWarning($"Could not find method {targetType.Name}.{methodName}");
                    return;
                }

                var prefix = AccessTools.Method(patchType, patchMethodName);
                if (prefix == null)
                {
                    Log.LogWarning($"Could not find patch method {patchType.Name}.{patchMethodName}");
                    return;
                }

                _harmony.Patch(original, prefix: new HarmonyMethod(prefix));
                Log.LogInfo($"Successfully patched {targetType.Name}.{methodName} (prefix)");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to patch {targetType.Name}.{methodName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Patch GameSave for character load detection (LoadCharacter). Save/Load triggers are in PatchGameSaveSaveLoad.
        /// </summary>
        private void PatchGameSave()
        {
            try
            {
                var gameSaveType = typeof(GameSave);
                var loadCharMethod = AccessTools.Method(gameSaveType, "LoadCharacter", new[] { typeof(int) });
                if (loadCharMethod != null)
                {
                    var postfix = AccessTools.Method(typeof(GameSavePatches), "OnLoadCharacter");
                    if (postfix != null)
                    {
                        _harmony.Patch(loadCharMethod, postfix: new HarmonyMethod(postfix));
                        Log.LogInfo("Patched GameSave.LoadCharacter");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error patching GameSave: {ex.Message}");
            }
        }

        /// <summary>
        /// Patch GameSave.SaveGame and LoadGame for vault persistence (game has no SaveLoadManager).
        /// </summary>
        private void PatchGameSaveSaveLoad()
        {
            try
            {
                var gameSaveType = typeof(GameSave);
                var saveGameMethod = AccessTools.Method(gameSaveType, "SaveGame");
                if (saveGameMethod != null)
                {
                    var postfix = AccessTools.Method(typeof(SaveLoadPatches), "OnGameSaved");
                    if (postfix != null)
                    {
                        _harmony.Patch(saveGameMethod, postfix: new HarmonyMethod(postfix));
                        Log.LogInfo("Patched GameSave.SaveGame");
                    }
                }

                var loadGameMethod = AccessTools.Method(gameSaveType, "LoadGame");
                if (loadGameMethod != null)
                {
                    var postfix = AccessTools.Method(typeof(SaveLoadPatches), "OnGameLoaded");
                    if (postfix != null)
                    {
                        _harmony.Patch(loadGameMethod, postfix: new HarmonyMethod(postfix));
                        Log.LogInfo("Patched GameSave.LoadGame");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error patching GameSave save/load: {ex.Message}");
            }
        }

        /// <summary>
        /// Patch Wish.Shop.BuyItem for vault purchase checks (game uses Shop, not ShopMenu).
        /// </summary>
        private void PatchShopBuyItem()
        {
            try
            {
                var shopType = typeof(Shop);
                var shopItemInfo2Type = typeof(ShopItemInfo2);
                var shopLoot2Type = typeof(ShopLoot2);
                if (shopItemInfo2Type != null)
                {
                    var buyItemMethod = AccessTools.Method(shopType, "BuyItem", new[] { shopItemInfo2Type, typeof(int) });
                    if (buyItemMethod != null)
                    {
                        var prefix = AccessTools.Method(typeof(ShopPatches), "OnBeforeBuyItem");
                        var postfix = AccessTools.Method(typeof(ShopPatches), "OnAfterBuyItem");
                        if (prefix != null)
                        {
                            _harmony.Patch(buyItemMethod, prefix: new HarmonyMethod(prefix));
                            Log.LogInfo("Patched Shop.BuyItem(ShopItemInfo2,int) for vault (prefix)");
                        }
                        if (postfix != null)
                        {
                            _harmony.Patch(buyItemMethod, postfix: new HarmonyMethod(postfix));
                            Log.LogInfo("Patched Shop.BuyItem(ShopItemInfo2,int) for vault (postfix)");
                        }
                    }
                }
                if (shopLoot2Type != null)
                {
                    var buyItemMethod = AccessTools.Method(shopType, "BuyItem", new[] { shopLoot2Type, typeof(int) });
                    if (buyItemMethod != null)
                    {
                        var prefix = AccessTools.Method(typeof(ShopPatches), "OnBeforeBuyItem");
                        var postfix = AccessTools.Method(typeof(ShopPatches), "OnAfterBuyItem");
                        if (prefix != null)
                        {
                            _harmony.Patch(buyItemMethod, prefix: new HarmonyMethod(prefix));
                            Log.LogInfo("Patched Shop.BuyItem(ShopLoot2,int) for vault (prefix)");
                        }
                        if (postfix != null)
                        {
                            _harmony.Patch(buyItemMethod, postfix: new HarmonyMethod(postfix));
                            Log.LogInfo("Patched Shop.BuyItem(ShopLoot2,int) for vault (postfix)");
                        }
                    }
                    var buyItemSingle = AccessTools.Method(shopType, "BuyItem", new[] { shopLoot2Type });
                    if (buyItemSingle != null)
                    {
                        var prefix = AccessTools.Method(typeof(ShopPatches), "OnBeforeBuyItemSingle");
                        var postfix = AccessTools.Method(typeof(ShopPatches), "OnAfterBuyItemSingle");
                        if (prefix != null)
                        {
                            _harmony.Patch(buyItemSingle, prefix: new HarmonyMethod(prefix));
                            Log.LogInfo("Patched Shop.BuyItem(ShopLoot2) for vault (prefix)");
                        }
                        if (postfix != null)
                        {
                            _harmony.Patch(buyItemSingle, postfix: new HarmonyMethod(postfix));
                            Log.LogInfo("Patched Shop.BuyItem(ShopLoot2) for vault (postfix)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error patching Shop: {ex.Message}");
            }
        }

        private void PatchItemPickup(Type playerType)
        {
            var pickupMethod = AccessTools.Method(playerType, "Pickup");
            if (pickupMethod != null)
            {
                var prefix = AccessTools.Method(typeof(ItemPatches), "OnPlayerPickupPrefix");
                var postfix = AccessTools.Method(typeof(ItemPatches), "OnPlayerPickup");
                if (prefix != null)
                {
                    _harmony.Patch(pickupMethod,
                        prefix: new HarmonyMethod(prefix),
                        postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                    Log.LogInfo($"Patched {playerType.Name}.Pickup for auto-deposit");
                }
            }
            else
            {
                Log.LogWarning("Could not find Pickup method on Player");
            }

            var inventoryType = typeof(Inventory);
            var invMethods = inventoryType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var itemType = typeof(Item);

            var addItemMethod = AccessTools.Method(inventoryType, "AddItem",
                new[] { itemType, typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(bool) });

            if (addItemMethod != null)
            {
                var prefix = AccessTools.Method(typeof(ItemPatches), "OnInventoryAddItemObjectPrefix");
                var postfix = AccessTools.Method(typeof(ItemPatches), "OnInventoryAddItemObjectPostfix");
                if (prefix != null)
                {
                    _harmony.Patch(addItemMethod,
                        prefix: new HarmonyMethod(prefix),
                        postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                    Log.LogInfo($"Patched {inventoryType.Name}.AddItem(Item,...) for auto-deposit");
                }
                else if (postfix != null)
                {
                    _harmony.Patch(addItemMethod, postfix: new HarmonyMethod(postfix));
                    Log.LogInfo($"Patched {inventoryType.Name}.AddItem(Item,...) postfix only");
                }
            }
            else
            {
                Log.LogWarning("Could not find AddItem(Item,int,int,bool,bool,bool)");
                foreach (var m in invMethods)
                {
                    if (m.Name != "AddItem") continue;
                    var parameters = m.GetParameters();
                    if (parameters.Length > 0 && parameters[0].ParameterType == itemType)
                    {
                        var prefix = AccessTools.Method(typeof(ItemPatches), "OnInventoryAddItemObjectPrefix");
                        var postfix = AccessTools.Method(typeof(ItemPatches), "OnInventoryAddItemObjectPostfix");
                        if (prefix != null)
                        {
                            _harmony.Patch(m,
                                prefix: new HarmonyMethod(prefix),
                                postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                            Log.LogInfo($"Patched {inventoryType.Name}.{m.Name} for auto-deposit");
                            break;
                        }
                        if (postfix != null)
                        {
                            _harmony.Patch(m, postfix: new HarmonyMethod(postfix));
                            Log.LogInfo($"Patched {inventoryType.Name}.{m.Name} postfix only");
                            break;
                        }
                    }
                }
            }

            // Fallback: game has AddItem(int, int, bool) not AddItem(int, int)
            var addItemIntMethod = AccessTools.Method(inventoryType, "AddItem", new[] { typeof(int), typeof(int) });
            if (addItemIntMethod == null)
                addItemIntMethod = AccessTools.Method(inventoryType, "AddItem", new[] { typeof(int), typeof(int), typeof(bool) });
            if (addItemIntMethod != null)
            {
                var postfix = AccessTools.Method(typeof(ItemPatches), "OnInventoryAddItem");
                if (postfix != null)
                {
                    _harmony.Patch(addItemIntMethod, postfix: new HarmonyMethod(postfix));
                    Log.LogInfo($"Successfully patched {inventoryType.Name}.AddItem for auto-deposit");
                }
            }

            // Patch GetAmount to include vault amounts - makes shops see vault currency
            var getAmountMethod = AccessTools.Method(inventoryType, "GetAmount", new[] { typeof(int) });
            if (getAmountMethod != null)
            {
                var postfix = AccessTools.Method(typeof(ItemPatches), "OnInventoryGetAmount");
                if (postfix != null)
                {
                    _harmony.Patch(getAmountMethod, postfix: new HarmonyMethod(postfix));
                    Log.LogInfo($"Successfully patched {inventoryType.Name}.GetAmount for vault integration");
                }
            }
            else
            {
                Log.LogWarning("Could not find Inventory.GetAmount method");
            }

            // Patch HasEnough to check vault - shops and any bag-based cost that uses HasEnough see vault balance
            var hasEnoughMethod = AccessTools.Method(inventoryType, "HasEnough", new[] { typeof(int), typeof(int) });
            if (hasEnoughMethod != null)
            {
                var postfix = AccessTools.Method(typeof(ItemPatches), "OnInventoryHasEnough");
                if (postfix != null)
                {
                    _harmony.Patch(hasEnoughMethod, postfix: new HarmonyMethod(postfix));
                    Log.LogInfo($"Successfully patched {inventoryType.Name}.HasEnough for vault integration");
                }
            }
            else
            {
                Log.LogWarning("Could not find Inventory.HasEnough method");
            }

            // Patch RemoveItem to deduct from vault when inventory is insufficient
            var removeItemMethod = AccessTools.Method(inventoryType, "RemoveItem", new[] { typeof(int), typeof(int), typeof(int) });
            if (removeItemMethod != null)
            {
                var prefix = AccessTools.Method(typeof(ItemPatches), "OnInventoryRemoveItemPrefix");
                var postfix = AccessTools.Method(typeof(ItemPatches), "OnInventoryRemoveItemPostfix");
                if (prefix != null && postfix != null)
                {
                    _harmony.Patch(removeItemMethod,
                        prefix: new HarmonyMethod(prefix),
                        postfix: new HarmonyMethod(postfix));
                    Log.LogInfo($"Successfully patched {inventoryType.Name}.RemoveItem for vault integration");
                }
            }
            else
            {
                Log.LogWarning("Could not find Inventory.RemoveItem method");
            }
        }

        private void Update()
        {
            // Check for auto-save
            if (_enableAutoSave.Value)
            {
                _saveSystem?.CheckAutoSave();
            }

            // Check for HUD toggle
            if (!TextInputFocusGuard.ShouldDeferModHotkeys(Log) && Input.GetKeyDown(_hudToggleKey.Value))
            {
                _vaultHUD?.Toggle();
            }

            // BACKUP: Poll for menu scene changes
            // This is a failsafe in case SceneManager.sceneLoaded stops firing
            _sceneCheckTimer += Time.deltaTime;
            if (_sceneCheckTimer >= SCENE_CHECK_INTERVAL)
            {
                _sceneCheckTimer = 0f;
                CheckForMenuSceneChange();
            }

            // Heartbeat - prove the plugin is still running
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
            {
                _heartbeatTimer = 0f;
                _heartbeatCount++;
                Log.LogInfo($"[Heartbeat #{_heartbeatCount}] Plugin alive. Scene: {_lastKnownScene}, VaultLoaded: {PlayerPatches.IsVaultLoaded}, Character: {PlayerPatches.LoadedCharacterName ?? "none"}");
            }
        }

        /// <summary>
        /// Backup menu detection via polling.
        /// Checks the active scene name and triggers SaveAndReset when entering a menu scene.
        /// </summary>
        private void CheckForMenuSceneChange()
        {
            try
            {
                var activeScene = SceneManager.GetActiveScene();
                string sceneName = activeScene.name;

                // Only log if scene actually changed
                if (sceneName != _lastKnownScene)
                {
                    Log.LogInfo($"[ScenePoll] Scene changed: '{_lastKnownScene}' -> '{sceneName}'");
                    _lastKnownScene = sceneName;

                    string sceneLower = sceneName.ToLowerInvariant();
                    bool isMenuScene = sceneLower.Contains("menu") || sceneLower.Contains("title");

                    // Detect transition INTO menu scene (was not in menu, now is)
                    if (isMenuScene && !_wasInMenuScene)
                    {
                        Log.LogInfo($"[ScenePoll] Menu scene detected via polling: {sceneName}");
                        PlayerPatches.SaveAndReset();
                    }

                    _wasInMenuScene = isMenuScene;
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error in CheckForMenuSceneChange: {ex.Message}");
            }
        }

        private static VaultHUD.HUDPosition ParseHUDPosition(string position)
        {
            return position?.ToLower() switch
            {
                "topleft" => VaultHUD.HUDPosition.TopLeft,
                "topcenter" => VaultHUD.HUDPosition.TopCenter,
                "topright" => VaultHUD.HUDPosition.TopRight,
                "bottomleft" => VaultHUD.HUDPosition.BottomLeft,
                "bottomcenter" => VaultHUD.HUDPosition.BottomCenter,
                "bottomright" => VaultHUD.HUDPosition.BottomRight,
                _ => VaultHUD.HUDPosition.TopLeft
            };
        }

        private void OnApplicationQuit()
        {
            // Save vault data on quit
            Log.LogInfo("Application quitting - saving vault data");
            _saveSystem?.ForceSave();
        }

        private void OnDisable()
        {
            Log.LogWarning("[CRITICAL] Plugin OnDisable called! Plugin is being disabled.");
            Log.LogWarning($"[CRITICAL] Last known scene: {_lastKnownScene}");
            Log.LogWarning($"[CRITICAL] Stack trace: {Environment.StackTrace}");
        }

        private void OnDestroy()
        {
            VaultModApiBridge.Instance = null;

            Log.LogWarning("[CRITICAL] Plugin OnDestroy called! Plugin is being destroyed.");
            Log.LogWarning($"[CRITICAL] Last known scene: {_lastKnownScene}");
            Log.LogWarning($"[CRITICAL] Stack trace: {Environment.StackTrace}");
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // IMPORTANT: Do NOT unpatch Harmony here!
            // Harmony patches are global and will continue working even after this MonoBehaviour is destroyed.
            // If we unpatch, the LoadCharacter and InitializeAsOwner hooks stop working,
            // which breaks character switching entirely.
            // Only unpatch in OnApplicationQuit when the game is actually closing.
            // _harmony?.UnpatchSelf(); // REMOVED - this was breaking character switching!

            _saveSystem?.ForceSave();
        }

        /// <summary>
        /// Called when a new scene is loaded.
        /// We only care about detecting menu scenes to reset vault state.
        /// Actual vault loading is handled by OnPlayerInitialized.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                // Log ALL scene changes for debugging
                Log.LogInfo($"[SceneChange] Scene loaded: '{scene.name}' (mode: {mode})");

                string sceneLower = scene.name.ToLowerInvariant();

                // Detect menu/title scenes to reset vault state
                if (sceneLower.Contains("menu") || sceneLower.Contains("title"))
                {
                    Log.LogInfo($"Menu scene detected: {scene.name}");
                    PlayerPatches.SaveAndReset();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error in OnSceneLoaded: {ex.Message}");
            }
        }

        #region Public API

        /// <summary>
        /// Get the vault manager instance
        /// </summary>
        public static VaultManager GetVaultManager()
        {
            // Use static field which survives Plugin destruction
            return _staticVaultManager;
        }

        /// <summary>
        /// Get the save system instance
        /// </summary>
        public static VaultSaveSystem GetSaveSystem()
        {
            // Use static field which survives Plugin destruction
            return _staticSaveSystem;
        }

        /// <summary>
        /// Get the vault UI instance
        /// </summary>
        public static VaultUI GetVaultUI()
        {
            // Use static field which survives Plugin destruction
            return _staticVaultUI;
        }

        /// <summary>Legacy-only build: uGUI panel is disabled and always null.</summary>
        public static VaultUguiPanel GetVaultUgui() => null;

        /// <summary>Legacy-only build: always true (IMGUI path).</summary>
        public static bool LegacyImguiVaultEnabled() => true;

        /// <summary>Whether the active main vault window (IMGUI or uGUI) is open.</summary>
        public static bool IsMainVaultPanelVisible()
        {
            return _staticVaultUI != null && _staticVaultUI.IsVisible;
        }

        /// <summary>Open or close the vault using whichever UI mode config selected.</summary>
        public static void ToggleMainVaultWindow()
        {
            _staticVaultUI?.Toggle();
        }

        /// <summary>
        /// Open the vault UI
        /// </summary>
        public static void OpenVault()
        {
            _staticVaultUI?.Show();
        }

        /// <summary>
        /// Close the vault UI
        /// </summary>
        public static void CloseVault()
        {
            _staticVaultUI?.Hide();
        }

        /// <summary>
        /// Load vault data for a player
        /// </summary>
        public static void LoadVaultForPlayer(string playerName)
        {
            // Use static field which survives Plugin destruction
            _staticSaveSystem?.Load(playerName);
        }

        /// <summary>
        /// Force save vault data
        /// </summary>
        public static void SaveVault()
        {
            // Use static field which survives Plugin destruction
            _staticSaveSystem?.ForceSave();
        }

        /// <summary>
        /// Get the vault HUD instance
        /// </summary>
        public static VaultHUD GetVaultHUD()
        {
            // Use static field which survives Plugin destruction
            return _staticVaultHUD;
        }

        /// <summary>
        /// Toggle the vault HUD visibility
        /// </summary>
        public static void ToggleHUD()
        {
            // Use static field which survives Plugin destruction
            _staticVaultHUD?.Toggle();
        }

        #endregion
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.azraelgodking.thevault";
        public const string PLUGIN_NAME = "The Vault";
        public const string PLUGIN_VERSION = "3.0.3";
    }

    /// <summary>
    /// A separate MonoBehaviour that runs on a hidden GameObject.
    /// This survives the game's UIHandler.UnloadGame cleanup because:
    /// 1. It's marked DontDestroyOnLoad
    /// 2. It's hidden from Unity's hierarchy (HideFlags)
    /// 3. It's not a child of any game object the cleanup knows about
    /// </summary>
    public class PersistentUpdateRunner : MonoBehaviour
    {
        private string _lastKnownScene = "";
        private bool _wasInMenuScene = true;
        private float _sceneCheckTimer = 0f;
        private float _heartbeatTimer = 0f;
        private int _heartbeatCount = 0;

        private const float SCENE_CHECK_INTERVAL = 0.5f;
        private const float HEARTBEAT_INTERVAL = 30f;

        private void Awake()
        {
            // Hide this object from the game's cleanup routines
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            Plugin.Log?.LogInfo("[PersistentRunner] Created hidden persistent runner");
        }

        private void Update()
        {
            // Poll for menu scene changes
            _sceneCheckTimer += Time.deltaTime;
            if (_sceneCheckTimer >= SCENE_CHECK_INTERVAL)
            {
                _sceneCheckTimer = 0f;
                CheckForMenuSceneChange();
            }

            // Heartbeat
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
            {
                _heartbeatTimer = 0f;
                _heartbeatCount++;
                Plugin.Log?.LogInfo($"[PersistentRunner Heartbeat #{_heartbeatCount}] Scene: {_lastKnownScene}, VaultLoaded: {PlayerPatches.IsVaultLoaded}, Character: {PlayerPatches.LoadedCharacterName ?? "none"}");
            }

            // Handle hotkey detection for Vault UI (since VaultUI might be destroyed)
            CheckHotkeys();

            // Drain auto-deposit notifications off the pickup path (reduces lag)
            if (PlayerPatches.IsVaultLoaded)
                ItemPatches.DrainAutoDepositNotifications();
        }

        private void CheckHotkeys()
        {
            try
            {
                if (TextInputFocusGuard.ShouldDeferModHotkeys(Plugin.Log))
                    return;

                if (Plugin.GetVaultUI() == null) return;

                // Check for vault toggle key (with modifier)
                bool modifierHeld = !Plugin.StaticRequireCtrl ||
                    Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

                if (modifierHeld && Input.GetKeyDown(Plugin.StaticToggleKey))
                {
                    Plugin.ToggleMainVaultWindow();
                }

                // Check for alternative toggle key (no modifier - for Steam Deck)
                if (Plugin.StaticAltToggleKey != KeyCode.None && Input.GetKeyDown(Plugin.StaticAltToggleKey))
                {
                    Plugin.ToggleMainVaultWindow();
                }

                // Check for HUD toggle key
                if (Input.GetKeyDown(Plugin.StaticHUDToggleKey))
                {
                    var vaultHUD = Plugin.GetVaultHUD();
                    vaultHUD?.Toggle();
                }

            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[PersistentRunner] Hotkey error: {ex.Message}");
            }
        }

        private void CheckForMenuSceneChange()
        {
            try
            {
                var activeScene = SceneManager.GetActiveScene();
                string sceneName = activeScene.name;

                if (sceneName != _lastKnownScene)
                {
                    Plugin.Log?.LogInfo($"[PersistentRunner] Scene changed: '{_lastKnownScene}' -> '{sceneName}'");
                    _lastKnownScene = sceneName;

                    string sceneLower = sceneName.ToLowerInvariant();
                    bool isMenuScene = sceneLower.Contains("menu") || sceneLower.Contains("title");

                    if (isMenuScene && !_wasInMenuScene)
                    {
                        Plugin.Log?.LogInfo($"[PersistentRunner] Menu scene detected: {sceneName}");
                        PlayerPatches.SaveAndReset();
                    }

                    _wasInMenuScene = isMenuScene;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[PersistentRunner] Error: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            Plugin.Log?.LogWarning("[PersistentRunner] OnDestroy called - this should NOT happen!");
        }
    }
}
