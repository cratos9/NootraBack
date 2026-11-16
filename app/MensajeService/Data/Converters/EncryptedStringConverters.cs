using MensajeService.Security;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MensajeService.Data.Converters;

public class EncryptedMessageContentConverter : ValueConverter<string, string>
{
    public EncryptedMessageContentConverter() : base(
            value => AesGcmStringEncryption.Encrypt(value, EncryptionKeyProvider.Key, "messages.content"),
            value => AesGcmStringEncryption.Decrypt(value, EncryptionKeyProvider.Key, "messages.content")
        )
    {
    }
}

public class EncryptedAttachmentFilenameConverter : ValueConverter<string, string>
{
    public EncryptedAttachmentFilenameConverter() : base(
            value => AesGcmStringEncryption.Encrypt(value, EncryptionKeyProvider.Key, "message_attachments.original_filename"),
            value => AesGcmStringEncryption.Decrypt(value, EncryptionKeyProvider.Key, "message_attachments.original_filename")
        )
    {
    }
}