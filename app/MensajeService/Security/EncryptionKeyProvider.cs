namespace MensajeService.Security;

public static class EncryptionKeyProvider
{
    private static byte[]? _key;
    public static byte[] Key => _key ?? throw new InvalidOperationException("Encryption key has not been initialized.");
    public static void Initialize(string? base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new InvalidOperationException("Encryption:Key is not configured.");
        }

        byte[] key;

        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Encryption key must be valid Base64.", ex);
        }

        if (key.Length != 32)
        {
            throw new InvalidOperationException("AES-256 requires a 32-byte encryption key.");
        }

        Interlocked.Exchange(ref _key, key);
    }
}