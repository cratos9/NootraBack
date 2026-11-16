using MensajeService.DTOs.Contacts;
using MensajeService.Models;
using MensajeService.Repositories;

namespace MensajeService.Services;

public class ContactPreferencesService
{
    private readonly ContactPreferencesRepository _repository;

    public ContactPreferencesService(ContactPreferencesRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ContactPreferenceResponseDto>> GetActiveAsync(Guid ownerId)
    {
        ValidateOwnerId(ownerId);

        var preferences = await _repository.GetActiveByOwnerAsync(ownerId);

        return preferences.Select(MapToDto).ToList();
    }

    public async Task<List<ContactPreferenceResponseDto>> GetArchivedAsync(Guid ownerId)
    {
        ValidateOwnerId(ownerId);

        var preferences = await _repository.GetArchivedByOwnerAsync(ownerId);

        return preferences.Select(MapToDto).ToList();
    }

    public async Task<ContactPreferenceResponseDto> GetByContactAsync(Guid ownerId, Guid contactId)
    {
        await ValidateAcceptedContactAsync(ownerId, contactId);

        var preference = await _repository.GetByOwnerAndContactAsync(ownerId, contactId);
        if (preference is null)
        {
            return new ContactPreferenceResponseDto
            {
                ContactId = contactId,
                IsPinned = false,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        return MapToDto(preference);
    }

    public async Task<ContactPreferenceResponseDto> UpdatePinAsync(Guid ownerId, Guid contactId, bool isPinned)
    {
        await ValidateAcceptedContactAsync(ownerId, contactId);

        var preference = await GetOrCreatePreferenceAsync(ownerId, contactId);
        if (preference.IsPinned != isPinned)
        {
            preference.IsPinned = isPinned;
            preference.UpdatedAt = DateTime.UtcNow;

            _repository.Update(preference);
            await _repository.SaveChangesAsync();
        }

        return MapToDto(preference);
    }

    public async Task<ContactPreferenceResponseDto> UpdateArchiveAsync(Guid ownerId, Guid contactId, bool isArchived)
    {
        await ValidateAcceptedContactAsync(ownerId, contactId);

        var preference = await GetOrCreatePreferenceAsync(ownerId, contactId);
        if (preference.IsAttached != isArchived)
        {
            preference.IsAttached = isArchived;
            preference.UpdatedAt = DateTime.UtcNow;

            _repository.Update(preference);
            await _repository.SaveChangesAsync();
        }

        return MapToDto(preference);
    }

    private async Task<ContactPreference> GetOrCreatePreferenceAsync(Guid ownerId, Guid contactId)
    {
        var preference = await _repository.GetByOwnerAndContactAsync(ownerId, contactId);

        if (preference is not null)
            return preference;

        preference = new ContactPreference
        {
            OwnerId = ownerId,
            ContactId = contactId,
            IsPinned = false,
            IsAttached = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(preference);
        await _repository.SaveChangesAsync();

        return preference;
    }

    private async Task ValidateAcceptedContactAsync(Guid ownerId, Guid contactId)
    {
        ValidateOwnerId(ownerId);

        if (contactId == Guid.Empty)
            throw new ArgumentException("Invalid contact ID.");

        var isAccepted = await _repository.IsAcceptedContactAsync(ownerId, contactId);
        if (!isAccepted)
            throw new KeyNotFoundException("Accepted contact not found.");
    }

    private static void ValidateOwnerId(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Invalid owner ID.");
    }

    private static ContactPreferenceResponseDto MapToDto(ContactPreference preference)
    {
        return new ContactPreferenceResponseDto
        {
            ContactId = preference.ContactId,
            IsPinned = preference.IsPinned,
            IsArchived = preference.IsAttached,
            CreatedAt = preference.CreatedAt,
            UpdatedAt = preference.UpdatedAt
        };
    }
}
