using MensajeService.Data;
using MensajeService.Models;
using Microsoft.EntityFrameworkCore;
using MensajeService.Models.Enums;

namespace MensajeService.Repositories;

public class ContactPreferencesRepository
{
    private readonly MessageDbContext _context;

    public ContactPreferencesRepository(MessageDbContext context)
    {
        _context = context;
    }
    public async Task<ContactPreference?> GetByOwnerAndContactAsync(Guid ownerId, Guid contactId)
    {
        return await _context.ContactPreferences.Include(p => p.Contact).FirstOrDefaultAsync(p =>
                p.OwnerId == ownerId &&
                p.ContactId == contactId);
    }

    public async Task<List<ContactPreference>> GetActiveByOwnerAsync(Guid ownerId)
    {
        return await _context.ContactPreferences.AsNoTracking().Include(p => p.Contact).Where(p =>
                p.OwnerId == ownerId &&
                !p.IsAttached)
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.UpdatedAt)
            .ToListAsync();
    }

    public async Task<List<ContactPreference>> GetArchivedByOwnerAsync(Guid ownerId)
    {
        return await _context.ContactPreferences.AsNoTracking().Include(p => p.Contact).Where(p =>
                p.OwnerId == ownerId &&
                p.IsAttached)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsAcceptedContactAsync(Guid ownerId, Guid contactId)
    {
        return await _context.Contacts.AnyAsync(c =>
            c.Id == contactId &&
            c.Status == ContactStatus.Accepted &&
            (c.RequesterId == ownerId || c.AddresseeId == ownerId));
    }

    public async Task AddAsync(ContactPreference preference)
    {
        await _context.ContactPreferences.AddAsync(preference);
    }

    public void Update(ContactPreference preference)
    {
        _context.ContactPreferences.Update(preference);
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}