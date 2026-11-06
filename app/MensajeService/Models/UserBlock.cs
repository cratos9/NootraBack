namespace MensajeService.Models;

public class UserBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BlockerId { get; set; }

    public Guid BlockedId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}