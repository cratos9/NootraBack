using MensajeService.DTOs.Contacts;
using MensajeService.Models;
using MensajeService.Models.Enums;
using MensajeService.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MensajeService.Services;

public class ContactServices
{
    private readonly ContactRepository _repository;

    public ContactServices(ContactRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContactResponseDto> SendRequestAsync(Guid requesterId, Guid addresseeId)
    {
        if (requesterId == Guid.Empty)
            throw new ArgumentException("The requester ID is not valid.");

        if (addresseeId == Guid.Empty)
            throw new ArgumentException("The addressee ID is not valid.");

        if (requesterId == addresseeId)
            throw new InvalidOperationException(
                "You cannot send a request to yourself.");

        if (await _repository.HasBlockBetweenUsersAsync(
            requesterId, addresseeId))
        {
            throw new InvalidOperationException("You cannot send a request due to a block between users.");
        }

        var existingContact = await _repository.GetByUsersAsync(requesterId, addresseeId);
        if (existingContact != null)
        {
            if (existingContact.Status == ContactStatus.Pending)
                throw new InvalidOperationException("A pending request already exists between these users.");

            if (existingContact.Status == ContactStatus.Accepted)
                throw new InvalidOperationException("These users are already contacts.");

            if (existingContact.Status == ContactStatus.Rejected || existingContact.Status == ContactStatus.Cancelled)
            {
                var reactivated = await _repository.TryReactivateAsync(existingContact.Id, requesterId, addresseeId);
                if (!reactivated)
                    throw new InvalidOperationException("The request was modified by another operation. Please try again.");

                existingContact.RequesterId = requesterId;
                existingContact.AddresseeId = addresseeId;
                existingContact.Status = ContactStatus.Pending;
                existingContact.CreatedAt = DateTime.UtcNow;
                existingContact.UpdatedAt = existingContact.CreatedAt;

                return MapToDto(existingContact);
            }
        }

        var now = DateTime.UtcNow;
        var contact = new Contact
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            UserLowId = requesterId.CompareTo(addresseeId) < 0 ? requesterId : addresseeId,
            UserHighId = requesterId.CompareTo(addresseeId) < 0 ? addresseeId : requesterId,
            Status = ContactStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddAsync(contact);

        try
        {
            await _repository.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            if (exception.InnerException is PostgresException postgresException && postgresException.SqlState == PostgresErrorCodes.UniqueViolation && postgresException.ConstraintName == "uq_contacts_user_pair")
            {
                throw new InvalidOperationException("A relationship already exists between these users.");
            }
            throw;
        }

        return MapToDto(contact);
    }

    public async Task<List<ContactResponseDto>> GetReceivedRequestsAsync(Guid userId)
    {
        var contacts = await _repository.GetReceivedRequestsAsync(userId);

        return contacts.Select(MapToDto).ToList();
    }

    public async Task<List<ContactResponseDto>> GetSentRequestsAsync(Guid userId)
    {
        var contacts = await _repository.GetSentRequestsAsync(userId);

        return contacts.Select(MapToDto).ToList();
    }

    public async Task<ContactResponseDto> AcceptRequestAsync(Guid userId, Guid contactId)
    {
        var contact = await GetContactOrThrowAsync(contactId);
        if (contact.AddresseeId != userId)
            throw new UnauthorizedAccessException("You do not have permission to accept this request.");

        if (contact.Status != ContactStatus.Pending)
            throw new InvalidOperationException("The request is no longer pending.");

        if (await _repository.HasBlockBetweenUsersAsync(contact.RequesterId, contact.AddresseeId))
        {
            throw new InvalidOperationException("You cannot accept this request due to a block between users.");
        }

        contact.Status = ContactStatus.Accepted;
        contact.UpdatedAt = DateTime.UtcNow;

        _repository.Update(contact);

        await _repository.SaveChangesAsync();

        return MapToDto(contact);
    }

    public async Task<ContactResponseDto> RejectRequestAsync(Guid userId, Guid contactId)
    {
        var contact = await GetContactOrThrowAsync(contactId);
        if (contact.AddresseeId != userId)
            throw new UnauthorizedAccessException("You do not have permission to reject this request.");

        if (contact.Status != ContactStatus.Pending)
            throw new InvalidOperationException("The request is no longer pending.");

        contact.Status = ContactStatus.Rejected;
        contact.UpdatedAt = DateTime.UtcNow;

        _repository.Update(contact);

        await _repository.SaveChangesAsync();

        return MapToDto(contact);
    }

    public async Task<ContactResponseDto> CancelRequestAsync(Guid userId, Guid contactId)
    {
        var contact = await GetContactOrThrowAsync(contactId);
        if (contact.RequesterId != userId)
            throw new UnauthorizedAccessException("You do not have permission to cancel this request.");

        if (contact.Status != ContactStatus.Pending)
            throw new InvalidOperationException("The request is no longer pending.");

        contact.Status = ContactStatus.Cancelled;
        contact.UpdatedAt = DateTime.UtcNow;

        _repository.Update(contact);

        await _repository.SaveChangesAsync();

        return MapToDto(contact);
    }

    public async Task<List<ContactResponseDto>> GetContactsAsync(Guid userId)
    {
        var contacts = await _repository.GetAcceptedContactsAsync(userId);

        return contacts.Select(MapToDto).ToList();
    }

    public async Task RemoveContactAsync(Guid userId, Guid contactId)
    {
        var contact = await GetContactOrThrowAsync(contactId);
        if (contact.RequesterId != userId && contact.AddresseeId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete this relationship.");
        }

        if (contact.Status != ContactStatus.Accepted)
            throw new InvalidOperationException("You can only delete contacts that have been accepted.");

        _repository.Delete(contact);

        await _repository.SaveChangesAsync();
    }

    private async Task<Contact> GetContactOrThrowAsync(Guid contactId)
    {
        var contact = await _repository.GetByIdAsync(contactId);
        if (contact == null)
            throw new KeyNotFoundException("Do not found the contact relationship.");

        return contact;
    }

    private static ContactResponseDto MapToDto(Contact contact)
    {
        return new ContactResponseDto
        {
            Id = contact.Id,
            RequesterId = contact.RequesterId,
            AddresseeId = contact.AddresseeId,
            Status = contact.Status.ToString(),
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt
        };
    }
}