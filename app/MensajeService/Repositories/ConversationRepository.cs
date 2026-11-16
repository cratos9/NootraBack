
using MensajeService.Data;
using MensajeService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MensajeService.Repositories;

public class ConversationRepository
{
    private readonly MessageDbContext _context;

    public ConversationRepository(MessageDbContext context)
    {
        _context = context;
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _context.Database.BeginTransactionAsync();
    }

    public async Task AcquireDirectKeyLockAsync(string directKey)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({directKey}, 0))"
        );
    }

    public async Task<Conversation?> GetByIdForUpdateAsync(Guid conversationId)
    {
        var conversations = await _context.Conversations
            .FromSqlInterpolated($"SELECT * FROM conversations WHERE id = {conversationId} FOR UPDATE")
            .ToListAsync();

        return conversations.FirstOrDefault();
    }

    public async Task<Conversation?> GetByIdAsync(Guid conversationId)
    {
        return await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId);
    }

    public async Task<Conversation?> GetByDirectKeyAsync(string directKey)
    {
        return await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.DirectKey == directKey);
    }

    public async Task<List<Conversation>> GetByUserAsync(Guid userId)
    {
        return await _context.Conversations
            .AsNoTracking()
            .Where(c => c.Participants.Any(p => p.UserId == userId && p.LeftAt == null))
            .Include(c => c.Participants.Where(p => p.LeftAt == null))
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Conversation conversation)
    {
        await _context.Conversations.AddAsync(conversation);
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public void Update(Conversation conversation)
    {
        _context.Conversations.Update(conversation);
    }

    public void Delete(Conversation conversation)
    {
        _context.Conversations.Remove(conversation);
    }
}