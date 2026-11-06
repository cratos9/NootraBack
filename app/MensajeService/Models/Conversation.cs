using MensajeService.Models.Enums;

namespace MensajeService.Models;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ConversationType Type { get; set; } = ConversationType.Direct;

    public string? DirectKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
}