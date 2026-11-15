using MensajeService.Data;
using MensajeService.Models;
using Microsoft.EntityFrameworkCore;

namespace MensajeService.Repositories;

public class MessageReceiptRepository
{
    private readonly MessageDbContext _context;

    public MessageReceiptRepository(MessageDbContext context)
    {
        _context = context;
    }

    public async Task<MessageReceipt?> GetByMessageAndRecipientAsync(Guid messageId, Guid recipientId)
    {
        return await _context.MessageReceipts
            .Include(r => r.Message)
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.RecipientId == recipientId);
    }

    public void Update(MessageReceipt receipt)
    {
        _context.MessageReceipts.Update(receipt);
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}