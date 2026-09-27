using HavensMirror.Config;
using HavensMirror.Gallery;
using UnityEngine;

namespace HavensMirror.Hotkeys
{
    /// <summary>
    /// Periodically re-syncs gallery character folders when GameSave.Saves becomes available or changes
    /// (e.g. title screen → character list loaded after plugin Awake).
    /// </summary>
    public sealed class GalleryFolderSyncLoop : MonoBehaviour
    {
        private const float IntervalSeconds = 5f;
        private float _nextScan;
        private int _passes;

        private void OnEnable()
        {
            _nextScan = 0f;
            _passes = 0;
        }

        private void Update()
        {
            if (MirrorOptions.Enabled == null || !MirrorOptions.Enabled.Value)
                return;

            if (Time.unscaledTime < _nextScan)
                return;

            _nextScan = Time.unscaledTime + IntervalSeconds;
            _passes++;

            int created = SaveSlotFolderSync.EnsureCharacterFolders();
            if (created > 0)
                Plugin.Log?.LogInfo($"[Gallery] Save-slot sync created {created} folder(s).");

            // After many quiet passes, slow down (still catch new characters / new saves).
            if (_passes > 24 && created == 0)
                _nextScan = Time.unscaledTime + 30f;
        }
    }
}
