using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using SunhavenMods.Shared;

namespace TheVault.Tests
{
    [TestFixture]
    public class VaultCsvault3Tests
    {
        private const int FastIterations = 1000;
        private const string Json = "{\"PlayerName\":\"Hero\",\"Entries\":[{\"CurrencyId\":\"spring\",\"Amount\":283}]}";

        [SetUp]
        public void ResetKeyCache() => VaultCsvault3.ResetCache();

        [TestCase(1)]
        [TestCase(FastIterations)]
        public void Pbkdf2Sha256_MatchesFrameworkImplementation(int iterations)
        {
            byte[] password = Encoding.UTF8.GetBytes("TheVault|CSVAULT3|Player_Hero");
            byte[] salt = Encoding.UTF8.GetBytes("0123456789abcdef");
            byte[] expected;
            using (var reference = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                expected = reference.GetBytes(64);

            Assert.That(VaultCsvault3.Pbkdf2Sha256(password, salt, iterations, 64), Is.EqualTo(expected));
        }

        [Test]
        public void RoundTrip_AtProductionIterations()
        {
            byte[] file = VaultCsvault3.Encrypt(Json, "Hero");
            VaultCsvault3.ResetCache();

            Assert.That(VaultCsvault3.HasHeader(file), Is.True);
            Assert.That(VaultCsvault3.TryDecrypt(file, "Hero", out string json), Is.True);
            Assert.That(json, Is.EqualTo(Json));
        }

        [Test]
        public void RoundTrip_PreservesUnicodeAndControlCharacters()
        {
            string text = "{\"PlayerName\":\"Hérø 勇者\",\"Note\":\"tab\\t\\u0001\"}";
            byte[] file = VaultCsvault3.Encrypt(text, "Hérø 勇者", FastIterations);

            Assert.That(VaultCsvault3.TryDecrypt(file, "Hérø 勇者", FastIterations, out string json), Is.True);
            Assert.That(json, Is.EqualTo(text));
        }

        [Test]
        public void SessionReusesSaltButNotIv()
        {
            byte[] first = VaultCsvault3.Encrypt(Json, "Hero", FastIterations);
            byte[] second = VaultCsvault3.Encrypt(Json, "Hero", FastIterations);

            Assert.That(Slice(first, 10, 16), Is.EqualTo(Slice(second, 10, 16)), "salt is per vault");
            Assert.That(Slice(first, 26, 16), Is.Not.EqualTo(Slice(second, 26, 16)), "IV is per write");
            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void FreshSessionGetsFreshSalt()
        {
            byte[] first = VaultCsvault3.Encrypt(Json, "Hero", FastIterations);
            VaultCsvault3.ResetCache();
            byte[] second = VaultCsvault3.Encrypt(Json, "Hero", FastIterations);

            Assert.That(Slice(first, 10, 16), Is.Not.EqualTo(Slice(second, 10, 16)));
        }

        [TestCase(8, TestName = "version byte")]
        [TestCase(12, TestName = "salt")]
        [TestCase(30, TestName = "iv")]
        [TestCase(45, TestName = "ciphertext")]
        [TestCase(-1, TestName = "mac")]
        public void SingleFlippedByte_IsRejected(int index)
        {
            byte[] file = VaultCsvault3.Encrypt(Json, "Hero", FastIterations);
            int at = index < 0 ? file.Length + index : index;
            file[at] ^= 0x01;

            Assert.That(VaultCsvault3.TryDecrypt(file, "Hero", FastIterations, out string json), Is.False);
            Assert.That(json, Is.Null);
        }

        [Test]
        public void TruncatedFile_IsRejected()
        {
            byte[] file = VaultCsvault3.Encrypt(Json, "Hero", FastIterations);
            byte[] truncated = Slice(file, 0, file.Length - 20);

            Assert.That(VaultCsvault3.TryDecrypt(truncated, "Hero", FastIterations, out _), Is.False);
        }

        [Test]
        public void OtherCharacter_IsRejected()
        {
            byte[] file = VaultCsvault3.Encrypt(Json, "Hero", FastIterations);
            VaultCsvault3.ResetCache();

            Assert.That(VaultCsvault3.TryDecrypt(file, "Villain", FastIterations, out _), Is.False);
        }

        [Test]
        public void Csvault2File_HasNoCsvault3Header()
        {
            Assert.That(VaultCsvault3.HasHeader(Encoding.ASCII.GetBytes("CSVAULT2........")), Is.False);
        }

        private static byte[] Slice(byte[] source, int offset, int count)
        {
            byte[] slice = new byte[count];
            System.Buffer.BlockCopy(source, offset, slice, 0, count);
            return slice;
        }
    }
}
