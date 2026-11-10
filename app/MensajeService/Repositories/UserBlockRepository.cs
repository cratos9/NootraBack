using MensajeService.Data;
using MensajeService.Models;
using Microsoft.EntityFrameworkCore;

namespace MensajeService.Repositories;

public class UserBlockRepository
{
    private readonly MessageDbContext _context;

    public UserBlockRepository(MessageDbContext context)
    {
        _context = context;
    }

    public async Task<UserBlock?> GetByIdAsync(Guid id)
    {
        return await _context.UserBlocks.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<UserBlock?> GetByUsersAsync(Guid blockerId, Guid blockedId)
    {
        return await _context.UserBlocks
            .FirstOrDefaultAsync(b =>
                b.BlockerId == blockerId &&
                b.BlockedId == blockedId);
    }

    public async Task<bool> HasBlockedAsync(Guid blockerId, Guid blockedId)
    {
        return await _context.UserBlocks.AnyAsync(b =>
                b.BlockerId == blockerId &&
                b.BlockedId == blockedId);
    }

    public async Task<bool> HasBlockBetweenUsersAsync(Guid firstUserId, Guid secondUserId)
    {
        return await _context.UserBlocks.AnyAsync(b =>
                (b.BlockerId == firstUserId &&
                 b.BlockedId == secondUserId) ||
                (b.BlockerId == secondUserId &&
                 b.BlockedId == firstUserId));
    }

    public async Task<List<UserBlock>> GetBlockedUsersAsync(Guid blockerId)
    {
        return await _context.UserBlocks
            .AsNoTracking()
            .Where(b => b.BlockerId == blockerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(UserBlock block)
    {
        await _context.UserBlocks.AddAsync(block);
    }

    public void Delete(UserBlock block)
    {
        _context.UserBlocks.Remove(block);
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
