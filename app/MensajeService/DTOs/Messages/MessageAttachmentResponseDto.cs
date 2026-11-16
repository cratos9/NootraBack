namespace MensajeService.DTOs.Messages;

public class MessageAttachmentResponseDto
{
    public Guid Id { get; set; }
    public string OriginalFilename { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}