using HavensMirror.Config;
using UnityEngine;

namespace HavensMirror.Hotkeys
{
    /// <summary>
    /// Polls reload hotkey each frame while the plugin is alive.
    /// </summary>
    public sealed class GalleryReloadLoop : MonoBehaviour
    {
        private void Update()
        {
            if (MirrorOptions.Enabled == null || !MirrorOptions.Enabled.Value)
                return;

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
    }
}
