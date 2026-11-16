namespace MensajeService.Models;

public class MessageReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MessageId { get; set; }

    public Guid RecipientId { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public Message Message { get; set; } = null!;
}