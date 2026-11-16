namespace MensajeService.DTOs.Messages;

public class MessageHistoryResponseDto
{
    public List<MessageResponseDto> Items { get; set; } = new();

    public bool HasMore { get; set; }

    public MessageCursorDto? NextCursor { get; set; }
}

public class MessageCursorDto
{
    public DateTime CreatedAt { get; set; }

    public Guid MessageId { get; set; }
}