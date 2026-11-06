namespace MensajeService.Models;

public class ContactPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerId { get; set; }

    public Guid ContactId { get; set; }

    public bool IsPinned { get; set; } = false;

    public bool IsAttached { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Contact Contact { get; set; } = null!;
}