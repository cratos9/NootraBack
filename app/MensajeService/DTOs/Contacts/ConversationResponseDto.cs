namespace MensajeService.DTOs.Conversations;

public class ConversationResponseDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ConversationParticipantResponseDto> Participants { get; set; } = new();
}