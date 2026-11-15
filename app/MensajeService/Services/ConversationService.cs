using MensajeService.DTOs.Conversations;
using MensajeService.Models;
using MensajeService.Models.Enums;
using MensajeService.Repositories;

namespace MensajeService.Services;

public class ConversationService
{
    private readonly ConversationRepository _conversationRepository;
    private readonly ConversationParticipantRepository _participantRepository;
    private readonly ContactRepository _contactRepository;
    private readonly UserBlockRepository _userBlockRepository;

    public ConversationService( ConversationRepository conversationRepository, ConversationParticipantRepository participantRepository, ContactRepository contactRepository, UserBlockRepository userBlockRepository)
    {
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _contactRepository = contactRepository;
        _userBlockRepository = userBlockRepository;
    }

    public async Task<ConversationResponseDto> CreateDirectAsync(Guid currentUserId, Guid otherUserId)
    {
        if (currentUserId == Guid.Empty || otherUserId == Guid.Empty)
            throw new ArgumentException("User IDs are required.");

        if (currentUserId == otherUserId)
            throw new ArgumentException("You cannot create a conversation with yourself.");

        var hasBlock = await _userBlockRepository.HasBlockBetweenUsersAsync(currentUserId, otherUserId);
        if (hasBlock)
            throw new InvalidOperationException("A conversation cannot be created because one user has blocked the other.");

        var contact = await _contactRepository.GetByUsersAsync(currentUserId, otherUserId);
        if (contact == null || contact.Status != ContactStatus.Accepted)
            throw new InvalidOperationException("You can only start conversations with accepted contacts.");

        var userLowId = currentUserId.CompareTo(otherUserId) < 0 ? currentUserId : otherUserId;
        var userHighId = currentUserId.CompareTo(otherUserId) < 0 ? otherUserId : currentUserId;
        var directKey = $"{userLowId:D}:{userHighId:D}";

        var existingConversation = await _conversationRepository.GetByDirectKeyAsync(directKey);
        if (existingConversation != null)
        {
            foreach (var userId in new[] { currentUserId, otherUserId })
            {
                var participant = existingConversation.Participants.FirstOrDefault(p => p.UserId == userId);
                if (participant == null)
                {
                    await _participantRepository.AddAsync( new ConversationParticipant {ConversationId = existingConversation.Id, UserId = userId});
                    continue;
                }

                if (participant.LeftAt != null)
                {
                    participant.LeftAt = null;
                    participant.JoinedAt = DateTime.UtcNow;
                    _participantRepository.Update(participant);
                }
            }

            existingConversation.UpdatedAt = DateTime.UtcNow;
            _conversationRepository.Update(existingConversation);

            await _conversationRepository.SaveChangesAsync();

            var refreshedConversation = await _conversationRepository.GetByIdAsync(existingConversation.Id);

            return MapToDto(refreshedConversation!);
        }

        var conversation = new Conversation
        {
            Type = ConversationType.Direct,
            DirectKey = directKey,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = currentUserId },
                new() { UserId = otherUserId }
            }
        };

        await _conversationRepository.AddAsync(conversation);
        await _conversationRepository.SaveChangesAsync();

        return MapToDto(conversation);
    }

    public async Task<List<ConversationResponseDto>> GetMyConversationsAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.");

        var conversations = await _conversationRepository.GetByUserAsync(userId);

        return conversations.Select(MapToDto).ToList();
    }

    public async Task<ConversationResponseDto> GetByIdAsync(Guid userId, Guid conversationId)
    {
        await ValidateActiveParticipantAsync(userId, conversationId);

        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
            throw new KeyNotFoundException("Conversation not found.");

        return MapToDto(conversation);
    }

    public async Task<List<ConversationParticipantResponseDto>>GetParticipantsAsync(Guid userId, Guid conversationId)
    {
        await ValidateActiveParticipantAsync(userId, conversationId);

        var participants = await _participantRepository.GetActiveByConversationAsync(conversationId);
        return participants.Select(p => new ConversationParticipantResponseDto{ UserId = p.UserId, JoinedAt = p.JoinedAt}).ToList();
    }

    public async Task DeleteForUserAsync(Guid userId, Guid conversationId)
    {
        if (userId == Guid.Empty || conversationId == Guid.Empty)
            throw new ArgumentException("IDs are required.");

        var participant = await _participantRepository.GetByConversationAndUserAsync(conversationId, userId);
        if (participant == null || participant.LeftAt != null)
            throw new KeyNotFoundException("Conversation not found.");

        participant.LeftAt = DateTime.UtcNow;

        _participantRepository.Update(participant);
        await _participantRepository.SaveChangesAsync();
    }

    private async Task ValidateActiveParticipantAsync(Guid userId, Guid conversationId)
    {
        if (userId == Guid.Empty || conversationId == Guid.Empty)
            throw new ArgumentException("IDs are required.");

        var isParticipant = await _participantRepository.IsActiveParticipantAsync(conversationId, userId);
        if (!isParticipant)
            throw new KeyNotFoundException("Conversation not found.");
    }

    private static ConversationResponseDto MapToDto(
        Conversation conversation)
    {
        return new ConversationResponseDto
        {
            Id = conversation.Id,
            Type = conversation.Type.ToString(),
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Participants = conversation.Participants
                .Where(p => p.LeftAt == null)
                .Select(p => new ConversationParticipantResponseDto{ UserId = p.UserId, JoinedAt = p.JoinedAt}).ToList()
        };
    }
}