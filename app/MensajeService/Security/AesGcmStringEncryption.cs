using System.Security.Cryptography;
using System.Text;

namespace MensajeService.Security;

public static class AesGcmStringEncryption
{
    private const string Prefix = "enc:v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static string Encrypt(string value, byte[] key, string purpose)
    {
        var plaintext = Encoding.UTF8.GetBytes(value);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        var associatedData = Encoding.UTF8.GetBytes(purpose);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

        var encryptedData = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, encryptedData, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, encryptedData, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, encryptedData, NonceSize + TagSize, ciphertext.Length);

        return Prefix + Convert.ToBase64String(encryptedData);
    }

    public static string Decrypt(string value, byte[] key, string purpose)
    {
        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
            return value;

        var encryptedData = Convert.FromBase64String(value[Prefix.Length..]);

        if (encryptedData.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Invalid encrypted data.");
        }

        var nonce = encryptedData[..NonceSize];
        var tag = encryptedData[NonceSize..(NonceSize + TagSize)];
        var ciphertext = encryptedData[(NonceSize + TagSize)..];
        var plaintext = new byte[ciphertext.Length];
        var associatedData = Encoding.UTF8.GetBytes(purpose);

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);

        return Encoding.UTF8.GetString(plaintext);
    }
}