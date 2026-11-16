namespace MensajeService.DTOs.Contacts;

public class ContactPreferenceResponseDto
{
    public Guid ContactId { get; set; }

    public bool IsPinned { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}