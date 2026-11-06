using MensajeService.Models.Enums;

namespace MensajeService.Models;

public class ContactReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ReporterId { get; set; }

    public Guid ReportedId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ContactReportStatus Status { get; set; } = ContactReportStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}