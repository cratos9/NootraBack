using MensajeService.Models.Enums;

namespace MensajeService.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConversationId { get; set; }

    public Guid SenderId { get; set; }

    public string? Content { get; set; }

    public MessageType Type { get; set; } = MessageType.Text;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsEdited { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public ICollection<MessageReceipt> Receipts { get; set; } = new List<MessageReceipt>();

    public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
}