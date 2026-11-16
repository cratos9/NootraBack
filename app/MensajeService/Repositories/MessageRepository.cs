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

    public async Task<List<Message>> GetHistoryAsync(Guid conversationId, int limit = 50)
    {
        var messages = await _context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .Include(m => m.Receipts)
            .Include(m => m.Attachments)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(limit)
            .ToListAsync();

        messages.Reverse();

        return messages;
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}