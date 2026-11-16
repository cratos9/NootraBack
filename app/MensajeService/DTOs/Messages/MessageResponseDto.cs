namespace MensajeService.DTOs.Messages;

public class MessageResponseDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string? Content { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? DeletedAt { get; set; }
    public List<MessageReceiptResponseDto> Receipts { get; set; } = new();
    public List<MessageAttachmentResponseDto> Attachments { get; set; } = new();
}