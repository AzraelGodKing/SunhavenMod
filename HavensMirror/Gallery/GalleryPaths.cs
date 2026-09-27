using System;
using System.IO;
using System.Reflection;
using HavensMirror.Config;
using SunhavenMods.Shared;

namespace HavensMirror.Gallery
{
    /// <summary>
    /// Resolves gallery root and per-character folders under the plugin directory.
    /// </summary>
    public static class GalleryPaths
    {
        public static string PluginDirectory
        {
            get
            {
                string loc = Assembly.GetExecutingAssembly().Location;
                return string.IsNullOrEmpty(loc)
                    ? "."
                    : Path.GetDirectoryName(loc) ?? ".";
            }
        }

        public static string GalleryRoot
        {
            get
            {
                string folder = MirrorOptions.GalleryFolderName?.Value;
                if (string.IsNullOrWhiteSpace(folder))
                    folder = "gallery";
                return Path.Combine(PluginDirectory, folder.Trim());
            }
        }

        public static string SharedFolderName
        {
            get
            {
                string name = MirrorOptions.SharedFolderName?.Value;
                return string.IsNullOrWhiteSpace(name) ? "_shared" : name.Trim();
            }
        }

        public static string CharacterFolder(string characterName)
        {
            string safe = SanitizeFolderName(characterName);
            return Path.Combine(GalleryRoot, safe);
        }

        public static string SharedFolder => Path.Combine(GalleryRoot, SharedFolderName);

        public static string FileNameFor(LookSlot slot)
        {
            switch (slot)
            {
                case LookSlot.Summer: return "summer.png";
                case LookSlot.Autumn: return "autumn.png";
                case LookSlot.Winter: return "winter.png";
                case LookSlot.Vows: return "vows.png";
                case LookSlot.Shore: return "shore.png";
                case LookSlot.Costume: return "costume.png";
                default: return "spring.png";
            }
        }

        /// <summary>
        /// Suite-consistent filename sanitization (matches CharacterSaveStore / other mods).
        /// </summary>
        public static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return SharedFolderName;

            string cleaned = CharacterSaveStore.SanitizeFileName(name.Trim(), SharedFolderName);
            return string.IsNullOrEmpty(cleaned) ? SharedFolderName : cleaned;
        }

        /// <summary>
        /// True when the folder exists and contains at least one known portrait PNG name.
        /// Empty character folders (created from saves) are ignored for display.
        /// </summary>
        public static bool FolderHasPortraitFiles(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return false;

            foreach (LookSlot slot in Enum.GetValues(typeof(LookSlot)))
            {
                if (File.Exists(Path.Combine(folder, FileNameFor(slot))))
                    return true;
            }

            return false;
        }

        public static void EnsureStarterLayout(Action<string> logInfo = null)
        {
            if (MirrorOptions.CreateStarterFolders == null || !MirrorOptions.CreateStarterFolders.Value)
                return;

            try
            {
                CharacterSaveStore.EnsureDirectory(GalleryRoot);
                CharacterSaveStore.EnsureDirectory(SharedFolder);
                string howto = Path.Combine(SharedFolder, "HOWTO.txt");
                File.WriteAllText(howto,
                    "Haven's Mirror\n" +
                    "==============\n" +
                    "\n" +
                    "What happens\n" +
                    "------------\n" +
                    "1. You install the mod.\n" +
                    "2. When the game runs, the mod reads your character saves and creates\n" +
                    "   an empty folder for each one under gallery/ (named like the character).\n" +
                    "3. Empty folders are fine — they are ignored until you add PNGs.\n" +
                    "4. Drop PNG bust images into a character folder (or into this _shared folder).\n" +
                    "5. Talk to someone — if that folder has images, your bust appears.\n" +
                    "\n" +
                    "Where images go\n" +
                    "---------------\n" +
                    "  gallery/<YourCharacterName>/   (created automatically from saves)\n" +
                    "  gallery/_shared/               (this folder — used if a character folder is empty)\n" +
                    "\n" +
                    "File names (PNG)\n" +
                    "----------------\n" +
                    "  spring.png    everyday / spring (default)\n" +
                    "  summer.png\n" +
                    "  autumn.png\n" +
                    "  winter.png\n" +
                    "  vows.png      wedding / ceremony\n" +
                    "  shore.png     swimsuit / beach\n" +
                    "  costume.png   halloween / costume\n" +
                    "\n" +
                    "You do not need every file. Missing names reuse the first PNG in that folder.\n" +
                    "\n" +
                    "Reload\n" +
                    "------\n" +
                    "After adding or changing images, press Ctrl+F8 in game (configurable).\n" +
                    "No restart needed.\n");

                logInfo?.Invoke($"Gallery ready at '{GalleryRoot}'.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[Gallery] Could not create starter folders: {ex.Message}");
            }
        }
    }
}
