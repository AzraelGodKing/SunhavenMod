using System;
using System.IO;

namespace SunhavenMods.Shared
{
    public enum CharacterSaveSource
    {
        None,
        Primary,
        Backup
    }

    /// <summary>
    /// Per-character save path helpers and atomic write / backup fallback load.
    /// Matches the temp → .bak rotate → promote contract documented in ATOMIC_SAVE_POLICY.md.
    /// </summary>
    public static class CharacterSaveStore
    {
        public const string BackupSuffix = ".bak";
        public const string TempSuffix = ".tmp";

        public static string SanitizeFileName(string name, string fallback = "unknown")
        {
            if (string.IsNullOrWhiteSpace(name))
                return fallback;

            name = name.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return string.IsNullOrEmpty(name) ? fallback : name;
        }

        public static string GetFilePath(string directory, string characterName, string fileSuffix, string fallback = "unknown")
        {
            EnsureDirectory(directory);
            return Path.Combine(directory, SanitizeFileName(characterName, fallback) + fileSuffix);
        }

        public static void EnsureDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("Directory is required.", nameof(directory));

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// Writes content atomically: .tmp → rotate live to .bak → promote .tmp.
        /// The temp file is always removed in <c>finally</c> (Senpai's Chest / Todo semantics).
        /// </summary>
        public static bool WriteAtomic(string filePath, string content)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            string tempPath = filePath + TempSuffix;
            try
            {
                File.WriteAllText(tempPath, content);

                if (File.Exists(filePath))
                {
                    string backupPath = filePath + BackupSuffix;
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Move(filePath, backupPath);
                }

                File.Move(tempPath, filePath);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        /// <summary>
        /// Tries primary, then .bak. Returns null when both are missing or unreadable.
        /// </summary>
        public static T LoadWithBackup<T>(string filePath, Func<string, T> tryDeserialize, out CharacterSaveSource source)
            where T : class
        {
            source = CharacterSaveSource.None;
            if (string.IsNullOrEmpty(filePath) || tryDeserialize == null)
                return null;

            if (File.Exists(filePath))
            {
                string json = TryReadAllText(filePath);
                if (json != null)
                {
                    T item = tryDeserialize(json);
                    if (item != null)
                    {
                        source = CharacterSaveSource.Primary;
                        return item;
                    }
                }
            }

            string backupPath = filePath + BackupSuffix;
            if (File.Exists(backupPath))
            {
                string json = TryReadAllText(backupPath);
                if (json != null)
                {
                    T item = tryDeserialize(json);
                    if (item != null)
                    {
                        source = CharacterSaveSource.Backup;
                        return item;
                    }
                }
            }

            return null;
        }

        public static bool LooksLikeJsonObject(string text) =>
            !string.IsNullOrWhiteSpace(text) && text.TrimStart().StartsWith("{", StringComparison.Ordinal);

        private static string TryReadAllText(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;
                return File.ReadAllText(path);
            }
            catch
            {
                return null;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort temp cleanup.
            }
        }
    }
}
