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

    public ConversationService(ConversationRepository conversationRepository, ConversationParticipantRepository participantRepository, ContactRepository contactRepository, UserBlockRepository userBlockRepository)
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
                    await _participantRepository.AddAsync(
                        new ConversationParticipant
                        {
                            ConversationId = existingConversation.Id,
                            UserId = userId
                        });

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

    public async Task<ConversationResponseDto> CreateGroupAsync(Guid creatorId, CreateGroupRequestDto request)
    {
        if (creatorId == Guid.Empty)
            throw new ArgumentException("Creator ID is required.");

        if (request == null)
            throw new ArgumentException("Request is required.");

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required.");

        if (name.Length > 100)
            throw new ArgumentException("Group name cannot exceed 100 characters.");

        var description = request.Description?.Trim();
        if (description?.Length > 500)
            throw new ArgumentException("Group description cannot exceed 500 characters.");

        var participantIds = request.ParticipantIds.Distinct().ToList();
        if (participantIds.Count == 0)
            throw new ArgumentException("A group must have at least one participant besides its creator.");

        if (participantIds.Contains(Guid.Empty))
            throw new ArgumentException("Participant IDs cannot be empty.");

        if (participantIds.Contains(creatorId))
            throw new ArgumentException("The creator is automatically added to the group.");

        foreach (var participantId in participantIds)
        {
            await ValidateAcceptedContactAsync(creatorId, participantId);
        }

        var now = DateTime.UtcNow;
        var conversation = new Conversation
        {
            Type = ConversationType.Group,
            DirectKey = null,
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            CreatedBy = creatorId,
            CreatedAt = now,
            UpdatedAt = now,
            Participants = new List<ConversationParticipant>
            {
                new()
                {
                    UserId = creatorId,
                    Role = ConversationParticipantRole.Admin,
                    JoinedAt = now
                }
            }
        };

        foreach (var participantId in participantIds)
        {
            conversation.Participants.Add(
                new ConversationParticipant
                {
                    UserId = participantId,
                    Role = ConversationParticipantRole.Member,
                    JoinedAt = now
                });
        }

        await _conversationRepository.AddAsync(conversation);
        await _conversationRepository.SaveChangesAsync();

        return MapToDto(conversation);
    }

    public async Task<ConversationResponseDto> UpdateGroupAsync(Guid userId, Guid conversationId, UpdateGroupRequestDto request)
    {
        if (request == null)
            throw new ArgumentException("Request is required.");

        var conversation = await ValidateGroupAdminAsync(userId, conversationId);
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name is required.");

        if (name.Length > 100)
            throw new ArgumentException("Group name cannot exceed 100 characters.");

        var description = request.Description?.Trim();
        if (description?.Length > 500)
            throw new ArgumentException("Group description cannot exceed 500 characters.");

        conversation.Name = name;
        conversation.Description = string.IsNullOrWhiteSpace(description) ? null : description;
        conversation.UpdatedAt = DateTime.UtcNow;
        _conversationRepository.Update(conversation);
        await _conversationRepository.SaveChangesAsync();

        return MapToDto(conversation);
    }

    public async Task<ConversationResponseDto> AddParticipantsAsync(Guid adminId, Guid conversationId, AddGroupParticipantsRequestDto request)
    {
        if (request == null)
            throw new ArgumentException("Request is required.");

        var conversation = await ValidateGroupAdminAsync(adminId, conversationId);
        var participantIds = request.ParticipantIds.Distinct().ToList();
        if (participantIds.Count == 0)
            throw new ArgumentException("At least one participant is required.");

        if (participantIds.Contains(Guid.Empty))
            throw new ArgumentException("Participant IDs cannot be empty.");

        if (participantIds.Contains(adminId))
            throw new ArgumentException("You are already a participant in this group.");

        foreach (var participantId in participantIds)
        {
            await ValidateAcceptedContactAsync(adminId, participantId);

            var participant = await _participantRepository.GetByConversationAndUserAsync(conversationId, participantId);
            if (participant == null)
            {
                await _participantRepository.AddAsync(
                    new ConversationParticipant
                    {
                        ConversationId = conversationId,
                        UserId = participantId,
                        Role = ConversationParticipantRole.Member,
                        JoinedAt = DateTime.UtcNow
                    });

                continue;
            }

            if (participant.LeftAt == null)
                throw new InvalidOperationException($"User {participantId} is already a group participant.");

            participant.LeftAt = null;
            participant.JoinedAt = DateTime.UtcNow;
            participant.Role = ConversationParticipantRole.Member;
            _participantRepository.Update(participant);
        }

        conversation.UpdatedAt = DateTime.UtcNow;
        _conversationRepository.Update(conversation);
        await _conversationRepository.SaveChangesAsync();

        var refreshedConversation = await _conversationRepository.GetByIdAsync(conversationId);

        return MapToDto(refreshedConversation!);
    }

    public async Task RemoveParticipantAsync(Guid adminId, Guid conversationId, Guid participantId)
    {
        if (participantId == Guid.Empty)
            throw new ArgumentException("Participant ID is required.");

        if (adminId == participantId)
            throw new ArgumentException("Use the leave group operation to remove yourself.");

        var conversation = await ValidateGroupAdminAsync(adminId, conversationId);
        var participant = await _participantRepository.GetByConversationAndUserAsync(conversationId, participantId);
        if (participant == null || participant.LeftAt != null)
            throw new KeyNotFoundException("Participant not found.");

        if (participant.Role == ConversationParticipantRole.Admin)
        {
            await EnsureAnotherAdminExistsAsync(conversationId, participantId);
        }

        participant.LeftAt = DateTime.UtcNow;
        _participantRepository.Update(participant);
        conversation.UpdatedAt = DateTime.UtcNow;
        _conversationRepository.Update(conversation);
        await _conversationRepository.SaveChangesAsync();
    }

    public async Task UpdateParticipantRoleAsync(Guid adminId, Guid conversationId, Guid participantId, UpdateParticipantRoleRequestDto request)
    {
        if (participantId == Guid.Empty)
            throw new ArgumentException("Participant ID is required.");

        if (request == null || !Enum.TryParse<ConversationParticipantRole>(request.Role, true, out var newRole) || !Enum.IsDefined(typeof(ConversationParticipantRole), newRole))
        {
            throw new ArgumentException("Role must be either Admin or Member.");
        }

        var conversation = await ValidateGroupAdminAsync(adminId, conversationId);
        var participant = await _participantRepository.GetByConversationAndUserAsync(conversationId, participantId);
        if (participant == null || participant.LeftAt != null)
            throw new KeyNotFoundException("Participant not found.");

        if (participant.Role == newRole)
            return;

        if (participant.Role == ConversationParticipantRole.Admin && newRole == ConversationParticipantRole.Member)
        {
            await EnsureAnotherAdminExistsAsync(conversationId, participantId);
        }

        participant.Role = newRole;
        _participantRepository.Update(participant);
        conversation.UpdatedAt = DateTime.UtcNow;
        _conversationRepository.Update(conversation);
        await _conversationRepository.SaveChangesAsync();
    }

    public async Task LeaveGroupAsync(Guid userId, Guid conversationId)
    {
        if (userId == Guid.Empty || conversationId == Guid.Empty)
            throw new ArgumentException("IDs are required.");

        var conversation = await GetGroupAsync(conversationId);
        var participant = await _participantRepository.GetByConversationAndUserAsync(conversationId, userId);
        if (participant == null || participant.LeftAt != null)
            throw new KeyNotFoundException("Group not found.");

        if (participant.Role == ConversationParticipantRole.Admin)
        {
            await EnsureAnotherAdminExistsAsync(conversationId, userId);
        }

        participant.LeftAt = DateTime.UtcNow;
        _participantRepository.Update(participant);
        conversation.UpdatedAt = DateTime.UtcNow;
        _conversationRepository.Update(conversation);
        await _conversationRepository.SaveChangesAsync();
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
        return participants.Select(p =>
            new ConversationParticipantResponseDto
            {
                UserId = p.UserId,
                JoinedAt = p.JoinedAt,
                Role = p.Role.ToString()
            }).ToList();
    }

    public async Task DeleteForUserAsync(Guid userId, Guid conversationId)
    {
        if (userId == Guid.Empty || conversationId == Guid.Empty)
            throw new ArgumentException("IDs are required.");

        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
            throw new KeyNotFoundException("Conversation not found.");

        if (conversation.Type == ConversationType.Group)
        {
            await LeaveGroupAsync(userId, conversationId);
            return;
        }

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

        var isParticipant = await _participantRepository.IsActiveParticipantAsync( conversationId, userId);
        if (!isParticipant)
            throw new KeyNotFoundException("Conversation not found.");
    }

    private async Task<Conversation> GetGroupAsync(Guid conversationId)
    {
        if (conversationId == Guid.Empty)
            throw new ArgumentException("Conversation ID is required.");

        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
            throw new KeyNotFoundException("Group not found.");

        if (conversation.Type != ConversationType.Group)
            throw new ArgumentException("This operation is only available for group conversations.");

        return conversation;
    }

    private async Task<Conversation> ValidateGroupAdminAsync(Guid userId, Guid conversationId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.");

        var conversation = await GetGroupAsync(conversationId);
        var participant = await _participantRepository.GetByConversationAndUserAsync(conversationId, userId);
        if (participant == null || participant.LeftAt != null)
            throw new KeyNotFoundException("Group not found.");

        if (participant.Role != ConversationParticipantRole.Admin)
            throw new UnauthorizedAccessException("Only group administrators can perform this operation.");

        return conversation;
    }

    private async Task ValidateAcceptedContactAsync(Guid currentUserId, Guid otherUserId)
    {
        if (otherUserId == Guid.Empty)
            throw new ArgumentException("Participant ID is required.");

        if (currentUserId == otherUserId)
            throw new ArgumentException("You cannot add yourself as a participant.");

        var hasBlock = await _userBlockRepository.HasBlockBetweenUsersAsync(currentUserId, otherUserId);
        if (hasBlock)
            throw new InvalidOperationException("A participant cannot be added because one user has blocked the other.");

        var contact = await _contactRepository.GetByUsersAsync(currentUserId, otherUserId);
        if (contact == null || contact.Status != ContactStatus.Accepted)
            throw new InvalidOperationException("You can only add accepted contacts.");
    }

    private async Task EnsureAnotherAdminExistsAsync(Guid conversationId, Guid adminToExclude)
    {
        var participants = await _participantRepository.GetActiveByConversationAsync(conversationId);
        var anotherAdminExists = participants.Any(p => p.UserId != adminToExclude && p.Role == ConversationParticipantRole.Admin);
        if (!anotherAdminExists)
        {
            throw new InvalidOperationException(
                "The group must have at least one active administrator. Promote another participant before leaving or removing this administrator.");
        }
    }

    private static ConversationResponseDto MapToDto(Conversation conversation)
    {
        return new ConversationResponseDto
        {
            Id = conversation.Id,
            Type = conversation.Type.ToString(),
            Name = conversation.Name,
            Description = conversation.Description,
            CreatedBy = conversation.CreatedBy,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Participants = conversation.Participants
                .Where(p => p.LeftAt == null)
                .Select(p => new ConversationParticipantResponseDto
                {
                    UserId = p.UserId,
                    JoinedAt = p.JoinedAt,
                    Role = p.Role.ToString()
                })
                .ToList()
        };
    }
}