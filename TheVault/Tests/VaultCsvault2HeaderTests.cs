using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace TheVault.Tests
{
    /// <summary>
    /// Documents the AZR-240 CSVAULT2 contract: ciphertext after the 8-byte ASCII header must be
    /// fed to AES — feeding the header itself (legacy DecryptWithKey bug) always fails.
    /// Production recovery uses SharedUtilities.VaultCryptography.DecryptAesBodyAfterHeader.
    /// </summary>
    [TestFixture]
    public class VaultCsvault2HeaderTests
    {
        private const string EncryptionSalt = "TheV4ultS@lt2026Secure";
        private const int Iterations = 10000;
        private static readonly byte[] Iv =
        {
            0x43, 0x75, 0x72, 0x72, 0x65, 0x6E, 0x63, 0x79, 0x53, 0x70, 0x65, 0x6C, 0x6C, 0x49, 0x56, 0x31
        };

        [TestCase("Hero", "\"PlayerName\":\"Hero\"")]
        [TestCase("Player_Hero", "\"PlayerName\":\"Hero\",\"Entries\":[]")]
        public void LegacyPayload_DecryptSucceedsOnlyWhenHeaderSkipped(string playerKeyPart, string jsonBody)
        {
            string keySource = $"{EncryptionSalt}_{playerKeyPart}_TheVaultPortable";
            byte[] key = DeriveKey(keySource);
            string json = "{" + jsonBody + "}";
            byte[] cipher = EncryptWithCsvault2Header(json, key);

            Assert.That(DecryptSkippingHeader(cipher, key), Is.EqualTo(json));
            Assert.That(DecryptIncludingHeader(cipher, key), Is.Null,
                "Feeding CSVAULT2 header into AES (pre-AZR-240 DecryptWithKey) must fail");
        }

        private static byte[] DeriveKey(string combined)
        {
            // SHA1 matches SharedUtilities.VaultCryptography / legacy CSVAULT2 keys (default of the obsolete ctor).
            using (var deriveBytes = new Rfc2898DeriveBytes(
                combined,
                Encoding.UTF8.GetBytes(EncryptionSalt),
                Iterations,
                HashAlgorithmName.SHA1))
                return deriveBytes.GetBytes(32);
        }

        private static byte[] EncryptWithCsvault2Header(string plainText, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = Iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    byte[] header = Encoding.UTF8.GetBytes("CSVAULT2");
                    ms.Write(header, 0, header.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs, Encoding.UTF8))
                        writer.Write(plainText);
                    return ms.ToArray();
                }
            }
        }

        private static string DecryptSkippingHeader(byte[] cipherData, byte[] key)
        {
            int offset = 0;
            if (cipherData.Length >= 8 && Encoding.UTF8.GetString(cipherData, 0, 8) == "CSVAULT2")
                offset = 8;
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = Iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherData, offset, cipherData.Length - offset))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        private static string DecryptIncludingHeader(byte[] cipherData, byte[] key)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = Iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] decrypted = decryptor.TransformFinalBlock(cipherData, 0, cipherData.Length);
                        return Encoding.UTF8.GetString(decrypted);
                    }
                }
            }
            catch (CryptographicException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
