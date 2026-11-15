using MensajeService.DTOs.Messages;
using MensajeService.Models;
using MensajeService.Models.Enums;
using MensajeService.Repositories;

namespace MensajeService.Services;

public class MessageService
{
    private readonly MessageRepository _messageRepository;
    private readonly ConversationRepository _conversationRepository;
    private readonly ConversationParticipantRepository _participantRepository;
    private readonly MessageReceiptRepository _receiptRepository;

    public MessageService(MessageRepository messageRepository, ConversationRepository conversationRepository, ConversationParticipantRepository participantRepository, MessageReceiptRepository receiptRepository)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _receiptRepository = receiptRepository;
    }

    public async Task<MessageResponseDto> SendTextAsync(Guid senderId, SendMessageRequestDto request)
    {
        if (senderId == Guid.Empty || request.ConversationId == Guid.Empty)
            throw new ArgumentException("Invalid user or conversation ID.");

        var content = request.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content is required.");

        if (content.Length > 5000)
            throw new ArgumentException("Message content cannot exceed 5000 characters.");

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
        if (conversation == null)
            throw new KeyNotFoundException("Conversation not found.");

        var senderIsActive = conversation.Participants.Any(p => p.UserId == senderId && p.LeftAt == null);
        if (!senderIsActive)
            throw new UnauthorizedAccessException("You are not an active participant of this conversation.");

        var activeParticipants = conversation.Participants.Where(p => p.LeftAt == null).ToList();
        var now = DateTime.UtcNow;
        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = senderId,
            Content = content,
            Type = MessageType.Text,
            CreatedAt = now,
            UpdatedAt = now,
            IsEdited = false,
            Receipts = activeParticipants
                .Where(p => p.UserId != senderId)
                .Select(p => new MessageReceipt{ RecipientId = p.UserId })
                .ToList()
        };
        conversation.UpdatedAt = now;

        _conversationRepository.Update(conversation);
        await _messageRepository.AddAsync(message);
        await _messageRepository.SaveChangesAsync();

        return MapToDto(message);
    }

    public async Task<List<MessageResponseDto>> GetHistoryAsync(Guid userId, Guid conversationId)
    {
        if (userId == Guid.Empty || conversationId == Guid.Empty)
            throw new ArgumentException("Invalid user or conversation ID.");

        var isActiveParticipant =await _participantRepository.IsActiveParticipantAsync(conversationId, userId);
        if (!isActiveParticipant)
            throw new KeyNotFoundException("Conversation not found.");

        var messages = await _messageRepository.GetHistoryAsync(conversationId);

        return messages.Select(MapToDto).ToList();
    }

    public async Task EnsureActiveParticipantAsync(Guid userId, Guid conversationId)
    {
        if (userId == Guid.Empty || conversationId == Guid.Empty)
            throw new ArgumentException("Invalid user or conversation ID.");

        var isActiveParticipant = await _participantRepository.IsActiveParticipantAsync(conversationId, userId);
        if (!isActiveParticipant)
            throw new KeyNotFoundException("Conversation not found.");
    }

    public async Task<List<Guid>> GetActiveParticipantIdsAsync(Guid conversationId)
    {
        var participants = await _participantRepository.GetActiveByConversationAsync(conversationId);

        return participants.Select(p => p.UserId).ToList();
    }


    public async Task<MessageStatusResponseDto> MarkDeliveredAsync(Guid recipientId, Guid messageId)
    {
        if (recipientId == Guid.Empty || messageId == Guid.Empty)
            throw new ArgumentException("Invalid user or message ID.");

        var receipt = await _receiptRepository.GetByMessageAndRecipientAsync(messageId, recipientId);
        if (receipt == null)
            throw new KeyNotFoundException("Message receipt not found.");

        if (receipt.DeliveredAt == null)
        {
            receipt.DeliveredAt = DateTime.UtcNow;
            _receiptRepository.Update(receipt);

            await _receiptRepository.SaveChangesAsync();
        }

        return MapReceiptToDto(receipt);
    }

    public async Task<MessageStatusResponseDto> MarkReadAsync(Guid recipientId, Guid messageId)
    {
        if (recipientId == Guid.Empty || messageId == Guid.Empty)
            throw new ArgumentException("Invalid user or message ID.");

        var receipt = await _receiptRepository.GetByMessageAndRecipientAsync(messageId, recipientId);
        if (receipt == null)
            throw new KeyNotFoundException("Message receipt not found.");

        if (receipt.ReadAt == null)
        {
            var now = DateTime.UtcNow;

            receipt.DeliveredAt ??= now;
            receipt.ReadAt = now;

            _receiptRepository.Update(receipt);

            await _receiptRepository.SaveChangesAsync();
        }

        return MapReceiptToDto(receipt);
    }

    private static MessageStatusResponseDto MapReceiptToDto(MessageReceipt receipt)
    {
        return new MessageStatusResponseDto
        {
            MessageId = receipt.MessageId,
            RecipientId = receipt.RecipientId,
            SenderId = receipt.Message.SenderId,
            DeliveredAt = receipt.DeliveredAt,
            ReadAt = receipt.ReadAt
        };
    }

    private static MessageResponseDto MapToDto(Message message)
    {
        return new MessageResponseDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Content = message.Content,
            Type = message.Type.ToString(),
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            IsEdited = message.IsEdited,
            DeletedAt = message.DeletedAt,
            Receipts = message.Receipts.Select(r =>
                new MessageReceiptResponseDto
                {
                    RecipientId = r.RecipientId,
                    DeliveredAt = r.DeliveredAt,
                    ReadAt = r.ReadAt
                }).ToList()
        };
    }
}