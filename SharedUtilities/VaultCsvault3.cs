using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// CSVAULT3 vault file format (AZR-243). Tamper and corruption detection for local saves, not confidentiality
    /// against the file's owner.
    /// <para>
    /// Layout: <c>"CSVAULT3" | version(1) | keyKind(1) | salt(16) | iv(16) | AES-256-CBC ciphertext | HMAC-SHA256(32)</c>.
    /// The MAC covers every byte before it and is verified before decrypting.
    /// </para>
    /// <para>
    /// Keys come from PBKDF2-HMAC-SHA256 over the player identity with a random per-vault salt. The identity is
    /// player-only so the key never depends on whether Steamworks has loaded yet (AZR-242).
    /// </para>
    /// Kept free of Unity and BepInEx so the net8 test project can link it.
    /// </summary>
    public static class VaultCsvault3
    {
        public const int Iterations = 100000;
        public const byte FormatVersion = 1;
        public const byte KeyKindPlayer = 1;

        private const int SaltSize = 16;
        private const int IvSize = 16;
        private const int KeySize = 32;
        private const int MacSize = 32;
        private const int AesBlockSize = 16;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("CSVAULT3");
        private static readonly int HeaderSize = Magic.Length + 2 + SaltSize + IvSize;
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false, true);

        private static readonly object CacheLock = new object();
        private static string _cachedIdentity;
        private static byte[] _cachedSalt;
        private static byte[] _cachedEncKey;
        private static byte[] _cachedMacKey;

        public static bool HasHeader(byte[] data)
        {
            if (data == null || data.Length < Magic.Length)
                return false;
            for (int i = 0; i < Magic.Length; i++)
            {
                if (data[i] != Magic[i])
                    return false;
            }
            return true;
        }

        public static byte[] Encrypt(string plainText, string playerName)
        {
            return Encrypt(plainText, playerName, Iterations);
        }

        public static bool TryDecrypt(byte[] data, string playerName, out string plainText)
        {
            return TryDecrypt(data, playerName, Iterations, out plainText);
        }

        internal static byte[] Encrypt(string plainText, string playerName, int iterations)
        {
            string identity = Identity(playerName);
            GetOrCreateKeys(identity, iterations, out byte[] salt, out byte[] encKey, out byte[] macKey);

            byte[] iv = RandomBytes(IvSize);
            byte[] cipher;
            using (var aes = CreateAes(encKey, iv))
            using (var encryptor = aes.CreateEncryptor())
            {
                byte[] plain = Utf8NoBom.GetBytes(plainText ?? string.Empty);
                cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
            }

            using (var ms = new MemoryStream(HeaderSize + cipher.Length + MacSize))
            {
                ms.Write(Magic, 0, Magic.Length);
                ms.WriteByte(FormatVersion);
                ms.WriteByte(KeyKindPlayer);
                ms.Write(salt, 0, salt.Length);
                ms.Write(iv, 0, iv.Length);
                ms.Write(cipher, 0, cipher.Length);
                byte[] body = ms.ToArray();
                byte[] mac = ComputeMac(macKey, body, body.Length);
                ms.Write(mac, 0, mac.Length);
                return ms.ToArray();
            }
        }

        internal static bool TryDecrypt(byte[] data, string playerName, int iterations, out string plainText)
        {
            plainText = null;
            if (!HasHeader(data) || data.Length < HeaderSize + AesBlockSize + MacSize)
                return false;
            if (data[Magic.Length] != FormatVersion || data[Magic.Length + 1] != KeyKindPlayer)
                return false;

            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[IvSize];
            Buffer.BlockCopy(data, Magic.Length + 2, salt, 0, SaltSize);
            Buffer.BlockCopy(data, Magic.Length + 2 + SaltSize, iv, 0, IvSize);

            string identity = Identity(playerName);
            DeriveForSalt(identity, salt, iterations, out byte[] encKey, out byte[] macKey);

            int macOffset = data.Length - MacSize;
            byte[] expected = ComputeMac(macKey, data, macOffset);
            if (!FixedTimeEquals(expected, data, macOffset))
                return false;

            int cipherLength = macOffset - HeaderSize;
            if (cipherLength <= 0 || cipherLength % AesBlockSize != 0)
                return false;

            try
            {
                using (var aes = CreateAes(encKey, iv))
                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] plain = decryptor.TransformFinalBlock(data, HeaderSize, cipherLength);
                    plainText = Utf8NoBom.GetString(plain);
                }
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            lock (CacheLock)
            {
                _cachedIdentity = identity;
                _cachedSalt = salt;
                _cachedEncKey = encKey;
                _cachedMacKey = macKey;
            }
            return true;
        }

        /// <summary>Drops the session key cache. Tests use it to force a fresh salt.</summary>
        internal static void ResetCache()
        {
            lock (CacheLock)
            {
                _cachedIdentity = null;
                _cachedSalt = null;
                _cachedEncKey = null;
                _cachedMacKey = null;
            }
        }

        internal static byte[] Pbkdf2Sha256(byte[] password, byte[] salt, int iterations, int length)
        {
            byte[] output = new byte[length];
            byte[] saltBlock = new byte[salt.Length + 4];
            Buffer.BlockCopy(salt, 0, saltBlock, 0, salt.Length);
            using (var hmac = new HMACSHA256(password))
            {
                int blockCount = (length + 31) / 32;
                for (int block = 1; block <= blockCount; block++)
                {
                    saltBlock[salt.Length] = (byte)(block >> 24);
                    saltBlock[salt.Length + 1] = (byte)(block >> 16);
                    saltBlock[salt.Length + 2] = (byte)(block >> 8);
                    saltBlock[salt.Length + 3] = (byte)block;

                    byte[] u = hmac.ComputeHash(saltBlock);
                    byte[] t = (byte[])u.Clone();
                    for (int i = 1; i < iterations; i++)
                    {
                        u = hmac.ComputeHash(u);
                        for (int k = 0; k < t.Length; k++)
                            t[k] ^= u[k];
                    }

                    int offset = (block - 1) * 32;
                    Buffer.BlockCopy(t, 0, output, offset, Math.Min(32, length - offset));
                }
            }
            return output;
        }

        private static string Identity(string playerName)
        {
            return "TheVault|CSVAULT3|Player_" + (playerName ?? string.Empty);
        }

        private static void GetOrCreateKeys(string identity, int iterations, out byte[] salt, out byte[] encKey, out byte[] macKey)
        {
            lock (CacheLock)
            {
                if (_cachedIdentity == identity && _cachedSalt != null)
                {
                    salt = _cachedSalt;
                    encKey = _cachedEncKey;
                    macKey = _cachedMacKey;
                    return;
                }
            }

            salt = RandomBytes(SaltSize);
            DeriveKeys(identity, salt, iterations, out encKey, out macKey);
            lock (CacheLock)
            {
                _cachedIdentity = identity;
                _cachedSalt = salt;
                _cachedEncKey = encKey;
                _cachedMacKey = macKey;
            }
        }

        private static void DeriveForSalt(string identity, byte[] salt, int iterations, out byte[] encKey, out byte[] macKey)
        {
            lock (CacheLock)
            {
                if (_cachedIdentity == identity && _cachedSalt != null && FixedTimeEquals(_cachedSalt, salt, 0))
                {
                    encKey = _cachedEncKey;
                    macKey = _cachedMacKey;
                    return;
                }
            }
            DeriveKeys(identity, salt, iterations, out encKey, out macKey);
        }

        private static void DeriveKeys(string identity, byte[] salt, int iterations, out byte[] encKey, out byte[] macKey)
        {
            byte[] material = Pbkdf2Sha256(Utf8NoBom.GetBytes(identity), salt, iterations, KeySize * 2);
            encKey = new byte[KeySize];
            macKey = new byte[KeySize];
            Buffer.BlockCopy(material, 0, encKey, 0, KeySize);
            Buffer.BlockCopy(material, KeySize, macKey, 0, KeySize);
        }

        private static byte[] ComputeMac(byte[] macKey, byte[] data, int count)
        {
            using (var hmac = new HMACSHA256(macKey))
                return hmac.ComputeHash(data, 0, count);
        }

        private static bool FixedTimeEquals(byte[] expected, byte[] actual, int actualOffset)
        {
            if (actual.Length - actualOffset < expected.Length)
                return false;
            int diff = 0;
            for (int i = 0; i < expected.Length; i++)
                diff |= expected[i] ^ actual[actualOffset + i];
            return diff == 0;
        }

        private static Aes CreateAes(byte[] key, byte[] iv)
        {
            var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        private static byte[] RandomBytes(int count)
        {
            byte[] bytes = new byte[count];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return bytes;
        }
    }
}
