namespace MensajeService.DTOs.Conversations;

public class ConversationParticipantResponseDto
{
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }
}