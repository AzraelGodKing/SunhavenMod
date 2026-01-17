using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
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

        private Harmony _harmony;
        private RacialBonusManager _racialBonusManager;

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

                // Initialize the racial bonus manager
                _racialBonusManager = new RacialBonusManager();

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

                    // Patch GetStat for stat bonuses (combat, skills, regen)
                    PatchMethod(playerType, "GetStat",
                        typeof(Patches.StatPatches), "ModifyGetStat",
                        new[] { typeof(Wish.StatType) });

                    // Patch NPCAI.AddFriendship for relationship bonuses (Human, Amari Dog)
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

                    // Patch ShopMenu.BuyItem for shop discounts (Human)
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

                Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully!");
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
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.azraelgodking.havensbirthright";
        public const string PLUGIN_NAME = "Haven's Birthright";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
