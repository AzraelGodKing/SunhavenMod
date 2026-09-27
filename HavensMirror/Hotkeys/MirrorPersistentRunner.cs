using HavensMirror.Config;
using HavensMirror.Gallery;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavensMirror.Hotkeys
{
    /// <summary>
    /// DDOL runner that keeps gallery folder sync and reload hotkey alive after early
    /// BepInEx Plugin.OnDestroy (empty-scene teardown).
    /// </summary>
    public sealed class MirrorPersistentRunner : PersistentRunnerBase
    {
        private const float SyncIntervalSeconds = 5f;
        private const float SyncSlowIntervalSeconds = 30f;

        private float _nextScan;
        private int _passes;

        protected override string RunnerName => "MirrorPersistentRunner";

        protected override void OnUpdate()
        {
            if (MirrorOptions.Enabled == null || !MirrorOptions.Enabled.Value)
                return;

            CheckReloadHotkey();
            TickFolderSync();
        }

        protected override void OnGameTransition()
        {
            Plugin.ReloadGallery(notify: false);
        }

        protected override void Log(string message)
        {
            Plugin.Log?.LogInfo(message);
        }

        protected override void LogWarning(string message)
        {
            Plugin.Log?.LogWarning(message);
        }

        private void CheckReloadHotkey()
        {
            KeyCode key = MirrorOptions.ReloadKey?.Value ?? KeyCode.None;
            if (key == KeyCode.None)
                return;

            if (!UnityEngine.Input.GetKeyDown(key))
                return;

            KeyCode modifier = MirrorOptions.ReloadModifier?.Value ?? KeyCode.None;
            if (modifier != KeyCode.None && !UnityEngine.Input.GetKey(modifier))
                return;

            Plugin.ReloadGallery(notify: MirrorOptions.NotifyOnReload?.Value ?? true);
        }

        private void TickFolderSync()
        {
            if (Time.unscaledTime < _nextScan)
                return;

            _nextScan = Time.unscaledTime + SyncIntervalSeconds;
            _passes++;

            int created = SaveSlotFolderSync.EnsureCharacterFolders();
            if (created > 0)
                Plugin.Log?.LogInfo($"[Gallery] Save-slot sync created {created} folder(s).");

            // After many quiet passes, slow down (still catch new characters / new saves).
            if (_passes > 24 && created == 0)
                _nextScan = Time.unscaledTime + SyncSlowIntervalSeconds;
        }
    }
}
