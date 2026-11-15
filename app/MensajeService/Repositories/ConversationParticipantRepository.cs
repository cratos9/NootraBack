using MensajeService.Data;
using MensajeService.Models;
using Microsoft.EntityFrameworkCore;

namespace MensajeService.Repositories;

public class ConversationParticipantRepository
{
    private readonly MessageDbContext _context;

    public ConversationParticipantRepository(MessageDbContext context)
    {
        _context = context;
    }

    public async Task<ConversationParticipant?> GetByConversationAndUserAsync(Guid conversationId, Guid userId)
    {
        return await _context.ConversationParticipants
            .FirstOrDefaultAsync(p =>p.ConversationId == conversationId && p.UserId == userId);
    }

    public async Task<List<ConversationParticipant>> GetActiveByConversationAsync(
        Guid conversationId)
    {
        return await _context.ConversationParticipants
            .AsNoTracking()
            .Where(p => p.ConversationId == conversationId && p.LeftAt == null)
            .OrderBy(p => p.JoinedAt)
            .ToListAsync();
    }

    public async Task<bool> IsActiveParticipantAsync(Guid conversationId, Guid userId)
    {
        return await _context.ConversationParticipants
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId && p.LeftAt == null);
    }

    public async Task AddAsync(ConversationParticipant participant)
    {
        await _context.ConversationParticipants.AddAsync(participant);
    }

    public async Task AddRangeAsync(IEnumerable<ConversationParticipant> participants)
    {
        await _context.ConversationParticipants.AddRangeAsync(participants);
    }

    public void Update(ConversationParticipant participant)
    {
        _context.ConversationParticipants.Update(participant);
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public void Delete(ConversationParticipant participant)
    {
        _context.ConversationParticipants.Remove(participant);
    }
}