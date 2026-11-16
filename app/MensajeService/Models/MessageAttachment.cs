namespace MensajeService.Models;

public class MessageAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MessageId { get; set; }

    public string OriginalFilename { get; set; } = string.Empty;

    public string StoredFilename { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Message Message { get; set; } = null!;
}