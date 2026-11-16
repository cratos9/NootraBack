using MensajeService.DTOs.ContactReports;
using MensajeService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MensajeService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactReportController : ControllerBase
{
    private readonly ContactReportService _service;
    private readonly ILogger<ContactReportController> _logger;

    public ContactReportController(ContactReportService service, ILogger<ContactReportController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContactReportRequestDto request)
    {
        try
        {
            var reporterId = GetCurrentUserId();
            var report = await _service.CreateAsync(reporterId, request.ReportedId, request.Reason, request.Description);

            return CreatedAtAction(nameof(GetById), new { id = report.Id }, report);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact report.");

            return StatusCode(500, new {message = "An unexpected error occurred."});
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyReports()
    {
        try
        {
            var reporterId = GetCurrentUserId();
            var reports = await _service.GetMyReportsAsync(reporterId);

            return Ok(reports);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contact reports.");

            return StatusCode(500, new {message = "An unexpected error occurred."});
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var reporterId = GetCurrentUserId();
            var report = await _service.GetByIdAsync(reporterId, id);

            return Ok(report);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contact report.");

            return StatusCode(500, new {message = "An unexpected error occurred."});
        }
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue("sub");

        if (!Guid.TryParse(userId, out var parsedUserId))
            throw new UnauthorizedAccessException("Invalid authenticated user.");

        return parsedUserId;
    }
}