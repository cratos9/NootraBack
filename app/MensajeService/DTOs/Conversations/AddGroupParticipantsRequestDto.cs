namespace MensajeService.DTOs.Conversations;

public class AddGroupParticipantsRequestDto
{
    public List<Guid> ParticipantIds { get; set; } = new();
}