namespace MensajeService.DTOs.Messages;

public class MessageStatusResponseDto
{
    public Guid MessageId { get; set; }
    public Guid RecipientId { get; set; }
    public Guid SenderId { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
}