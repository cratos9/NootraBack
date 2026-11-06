using Microsoft.EntityFrameworkCore;

namespace MensajeService.Data;

public class MessageDbContext : DbContext
{
    public MessageDbContext(DbContextOptions<MessageDbContext> options) : base(options)
    {
    }
}