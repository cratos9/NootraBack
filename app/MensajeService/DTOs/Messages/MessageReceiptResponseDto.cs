namespace MensajeService.DTOs.Messages;

public class MessageReceiptResponseDto
{
    public Guid RecipientId { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
}