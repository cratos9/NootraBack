namespace MensajeService.DTOs.ContactReports;

public class CreateContactReportRequestDto
{
    public Guid ReportedId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Description { get; set; }
}