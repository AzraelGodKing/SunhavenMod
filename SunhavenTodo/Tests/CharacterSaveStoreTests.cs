using System;
using System.IO;
using NUnit.Framework;
using SunhavenMods.Shared;

namespace SunhavenTodo.Tests
{
    [TestFixture]
    public class CharacterSaveStoreTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "CharacterSaveStoreTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Test]
        public void SanitizeFileName_ReplacesInvalidCharacters()
        {
            Assert.That(CharacterSaveStore.SanitizeFileName("A/B:C"), Is.EqualTo("A_B_C"));
            Assert.That(CharacterSaveStore.SanitizeFileName(null), Is.EqualTo("unknown"));
        }

        [Test]
        public void SanitizeFileName_ReplacesControlCharsBeforeTrim()
        {
            // Pre-CharacterSaveStore mods replaced invalid chars then trimmed — tabs become underscores, not stripped.
            Assert.That(CharacterSaveStore.SanitizeFileName("\tHero\t"), Is.EqualTo("_Hero_"));
        }

        [Test]
        public void GetAbsenceReason_DistinguishesMissingFromUnusable()
        {
            string path = Path.Combine(_tempDir, "missing.json");
            Assert.That(
                CharacterSaveStore.GetAbsenceReason(path),
                Is.EqualTo(CharacterSaveAbsenceReason.NoFiles));

            CharacterSaveStore.WriteAtomic(path, "not-json");
            Assert.That(
                CharacterSaveStore.GetAbsenceReason(path),
                Is.EqualTo(CharacterSaveAbsenceReason.FilesPresentButUnusable));
        }

        [Test]
        public void WriteAtomic_ThrowsWhenContentNull()
        {
            string path = Path.Combine(_tempDir, "hero_todos.json");
            Assert.Throws<ArgumentNullException>(() => CharacterSaveStore.WriteAtomic(path, null));
        }

        [Test]
        public void WriteAtomicBytes_ThrowsWhenContentNull()
        {
            string path = Path.Combine(_tempDir, "hero.vault");
            Assert.Throws<ArgumentNullException>(() =>
                CharacterSaveStore.WriteAtomicBytes(path, null));
        }

        [Test]
        public void WriteAtomic_DeletesTempFileWhenPromoteFails()
        {
            string path = Path.Combine(_tempDir, "hero_todos.json");
            string tempPath = path + CharacterSaveStore.TempSuffix;
            CharacterSaveStore.WriteAtomic(path, "{\"v\":1}");
            // Block backup rotate so the write fails after .tmp is created.
            Directory.CreateDirectory(path + CharacterSaveStore.BackupSuffix);

            Assert.Throws<IOException>(() => CharacterSaveStore.WriteAtomic(path, "{\"v\":2}"));
            Assert.That(File.Exists(tempPath), Is.False);
        }

        [Test]
        public void WriteAtomic_RotatesBackupAndLoadsPrimary()
        {
            string path = Path.Combine(_tempDir, "hero_todos.json");
            Assert.That(CharacterSaveStore.WriteAtomic(path, "{\"v\":1}"), Is.True);
            Assert.That(CharacterSaveStore.WriteAtomic(path, "{\"v\":2}"), Is.True);

            Assert.That(File.ReadAllText(path), Is.EqualTo("{\"v\":2}"));
            Assert.That(File.ReadAllText(path + CharacterSaveStore.BackupSuffix), Is.EqualTo("{\"v\":1}"));
            Assert.That(File.Exists(path + CharacterSaveStore.TempSuffix), Is.False);
        }

        [Test]
        public void WriteAtomicBytes_UsesCustomBackupSuffix()
        {
            string path = Path.Combine(_tempDir, "hero.vault");
            Assert.That(
                CharacterSaveStore.WriteAtomicBytes(
                    path,
                    new byte[] { 1, 2, 3 },
                    CharacterSaveStore.VaultBackupSuffix,
                    deleteTempInFinally: false),
                Is.True);

            Assert.That(File.ReadAllBytes(path), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(File.Exists(path + CharacterSaveStore.VaultBackupSuffix), Is.False);
        }

        [Test]
        public void LoadWithBackup_FallsBackWhenPrimaryCorrupt()
        {
            string path = Path.Combine(_tempDir, "hero_todos.json");
            CharacterSaveStore.WriteAtomic(path, "{\"ok\":true}");
            CharacterSaveStore.WriteAtomic(path, "{\"ok\":false}");
            File.WriteAllText(path, "not-json");

            string loaded = CharacterSaveStore.LoadWithBackup(
                path,
                json => CharacterSaveStore.LooksLikeJsonObject(json) ? json : null,
                out var source);

            Assert.That(loaded, Is.EqualTo("{\"ok\":true}"));
            Assert.That(source, Is.EqualTo(CharacterSaveSource.Backup));
        }

        [Test]
        public void LoadWithBackup_ReportsDeserializeFailureAndFallsBackToBackup()
        {
            string path = Path.Combine(_tempDir, "hero_todos.json");
            CharacterSaveStore.WriteAtomic(path, "{\"ok\":true}");
            CharacterSaveStore.WriteAtomic(path, "{\"ok\":false}");
            File.WriteAllText(path, "{\"bad\":");

            Exception reported = null;
            string reportedPath = null;
            int deserializeCalls = 0;
            string loaded = CharacterSaveStore.LoadWithBackup<string>(
                path,
                text =>
                {
                    deserializeCalls++;
                    if (deserializeCalls == 1)
                        throw new InvalidOperationException("deserialize boom");
                    return text;
                },
                out var source,
                onReadFailure: (p, ex) =>
                {
                    reportedPath = p;
                    reported = ex;
                });

            Assert.That(loaded, Is.EqualTo("{\"ok\":true}"));
            Assert.That(source, Is.EqualTo(CharacterSaveSource.Backup));
            Assert.That(reportedPath, Is.EqualTo(path));
            Assert.That(reported, Is.TypeOf<InvalidOperationException>());
            Assert.That(reported.Message, Is.EqualTo("deserialize boom"));
        }

        [Test]
        public void LoadTextWithBackup_ReturnsEmptyPrimaryWithoutFallingBackToBackup()
        {
            string path = Path.Combine(_tempDir, "hero_favorites.txt");
            CharacterSaveStore.WriteAtomic(path, "npc\tgift\n");
            File.WriteAllText(path, string.Empty);

            string loaded = CharacterSaveStore.LoadTextWithBackup(path, out var source);
            Assert.That(loaded, Is.EqualTo(string.Empty));
            Assert.That(source, Is.EqualTo(CharacterSaveSource.Primary));
        }

        [Test]
        public void LoadTextWithBackup_ReadsPrimaryFile()
        {
            string path = Path.Combine(_tempDir, "hero_favorites.txt");
            CharacterSaveStore.WriteAtomic(path, "npc\tgift\n");

            string loaded = CharacterSaveStore.LoadTextWithBackup(path, out var source);
            Assert.That(loaded, Does.Contain("npc\tgift"));
            Assert.That(source, Is.EqualTo(CharacterSaveSource.Primary));
        }
    }
}
