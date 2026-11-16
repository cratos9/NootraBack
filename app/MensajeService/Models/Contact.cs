using MensajeService.Models.Enums;

namespace MensajeService.Models;

public class Contact
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RequesterId { get; set; }

    public Guid AddresseeId { get; set; }

    public Guid UserLowId { get; set; }

    public Guid UserHighId { get; set; }

    public ContactStatus Status { get; set; } = ContactStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}