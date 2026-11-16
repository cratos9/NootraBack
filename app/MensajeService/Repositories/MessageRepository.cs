using MensajeService.Data;
using MensajeService.Models;
using Microsoft.EntityFrameworkCore;

namespace MensajeService.Repositories;

public class MessageRepository
{
    private readonly MessageDbContext _context;

    public MessageRepository(MessageDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Message message)
    {
        await _context.Messages.AddAsync(message);
    }

    public async Task<Message?> GetByIdAsync(Guid messageId)
    {
        return await _context.Messages.Include(m => m.Receipts).FirstOrDefaultAsync(m => m.Id == messageId);
    }

    public async Task<MessageAttachment?> GetAttachmentByIdAsync(Guid attachmentId)
    {
        return await _context.MessageAttachments.Include(a => a.Message).FirstOrDefaultAsync(a => a.Id == attachmentId);
    }

    public async Task<(List<Message> Messages, bool HasMore)> GetHistoryAsync(Guid conversationId, int limit, DateTime? beforeCreatedAt = null, Guid? beforeMessageId = null)
    {
        var query = _context.Messages.AsNoTracking().Where(m => m.ConversationId == conversationId);
        if (beforeCreatedAt.HasValue && beforeMessageId.HasValue)
        {
            var cursorDate = beforeCreatedAt.Value;
            var cursorId = beforeMessageId.Value;

            query = query.Where(m => m.CreatedAt < cursorDate || (m.CreatedAt == cursorDate && m.Id.CompareTo(cursorId) < 0));
        }

        var messages = await query
            .Include(m => m.Receipts)
            .Include(m => m.Attachments)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(limit + 1)
            .ToListAsync();

        var hasMore = messages.Count > limit;

        if (hasMore)
            messages.RemoveAt(messages.Count - 1);

        messages.Reverse();

        return (messages, hasMore);
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}