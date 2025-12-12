using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PassGuard
{
    public static class CryptoService
    {
        private static readonly byte[] Salt = new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10
        };

        public static bool ValidateMasterPassword(string password, string settingsFile)
        {
            if (!File.Exists(settingsFile))
            {
                // First run - create password hash
                string hash = ComputeHash(password);
                File.WriteAllText(settingsFile, hash);
                return true;
            }

            string savedHash = File.ReadAllText(settingsFile);
            string inputHash = ComputeHash(password);
            return savedHash == inputHash;
        }

        private static string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input + "SecretSalt");
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public static string Encrypt(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText)) return "";

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] key = GenerateKey(password);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = new byte[16]; // Simple IV

                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(plainBytes, 0, plainBytes.Length);
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch
            {
                return plainText; // Return plain text if encryption fails
            }
        }

        public static string Decrypt(string cipherText, string password)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] key = GenerateKey(password);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = new byte[16];

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(cipherBytes))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                return "Decryption error";
            }
        }

        private static byte[] GenerateKey(string password)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(
                password,
                Salt,
                10000,
                HashAlgorithmName.SHA256))
            {
                return deriveBytes.GetBytes(32); // 256-bit key
            }
        }
    }
}