using MensajeService.Data;
using MensajeService.Models;
using MensajeService.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MensajeService.Repositories;

public class ContactRepository
{
    private readonly MessageDbContext _context;

    public ContactRepository(MessageDbContext context)
    {
        _context = context;
    }

    public async Task<Contact?> GetByIdAsync(Guid contactId)
    {
        return await _context.Contacts.FirstOrDefaultAsync(c => c.Id == contactId);
    }

    public async Task<Contact?> GetByUsersAsync(Guid userOneId, Guid userTwoId)
    {
        var userLowId = userOneId.CompareTo(userTwoId) < 0 ? userOneId : userTwoId;
        var userHighId = userOneId.CompareTo(userTwoId) < 0 ? userTwoId : userOneId;
        return await _context.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.UserLowId == userLowId &&
                c.UserHighId == userHighId);
    }

    public async Task<List<Contact>> GetReceivedRequestsAsync(Guid userId)
    {
        return await _context.Contacts.Where(c =>
                c.AddresseeId == userId &&
                c.Status == ContactStatus.Pending)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Contact>> GetSentRequestsAsync(Guid userId)
    {
        return await _context.Contacts.Where(c =>
                c.RequesterId == userId &&
                c.Status == ContactStatus.Pending)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Contact>> GetAcceptedContactsAsync(Guid userId)
    {
        return await _context.Contacts.Where(c =>
                c.Status == ContactStatus.Accepted &&
                (c.RequesterId == userId || c.AddresseeId == userId))
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasBlockBetweenUsersAsync(Guid userOneId, Guid userTwoId)
    {
        return await _context.UserBlocks.AnyAsync(b =>
            (b.BlockerId == userOneId && b.BlockedId == userTwoId) ||
            (b.BlockerId == userTwoId && b.BlockedId == userOneId));
    }

    public async Task<bool> TryReactivateAsync(Guid contactId, Guid requesterId, Guid addresseeId)
    {
        var now = DateTime.UtcNow;

        var affectedRows = await _context.Contacts.Where(c =>
                c.Id == contactId &&
                (c.Status == ContactStatus.Rejected ||
                 c.Status == ContactStatus.Cancelled))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.RequesterId, requesterId)
                .SetProperty(c => c.AddresseeId, addresseeId)
                .SetProperty(c => c.Status, ContactStatus.Pending)
                .SetProperty(c => c.CreatedAt, now)
                .SetProperty(c => c.UpdatedAt, now));

        return affectedRows == 1;
    }

    public async Task AddAsync(Contact contact)
    {
        await _context.Contacts.AddAsync(contact);
    }

    public void Update(Contact contact)
    {
        _context.Contacts.Update(contact);
    }

    public void Delete(Contact contact)
    {
        _context.Contacts.Remove(contact);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}