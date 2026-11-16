namespace MensajeService.DTOs.UserBlocks;

public class UserBlockResponseDto
{
    public Guid Id { get; set; }

    public Guid BlockedId { get; set; }

    public DateTime CreatedAt { get; set; }
}