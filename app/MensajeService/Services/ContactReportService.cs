using MensajeService.Models.Enums;
using MensajeService.DTOs.ContactReports;
using MensajeService.Models;
using MensajeService.Repositories;

namespace MensajeService.Services;

public class ContactReportService
{
    private readonly ContactReportRepository _repository;

    public ContactReportService(ContactReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContactReportResponseDto> CreateAsync(Guid reporterId, Guid reportedId, string reason, string? description)
    {
        ValidateUserIds(reporterId, reportedId);

        if (reporterId == reportedId)
            throw new ArgumentException("You cannot report yourself.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Report reason is required.");

        reason = reason.Trim();

        if (reason.Length > 100)
            throw new ArgumentException("Report reason cannot exceed 100 characters.");

        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        var report = new ContactReport
        {
            ReporterId = reporterId,
            ReportedId = reportedId,
            Reason = reason,
            Description = description,
            Status = ContactReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(report);
        await _repository.SaveChangesAsync();

        return MapToDto(report);
    }

    public async Task<List<ContactReportResponseDto>> GetMyReportsAsync(
        Guid reporterId)
    {
        ValidateReporterId(reporterId);

        var reports = await _repository.GetByReporterAsync(reporterId);

        return reports.Select(MapToDto).ToList();
    }

    public async Task<ContactReportResponseDto> GetByIdAsync(Guid reporterId, Guid reportId)
    {
        ValidateReporterId(reporterId);

        if (reportId == Guid.Empty)
            throw new ArgumentException("Invalid report ID.");

        var report = await _repository.GetByIdAsync(reportId, reporterId);
        if (report is null)
            throw new KeyNotFoundException("Report not found.");

        return MapToDto(report);
    }

    private static void ValidateUserIds(Guid reporterId, Guid reportedId)
    {
        ValidateReporterId(reporterId);

        if (reportedId == Guid.Empty)
            throw new ArgumentException("Invalid reported user ID.");
    }

    private static void ValidateReporterId(Guid reporterId)
    {
        if (reporterId == Guid.Empty)
            throw new ArgumentException("Invalid reporter ID.");
    }

    private static ContactReportResponseDto MapToDto(ContactReport report)
    {
        return new ContactReportResponseDto
        {
            Id = report.Id,
            ReportedId = report.ReportedId,
            Reason = report.Reason,
            Description = report.Description,
            Status = report.Status.ToString(),
            CreatedAt = report.CreatedAt
        };
    }
}