using MensajeService.Data;
using MensajeService.Models;
using Microsoft.EntityFrameworkCore;

namespace MensajeService.Repositories;

public class ContactReportRepository
{
    private readonly MessageDbContext _context;

    public ContactReportRepository(MessageDbContext context)
    {
        _context = context;
    }

    public async Task<ContactReport?> GetByIdAsync(Guid id, Guid reporterId)
    {
        return await _context.ContactReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.ReporterId == reporterId);
    }

    public async Task<List<ContactReport>> GetByReporterAsync(Guid reporterId)
    {
        return await _context.ContactReports
            .AsNoTracking()
            .Where(r => r.ReporterId == reporterId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(ContactReport report)
    {
        await _context.ContactReports.AddAsync(report);
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
