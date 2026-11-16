using MensajeService.Models.Enums;

namespace MensajeService.Models;

public class ConversationParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public ConversationParticipantRole Role { get; set; } = ConversationParticipantRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public Conversation Conversation { get; set; } = null!;
}