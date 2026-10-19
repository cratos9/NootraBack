using System.Security.Cryptography;

namespace AuthService.Services
{
    public static class EncryptionService
    {
        private const int NonceSize = 12;
        private const int TagSize = 16;

        public static string Encrypt(string plainText, string key)
        {
            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[TagSize];

            using (var aes = new AesGcm(keyBytes, TagSize))
            {
                aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            byte[] result = new byte[NonceSize + TagSize + cipherBytes.Length];

            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
            Buffer.BlockCopy(cipherBytes, 0, result, NonceSize + TagSize, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string cipherText, string key)
        {
            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] encryptedBytes = Convert.FromBase64String(cipherText);

            if (encryptedBytes.Length < NonceSize + TagSize)
            {
                throw new CryptographicException("Invalid encrypted data.");
            }

            byte[] nonce = new byte[NonceSize];
            byte[] tag = new byte[TagSize];
            byte[] cipherBytes = new byte[encryptedBytes.Length - NonceSize - TagSize];
            byte[] plainBytes = new byte[cipherBytes.Length];

            Buffer.BlockCopy(encryptedBytes, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(encryptedBytes, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(encryptedBytes, NonceSize + TagSize, cipherBytes, 0, cipherBytes.Length);

            using (var aes = new AesGcm(keyBytes, TagSize))
            {
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            return System.Text.Encoding.UTF8.GetString(plainBytes);
        }
    }
}