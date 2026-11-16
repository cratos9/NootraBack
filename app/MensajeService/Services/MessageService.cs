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
    private readonly FileStorageService _fileStorageService;

    public MessageService(MessageRepository messageRepository, ConversationRepository conversationRepository, ConversationParticipantRepository participantRepository, MessageReceiptRepository receiptRepository, FileStorageService fileStorageService)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _receiptRepository = receiptRepository;
        _fileStorageService = fileStorageService;
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

    public async Task<MessageHistoryResponseDto> GetHistoryAsync(Guid userId, Guid conversationId, int limit = 50, DateTime? beforeCreatedAt = null, Guid? beforeMessageId = null)
    {
        if (userId == Guid.Empty || conversationId == Guid.Empty)
            throw new ArgumentException("Invalid user or conversation ID.");

        if (limit < 1 || limit > 100)
            throw new ArgumentException("Limit must be between 1 and 100.");

        if (beforeCreatedAt.HasValue != beforeMessageId.HasValue)
            throw new ArgumentException("Both beforeCreatedAt and beforeMessageId must be provided together.");

        if (beforeMessageId == Guid.Empty)
            throw new ArgumentException("Invalid cursor message ID.");

        var isActiveParticipant =await _participantRepository.IsActiveParticipantAsync(conversationId, userId);
        if (!isActiveParticipant)
            throw new KeyNotFoundException("Conversation not found.");

        var (messages, hasMore) = await _messageRepository.GetHistoryAsync(conversationId, limit, beforeCreatedAt, beforeMessageId);
        var response = new MessageHistoryResponseDto { Items = messages.Select(MapToDto).ToList(), HasMore = hasMore};
        if (hasMore && messages.Count > 0)
        {
            var oldestMessage = messages[0];
            response.NextCursor = new MessageCursorDto { CreatedAt = oldestMessage.CreatedAt, MessageId = oldestMessage.Id};
        }

        return response;
    }

    public async Task<MessageResponseDto> UpdateTextAsync(Guid senderId, Guid messageId, UpdateMessageRequestDto request)
    {
        if (senderId == Guid.Empty)
            throw new ArgumentException("Invalid sender ID.");

        if (messageId == Guid.Empty)
            throw new ArgumentException("Invalid message ID.");

        if (request is null)
            throw new ArgumentException("Request cannot be null.");

        var content = request.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty.");

        if (content.Length > 5000)
            throw new ArgumentException("Message cannot exceed 5000 characters.");

        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message is null)
            throw new KeyNotFoundException("Message not found.");

        await EnsureActiveParticipantAsync(senderId, message.ConversationId);

        if (message.SenderId != senderId)
            throw new UnauthorizedAccessException("You cannot edit this message.");

        if (message.Type != MessageType.Text)
            throw new InvalidOperationException("Only text messages can be edited.");

        if (message.DeletedAt is not null)
            throw new InvalidOperationException("Deleted messages cannot be edited.");

        message.Content = content;
        message.IsEdited = true;
        message.UpdatedAt = DateTime.UtcNow;

        await _messageRepository.SaveChangesAsync();

        return MapToDto(message);
    }

    public async Task<MessageResponseDto> DeleteAsync(Guid senderId, Guid messageId)
    {
        if (senderId == Guid.Empty)
            throw new ArgumentException("Invalid sender ID.");

        if (messageId == Guid.Empty)
            throw new ArgumentException("Invalid message ID.");

        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message is null)
            throw new KeyNotFoundException("Message not found.");

        await EnsureActiveParticipantAsync(senderId, message.ConversationId);

        if (message.SenderId != senderId)
            throw new UnauthorizedAccessException("You cannot delete this message.");

        if (message.DeletedAt is not null)
            throw new InvalidOperationException("Message has already been deleted.");

        message.Content = null;
        message.DeletedAt = DateTime.UtcNow;
        message.UpdatedAt = DateTime.UtcNow;

        await _messageRepository.SaveChangesAsync();

        return MapToDto(message);
    }


    public async Task<MessageResponseDto> SendAttachmentAsync(Guid senderId, Guid conversationId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (senderId == Guid.Empty)
            throw new ArgumentException("Invalid sender ID.");

        if (conversationId == Guid.Empty)
            throw new ArgumentException("Invalid conversation ID.");

        if (file is null || file.Length == 0)
            throw new ArgumentException("File cannot be empty.");

        var originalFilename = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalFilename).ToLowerInvariant();

        var allowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".pdf"] = "application/pdf",
            [".txt"] = "text/plain",
            [".csv"] = "text/csv",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation"
        };

        if (!allowedTypes.TryGetValue(extension, out var expectedMimeType))
            throw new ArgumentException("File extension is not allowed.");

        if (!string.Equals(file.ContentType, expectedMimeType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid file MIME type.");
        }

        var isImage = expectedMimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        var maxFileSize = isImage ? 10 * 1024 * 1024 : 25 * 1024 * 1024;
        if (file.Length > maxFileSize)
            throw new ArgumentException("File exceeds the allowed size.");

        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation is null)
            throw new KeyNotFoundException("Conversation not found.");

        await EnsureActiveParticipantAsync(senderId, conversationId);

        var participantIds = await GetActiveParticipantIdsAsync(conversationId);
        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = null,
            Type = isImage ? MessageType.Image : MessageType.File,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsEdited = false
        };

        foreach (var participantId in participantIds)
        {
            if (participantId == senderId)
                continue;

            message.Receipts.Add(new MessageReceipt{ MessageId = message.Id, RecipientId = participantId});
        }

        var storedFilename = $"{Guid.NewGuid():N}{extension}";
        var relativePath = await _fileStorageService.SaveAsync(file, storedFilename, cancellationToken);
        message.Attachments.Add(new MessageAttachment
        {
            MessageId = message.Id,
            OriginalFilename = originalFilename,
            StoredFilename = storedFilename,
            FilePath = relativePath,
            MimeType = expectedMimeType,
            FileSizeBytes = file.Length,
            CreatedAt = DateTime.UtcNow
        });

        conversation.UpdatedAt = DateTime.UtcNow;
        _conversationRepository.Update(conversation);

        try
        {
            await _messageRepository.AddAsync(message);
            await _messageRepository.SaveChangesAsync();
        }
        catch
        {
            try
            {
                await _fileStorageService.DeleteAsync(relativePath);
            }
            catch
            {
                // pass
            }
            throw;
        }

        return MapToDto(message);
    }

    public async Task<AttachmentDownloadResult> GetAttachmentForDownloadAsync(Guid userId, Guid attachmentId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Invalid user ID.");

        if (attachmentId == Guid.Empty)
            throw new ArgumentException("Invalid attachment ID.");

        var attachment = await _messageRepository.GetAttachmentByIdAsync(attachmentId);
        if (attachment is null || attachment.Message is null)
            throw new KeyNotFoundException("Attachment not found.");

        var message = attachment.Message;
        await EnsureActiveParticipantAsync(userId, message.ConversationId);

        if (message.DeletedAt is not null)
            throw new KeyNotFoundException("Attachment not found.");

        var stream = _fileStorageService.OpenRead(attachment.FilePath);
        return new AttachmentDownloadResult
        {
            Content = stream,
            FileName = Path.GetFileName(attachment.OriginalFilename),
            ContentType = attachment.MimeType
        };
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
                }).ToList(),
            Attachments = message.Attachments
                .Select(a => new MessageAttachmentResponseDto
                {
                    Id = a.Id,
                    OriginalFilename = a.OriginalFilename,
                    MimeType = a.MimeType,
                    FileSizeBytes = a.FileSizeBytes,
                    CreatedAt = a.CreatedAt
                })
                .ToList()
        };
    }
}