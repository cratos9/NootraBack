namespace MensajeService.DTOs.Contacts;

public class ContactResponseDto
{
    public Guid Id { get; set; }

    public Guid RequesterId { get; set; }

    public Guid AddresseeId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}