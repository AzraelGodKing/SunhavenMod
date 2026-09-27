using BepInEx.Configuration;
using UnityEngine;

namespace HavensMirror.Config
{
    /// <summary>
    /// BepInEx config bindings for Haven's Mirror.
    /// </summary>
    public static class MirrorOptions
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> GalleryFolderName { get; private set; }
        public static ConfigEntry<string> SharedFolderName { get; private set; }
        public static ConfigEntry<string> ForcedLook { get; private set; }
        public static ConfigEntry<KeyCode> ReloadModifier { get; private set; }
        public static ConfigEntry<KeyCode> ReloadKey { get; private set; }
        public static ConfigEntry<bool> NotifyOnReload { get; private set; }
        public static ConfigEntry<bool> CreateStarterFolders { get; private set; }

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind(
                "General",
                "Enabled",
                true,
                "Master switch for Haven's Mirror. When false, dialogue busts and look overrides are untouched.");

            GalleryFolderName = config.Bind(
                "General",
                "GalleryFolderName",
                "gallery",
                "Folder name under the plugin directory that holds per-character portrait subfolders.");

            SharedFolderName = config.Bind(
                "General",
                "SharedFolderName",
                "_shared",
                "Subfolder used when the active character has no portrait folder of their own.");

            ForcedLook = config.Bind(
                "General",
                "ForcedLook",
                "",
                "Optional look override for NPC dialogue busts and seasonal NPC outfits. " +
                "One of: Spring, Summer, Autumn, Winter, Vows, Shore, Costume. Leave blank to use the game calendar.");

            ReloadModifier = config.Bind(
                "Hotkeys",
                "ReloadModifier",
                KeyCode.LeftControl,
                "Modifier held with ReloadKey to refresh portrait PNGs from disk. Set to None to require only ReloadKey.");

            ReloadKey = config.Bind(
                "Hotkeys",
                "ReloadKey",
                KeyCode.F8,
                "Key that reloads gallery PNGs when pressed with ReloadModifier (if set).");

            NotifyOnReload = config.Bind(
                "Hotkeys",
                "NotifyOnReload",
                true,
                "Show an in-game notification after a successful gallery reload.");

            CreateStarterFolders = config.Bind(
                "General",
                "CreateStarterFolders",
                true,
                "On load, create the gallery/_shared folder (and a short HOWTO.txt) if missing.");
        }
    }
}
