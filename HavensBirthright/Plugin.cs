using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HavensBirthright.Abilities;
using HarmonyLib;
using SunhavenMods.Shared;
using System;
using System.Linq;
using UnityEngine;

namespace HavensBirthright
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
        public static ConfigFile ConfigFile { get; private set; }

        // Static config value for BirthrightRunner hotkey detection
        internal static UnityEngine.KeyCode StaticAbilityToggleKey = UnityEngine.KeyCode.F9;

        private Harmony _harmony;
        private RacialBonusManager _racialBonusManager;
        private ConfigEntry<bool> _checkForUpdates;
        private BirthrightRunner _runner;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            ConfigFile = Config;

            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");

            try
            {
                // Initialize configuration
                RacialConfig.Initialize(Config);
                AbilityConfig.Initialize(Config);

                // Cache static keybind for BirthrightRunner to access
                StaticAbilityToggleKey = AbilityConfig.ActiveAbilityToggleKey.Value;

                _checkForUpdates = Config.Bind(
                    "Updates",
                    "CheckForUpdates",
                    true,
                    "Check for mod updates on startup"
                );

                // Initialize the racial bonus manager
                _racialBonusManager = new RacialBonusManager();

                // Create the persistent runner for ability update loop
                _runner = PersistentRunnerBase.CreateRunner<BirthrightRunner>();
                Log.LogInfo("BirthrightRunner created for active abilities");

                // Apply Harmony patches
                _harmony = new Harmony(PluginInfo.PLUGIN_GUID);

                try
                {
                    var playerType = typeof(Wish.Player);
                    Log.LogInfo($"Player type: {playerType.FullName} from {playerType.Assembly.GetName().Name}");

                    // Patch InitializeAsOwner for race detection
                    PatchMethod(playerType, "InitializeAsOwner",
                        typeof(Patches.PlayerPatches), "OnPlayerInitialized");

                    // Patch Initialize as backup for race detection
                    PatchMethod(playerType, "Initialize",
                        typeof(Patches.PlayerPatches), "OnPlayerInitialize",
                        Type.EmptyTypes);

                    // Patch GetStat for stat bonuses (combat, skills, regen, abilities, drawbacks, synergies)
                    PatchMethod(playerType, "GetStat",
                        typeof(Patches.StatPatches), "ModifyGetStat",
                        new[] { typeof(Wish.StatType) });

                    // Patch ReceiveDamage for defense + combat abilities (prefix)
                    PatchMethodPrefix(playerType, "ReceiveDamage",
                        typeof(Patches.CombatPatches), "ModifyDamageReceived");

                    // Patch ReceiveDamage for dodge detection + Divine Ward trigger (postfix)
                    PatchMethod(playerType, "ReceiveDamage",
                        typeof(Patches.CombatPatches), "OnDamageReceivedPostfix");

                    // Patch NPCAI.AddFriendship for relationship bonuses/drawbacks
                    var npcaiType = AccessTools.TypeByName("Wish.NPCAI");
                    if (npcaiType != null)
                    {
                        PatchMethodPrefix(npcaiType, "AddFriendship",
                            typeof(Patches.EconomyPatches), "ModifyRelationshipGain",
                            new[] { typeof(int) });
                    }
                    else
                    {
                        Log.LogWarning("Could not find NPCAI type - relationship bonuses will not work");
                    }

                    // Patch ShopMenu.BuyItem for shop discounts
                    var shopMenuType = AccessTools.TypeByName("Wish.ShopMenu");
                    if (shopMenuType != null)
                    {
                        PatchMethodPrefix(shopMenuType, "BuyItem",
                            typeof(Patches.EconomyPatches), "ModifyBuyPrice");
                    }
                    else
                    {
                        Log.LogWarning("Could not find ShopMenu type - shop discounts will not work");
                    }

                    // Patch Player.AddMana to block mana regen while Infernal Forge is active
                    PatchMethodPrefix(playerType, "AddMana",
                        typeof(Patches.AbilityPatches), "OnPlayerAddManaPrefix");

                    // NOTE: Infernal Forge no longer uses a Harmony prefix on Inventory.AddItem.
                    // It now uses a periodic inventory scan in BirthrightRunner.UpdateInfernalForge()
                    // because ore is picked up 1 at a time, making per-pickup smelting impossible.

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
                catch (Exception patchEx)
                {
                    Log.LogError($"Harmony patching failed: {patchEx}");
                }

                // Check for updates
                if (_checkForUpdates.Value)
                {
                    VersionChecker.CheckForUpdate(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_VERSION, Log,
                        result => result.NotifyUpdateAvailable(Log));
                }

                Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully!");
                Log.LogInfo($"Active abilities: {(AbilityConfig.EnableActiveAbilities.Value ? "ENABLED" : "DISABLED")}");
                Log.LogInfo($"Racial drawbacks: {(AbilityConfig.EnableRacialDrawbacks.Value ? "ENABLED" : "DISABLED")}");
                Log.LogInfo($"Conditional synergies: {(AbilityConfig.EnableConditionalSynergies.Value ? "ENABLED" : "DISABLED")}");
                Log.LogInfo($"Ability toggle key: {StaticAbilityToggleKey}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to load {PluginInfo.PLUGIN_NAME}: {ex}");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        /// <summary>
        /// Helper method to manually patch a method with a postfix
        /// </summary>
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

        /// <summary>
        /// Helper method to manually patch a method with a prefix
        /// </summary>
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
        /// Gets the racial bonus manager instance
        /// </summary>
        public static RacialBonusManager GetRacialBonusManager()
        {
            return Instance?._racialBonusManager;
        }

        /// <summary>
        /// Ensures BirthrightRunner exists. Recreates it if destroyed (e.g. by UIHandler.UnloadGame).
        /// Called from PlayerPatches.OnPlayerInitialized() on every game load.
        /// </summary>
        public static void EnsureRunner()
        {
            if (Instance == null) return;

            // Check if runner is null or destroyed (destroyed Unity objects bypass C# null check)
            if (Instance._runner == null || (Instance._runner is UnityEngine.Object obj && obj == null))
            {
                Instance._runner = PersistentRunnerBase.CreateRunner<BirthrightRunner>();
                Log?.LogInfo("[Plugin] BirthrightRunner recreated after destruction");
            }
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.azraelgodking.havensbirthright";
        public const string PLUGIN_NAME = "Haven's Birthright";
        public const string PLUGIN_VERSION = "1.2.0";
    }
}
