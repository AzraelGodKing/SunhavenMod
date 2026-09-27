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
                        "Haven's Mirror\n" +
                        "==============\n" +
                        "\n" +
                        "This mod shows a custom picture of your farmer when you talk to people.\n" +
                        "You add PNG image files; the mod displays them as your dialogue bust.\n" +
                        "\n" +
                        "1. Where to put your images\n" +
                        "---------------------------\n" +
                        "Next to this mod, open (or create) the gallery folder:\n" +
                        "\n" +
                        "  BepInEx/plugins/HavensMirror/gallery/\n" +
                        "\n" +
                        "Then pick one of these:\n" +
                        "\n" +
                        "  A) One character only\n" +
                        "     Make a folder named exactly like that character.\n" +
                        "     Example: gallery/Azrael/\n" +
                        "\n" +
                        "  B) Every character (shared)\n" +
                        "     Put images in gallery/_shared/\n" +
                        "     (that is this folder — the shared fallback).\n" +
                        "\n" +
                        "The character folder is checked first. If it has no images, the mod\n" +
                        "uses gallery/_shared/ instead.\n" +
                        "\n" +
                        "2. What to name the files\n" +
                        "------------------------\n" +
                        "Use PNG files with these exact names:\n" +
                        "\n" +
                        "  spring.png    everyday / spring (also the default)\n" +
                        "  summer.png\n" +
                        "  autumn.png\n" +
                        "  winter.png\n" +
                        "  vows.png      wedding / ceremony\n" +
                        "  shore.png     swimsuit / beach\n" +
                        "  costume.png   halloween / costume\n" +
                        "\n" +
                        "You do not need every file. If a name is missing, the mod reuses\n" +
                        "the first PNG it finds in that same folder.\n" +
                        "\n" +
                        "3. How to reload\n" +
                        "---------------\n" +
                        "After you add or change images:\n" +
                        "\n" +
                        "  1. Save the PNG files into the folder above.\n" +
                        "  2. In game, press Ctrl+F8 (changeable in HavensMirror.cfg).\n" +
                        "  3. Talk to someone — your new bust should appear.\n" +
                        "\n" +
                        "No game restart needed after a reload.\n");
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
