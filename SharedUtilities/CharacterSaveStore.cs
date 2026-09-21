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
    /// When <see cref="LoadWithBackup{T}"/> returns null, distinguishes missing files from unreadable/corrupt ones.
    /// </summary>
    public enum CharacterSaveAbsenceReason
    {
        NoFiles,
        FilesPresentButUnusable
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

        /// <summary>
        /// Replace invalid filename characters, then trim (matches pre-CharacterSaveStore mod behavior).
        /// </summary>
        public static string SanitizeFileName(string name, string fallback = "unknown")
        {
            if (string.IsNullOrWhiteSpace(name))
                return fallback;

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            name = name.Trim();
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
        /// When load returned null, tells callers whether to treat the outcome as "no save yet" vs "corrupt/unreadable".
        /// </summary>
        public static CharacterSaveAbsenceReason GetAbsenceReason(string filePath, string backupSuffix = BackupSuffix)
        {
            if (string.IsNullOrEmpty(filePath))
                return CharacterSaveAbsenceReason.NoFiles;

            bool primaryExists = File.Exists(filePath);
            bool backupExists = File.Exists(filePath + backupSuffix);
            return primaryExists || backupExists
                ? CharacterSaveAbsenceReason.FilesPresentButUnusable
                : CharacterSaveAbsenceReason.NoFiles;
        }

        /// <summary>
        /// Writes text atomically: .tmp → rotate live to backup → promote .tmp.
        /// Returns false on IO failure after best-effort restore; does not throw for those failures.
        /// </summary>
        /// <param name="deleteTempInFinally">Obsolete: kept for call-site compatibility. Temp is only deleted on success or when failure happened before the live file was rotated.</param>
        public static bool WriteAtomic(
            string filePath,
            string content,
            string backupSuffix = BackupSuffix,
            bool deleteTempInFinally = true)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            return WriteAtomicCore(
                filePath,
                tempPath => WriteAllTextDurable(tempPath, content),
                backupSuffix,
                deleteTempInFinally);
        }

        /// <summary>
        /// Writes bytes atomically (encrypted vault payloads).
        /// </summary>
        /// <param name="deleteTempInFinally">Obsolete: kept for call-site compatibility. See <see cref="WriteAtomic"/>.</param>
        public static bool WriteAtomicBytes(
            string filePath,
            byte[] content,
            string backupSuffix = BackupSuffix,
            bool deleteTempInFinally = true)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            return WriteAtomicCore(
                filePath,
                tempPath => WriteAllBytesDurable(tempPath, content),
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
            string backupSuffix = BackupSuffix,
            Action<string, Exception> onReadFailure = null)
            where T : class
        {
            source = CharacterSaveSource.None;
            if (string.IsNullOrEmpty(filePath) || tryDeserialize == null)
                return null;

            if (File.Exists(filePath))
            {
                string text = TryReadAllText(filePath, onReadFailure);
                if (text != null)
                {
                    try
                    {
                        T item = tryDeserialize(text);
                        if (item != null)
                        {
                            source = CharacterSaveSource.Primary;
                            return item;
                        }
                    }
                    catch (Exception ex)
                    {
                        onReadFailure?.Invoke(filePath, ex);
                    }
                }
            }

            string backupPath = filePath + backupSuffix;
            if (File.Exists(backupPath))
            {
                string text = TryReadAllText(backupPath, onReadFailure);
                if (text != null)
                {
                    try
                    {
                        T item = tryDeserialize(text);
                        if (item != null)
                        {
                            source = CharacterSaveSource.Backup;
                            return item;
                        }
                    }
                    catch (Exception ex)
                    {
                        onReadFailure?.Invoke(backupPath, ex);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Reads primary text, then backup. Useful for non-JSON line formats.
        /// </summary>
        public static string LoadTextWithBackup(
            string filePath,
            out CharacterSaveSource source,
            string backupSuffix = BackupSuffix,
            Action<string, Exception> onReadFailure = null)
        {
            source = CharacterSaveSource.None;
            if (string.IsNullOrEmpty(filePath))
                return null;

            if (File.Exists(filePath))
            {
                string primary = TryReadAllText(filePath, onReadFailure);
                if (primary != null)
                {
                    source = CharacterSaveSource.Primary;
                    return primary;
                }
            }

            string backupPath = filePath + backupSuffix;
            if (File.Exists(backupPath))
            {
                string backup = TryReadAllText(backupPath, onReadFailure);
                if (backup != null)
                {
                    source = CharacterSaveSource.Backup;
                    return backup;
                }
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
            string backupPath = filePath + backupSuffix;
            bool rotatedLiveToBackup = false;
            try
            {
                writeTemp(tempPath);

                if (File.Exists(filePath))
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Move(filePath, backupPath);
                    rotatedLiveToBackup = true;
                }

                File.Move(tempPath, filePath);
                // Success: drop temp if somehow still present (Move should have removed it).
                TryDelete(tempPath);
                return true;
            }
            catch (Exception)
            {
                // Restore rotated live file when promote failed after the move-away.
                if (rotatedLiveToBackup && !File.Exists(filePath) && File.Exists(backupPath))
                {
                    try
                    {
                        File.Move(backupPath, filePath);
                    }
                    catch
                    {
                        // Best-effort restore; caller still gets false.
                    }
                }

                // Only delete temp when we never rotated the live file — otherwise keep .tmp for recovery.
                // deleteTempInFinally=false (Vault) always keeps .tmp on failure.
                if (deleteTempInFinally && !rotatedLiveToBackup)
                    TryDelete(tempPath);

                return false;
            }
        }

        private static void WriteAllTextDurable(string path, string content)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(fs))
            {
                writer.Write(content);
                writer.Flush();
                fs.Flush(true);
            }
        }

        private static void WriteAllBytesDurable(string path, byte[] content)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(content, 0, content.Length);
                fs.Flush(true);
            }
        }

        private static string TryReadAllText(string path, Action<string, Exception> onReadFailure)
        {
            try
            {
                if (!File.Exists(path))
                    return null;
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                onReadFailure?.Invoke(path, ex);
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
                // Best-effort temp cleanup only; failure is non-fatal.
            }
        }
    }
}
