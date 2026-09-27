using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HavensMirror.Config;
using HavensMirror.Dialogue;
using HavensMirror.Gallery;
using HavensMirror.Hotkeys;
using HavensMirror.Look;
using SunhavenMods.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HavensMirror
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }
        public static SpriteShelf Shelf { get; private set; }

        private Harmony _harmony;
        private bool _applicationQuitting;
        private GalleryReloadLoop _reloadLoop;
        private GalleryFolderSyncLoop _folderSyncLoop;

        private void Awake()
        {
            Log = Logger;
            ConfigFile configFile = CreateNamedConfig();
            ConfigFileHelper.ReplacePluginConfig(this, configFile, Log.LogWarning);
            MirrorOptions.Bind(configFile);

            Shelf = new SpriteShelf();

            if (MirrorOptions.Enabled == null || !MirrorOptions.Enabled.Value)
            {
                Log.LogInfo($"{PluginInfo.PLUGIN_NAME} disabled in config.");
                ModDiagnostics.LogModStartup(Log, PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION,
                    "standalone",
                    mode: "disabled");
                return;
            }

            GalleryPaths.EnsureStarterLayout(msg => Log.LogInfo(msg));
            // Creates empty gallery/<CharacterName>/ for each save slot when GameSave is already up.
            int created = SaveSlotFolderSync.EnsureCharacterFolders();
            if (created > 0)
                Log.LogInfo($"[Gallery] Created {created} character folder(s) from saves.");

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            try
            {
                DialogueMirrorBinder.Apply(_harmony);
                NpcLookPatches.Apply(_harmony);
            }
            catch (Exception ex)
            {
                Log.LogError($"Harmony setup failed (game version drift?): {ex}");
            }

            _reloadLoop = gameObject.AddComponent<GalleryReloadLoop>();
            _folderSyncLoop = gameObject.AddComponent<GalleryFolderSyncLoop>();

            ReloadGallery(notify: false);

            VersionChecker.CheckForUpdate(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_VERSION, Log,
                onComplete: result => result.NotifyUpdateAvailable(Log));

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded. Gallery: {GalleryPaths.GalleryRoot}");
            ModDiagnostics.LogModStartup(Log, PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION,
                ModHealthIntegrationSummary.Build(("DevTools", SuitePluginGuids.DevTools)),
                mode: "startup");
        }

        public static void ReloadGallery(bool notify)
        {
            if (Shelf == null)
                Shelf = new SpriteShelf();

            // Re-scan saves on reload so newly created characters get folders without restart.
            SaveSlotFolderSync.EnsureCharacterFolders();

            string character = TryReadCharacterName();
            string status = Shelf.LoadForCharacter(character);

            if (Shelf.HasAny)
                Log?.LogInfo($"[Gallery] {status}");
            else
                Log?.LogDebug($"[Gallery] {status}");

            if (notify)
                TryNotify($"[Haven's Mirror] {status}");
        }

        private static string TryReadCharacterName()
        {
            return GameSaveCharacterName.TryGetCurrent(
                fallback: null,
                logWarning: msg => Log?.LogDebug($"[Gallery] Character name resolve failed: {msg}"));
        }

        private static void TryNotify(string message)
        {
            try
            {
                Type stackType = ReflectionHelper.FindWishType("NotificationStack");
                object stack = stackType != null ? ReflectionHelper.GetSingletonInstance(stackType) : null;
                if (stack == null)
                    return;

                MethodInfo send = AccessTools.Method(stackType, "SendNotification", new[] { typeof(string) })
                                  ?? AccessTools.Method(stackType, "SendNotification");
                if (send == null)
                    return;

                var parms = send.GetParameters();
                if (parms.Length == 1 && parms[0].ParameterType == typeof(string))
                    send.Invoke(stack, new object[] { message });
                else if (parms.Length >= 1)
                {
                    object[] args = new object[parms.Length];
                    args[0] = message;
                    for (int i = 1; i < parms.Length; i++)
                        args[i] = parms[i].HasDefaultValue ? parms[i].DefaultValue : GetDefault(parms[i].ParameterType);
                    send.Invoke(stack, args);
                }
            }
            catch (Exception ex)
            {
                Log?.LogDebug($"[Notify] {ex.Message}");
            }
        }

        private static object GetDefault(Type type)
        {
            if (type == typeof(bool)) return false;
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0f;
            if (type.IsValueType) return Activator.CreateInstance(type);
            return null;
        }

        private static ConfigFile CreateNamedConfig()
        {
            return ConfigFileHelper.CreateNamedConfig(PluginInfo.PLUGIN_GUID, "HavensMirror.cfg", msg => Log?.LogWarning(msg));
        }

        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        private void OnDestroy()
        {
            string sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            string sceneLower = sceneName.ToLowerInvariant();
            bool expectedTeardown = _applicationQuitting || !Application.isPlaying
                                    || sceneLower.Contains("menu") || sceneLower.Contains("title");
            if (expectedTeardown)
                Log?.LogInfo($"[Lifecycle] Plugin OnDestroy during expected teardown (scene: {sceneName})");
            else
                Log?.LogWarning($"[Lifecycle] Plugin OnDestroy outside expected teardown (scene: {sceneName})");

            if (_reloadLoop != null)
            {
                Destroy(_reloadLoop);
                _reloadLoop = null;
            }

            if (_folderSyncLoop != null)
            {
                Destroy(_folderSyncLoop);
                _folderSyncLoop = null;
            }

            Shelf?.Dispose();
            Shelf = null;
            _harmony?.UnpatchSelf();
        }
    }
}
