using System;
using System.IO;
using System.Reflection;
using HavensMirror.Config;

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

        public static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return SharedFolderName;

            char[] invalid = Path.GetInvalidFileNameChars();
            var chars = name.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0)
                    chars[i] = '_';
            }

            string cleaned = new string(chars).Trim();
            return string.IsNullOrEmpty(cleaned) ? SharedFolderName : cleaned;
        }

        public static void EnsureStarterLayout(Action<string> logInfo = null)
        {
            if (MirrorOptions.CreateStarterFolders == null || !MirrorOptions.CreateStarterFolders.Value)
                return;

            try
            {
                Directory.CreateDirectory(SharedFolder);
                string howto = Path.Combine(SharedFolder, "HOWTO.txt");
                if (!File.Exists(howto))
                {
                    File.WriteAllText(howto,
                        "Haven's Mirror — drop PNG bust portraits here.\n" +
                        "\n" +
                        "Per-character folders: gallery/<YourCharacterName>/\n" +
                        "Shared fallback: gallery/_shared/\n" +
                        "\n" +
                        "Expected file names:\n" +
                        "  spring.png   (default / spring look)\n" +
                        "  summer.png\n" +
                        "  autumn.png\n" +
                        "  winter.png\n" +
                        "  vows.png     (wedding / ceremony)\n" +
                        "  shore.png    (swimsuit / beach)\n" +
                        "  costume.png  (halloween / costume)\n" +
                        "\n" +
                        "Missing files fall back to the first PNG found in the folder.\n" +
                        "Reload in-game with Ctrl+F8 (configurable).\n");
                }

                logInfo?.Invoke($"Gallery ready at '{GalleryRoot}'.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[Gallery] Could not create starter folders: {ex.Message}");
            }
        }
    }
}
