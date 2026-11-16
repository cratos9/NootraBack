
namespace MensajeService.DTOs.ContactReports;

public class ContactReportResponseDto
{
    public Guid Id { get; set; }

    public Guid ReportedId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
