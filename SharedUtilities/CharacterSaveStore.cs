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
    /// Matches the temp → backup rotate → promote contract documented in ATOMIC_SAVE_POLICY.md.
    /// </summary>
    public static class CharacterSaveStore
    {
        public const string BackupSuffix = ".bak";
        public const string VaultBackupSuffix = ".backup";
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
        /// Writes text atomically: .tmp → rotate live to backup → promote .tmp.
        /// </summary>
        /// <param name="deleteTempInFinally">When false (The Vault), a failed write may leave .tmp for manual recovery.</param>
        public static bool WriteAtomic(
            string filePath,
            string content,
            string backupSuffix = BackupSuffix,
            bool deleteTempInFinally = true)
        {
            return WriteAtomicCore(
                filePath,
                tempPath => File.WriteAllText(tempPath, content),
                backupSuffix,
                deleteTempInFinally);
        }

        /// <summary>
        /// Writes bytes atomically (encrypted vault payloads).
        /// </summary>
        public static bool WriteAtomicBytes(
            string filePath,
            byte[] content,
            string backupSuffix = BackupSuffix,
            bool deleteTempInFinally = true)
        {
            if (content == null)
                return false;

            return WriteAtomicCore(
                filePath,
                tempPath => File.WriteAllBytes(tempPath, content),
                backupSuffix,
                deleteTempInFinally);
        }

        /// <summary>
        /// Tries primary, then backup. Returns null when both are missing or unreadable.
        /// </summary>
        public static T LoadWithBackup<T>(
            string filePath,
            Func<string, T> tryDeserialize,
            out CharacterSaveSource source,
            string backupSuffix = BackupSuffix)
            where T : class
        {
            source = CharacterSaveSource.None;
            if (string.IsNullOrEmpty(filePath) || tryDeserialize == null)
                return null;

            if (File.Exists(filePath))
            {
                string text = TryReadAllText(filePath);
                if (text != null)
                {
                    T item = tryDeserialize(text);
                    if (item != null)
                    {
                        source = CharacterSaveSource.Primary;
                        return item;
                    }
                }
            }

            string backupPath = filePath + backupSuffix;
            if (File.Exists(backupPath))
            {
                string text = TryReadAllText(backupPath);
                if (text != null)
                {
                    T item = tryDeserialize(text);
                    if (item != null)
                    {
                        source = CharacterSaveSource.Backup;
                        return item;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Reads primary text, then backup. Useful for non-JSON line formats.
        /// </summary>
        public static string LoadTextWithBackup(string filePath, out CharacterSaveSource source, string backupSuffix = BackupSuffix)
        {
            source = CharacterSaveSource.None;
            if (string.IsNullOrEmpty(filePath))
                return null;

            string primary = TryReadAllText(filePath);
            if (primary != null)
            {
                source = CharacterSaveSource.Primary;
                return primary;
            }

            string backup = TryReadAllText(filePath + backupSuffix);
            if (backup != null)
            {
                source = CharacterSaveSource.Backup;
                return backup;
            }

            return null;
        }

        public static bool LooksLikeJsonObject(string text) =>
            !string.IsNullOrWhiteSpace(text) && text.TrimStart().StartsWith("{", StringComparison.Ordinal);

        private static bool WriteAtomicCore(
            string filePath,
            Action<string> writeTemp,
            string backupSuffix,
            bool deleteTempInFinally)
        {
            if (string.IsNullOrEmpty(filePath) || writeTemp == null)
                return false;

            string tempPath = filePath + TempSuffix;
            try
            {
                writeTemp(tempPath);

                if (File.Exists(filePath))
                {
                    string backupPath = filePath + backupSuffix;
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Move(filePath, backupPath);
                }

                File.Move(tempPath, filePath);
                return true;
            }
            finally
            {
                if (deleteTempInFinally)
                    TryDelete(tempPath);
            }
        }

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
