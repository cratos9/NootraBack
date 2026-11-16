namespace MensajeService.DTOs.Conversations;

public class CreateGroupRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<Guid> ParticipantIds { get; set; } = new();
}