using MensajeService.DTOs.Contacts;
using MensajeService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; 
using System.Security.Claims;

namespace MensajeService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactPreferencesController : ControllerBase
{
    private readonly ContactPreferencesService _service;
    private readonly ILogger<ContactPreferencesController> _logger;

    public ContactPreferencesController(ContactPreferencesService service, ILogger<ContactPreferencesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var ownerId = GetCurrentUserId();
            var preferences = await _service.GetActiveAsync(ownerId);

            return Ok(preferences);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active contact preferences.");

            return StatusCode(500, new {message = "An unexpected error occurred."});
        }
    }

    [HttpGet("archived")]
    public async Task<IActionResult> GetArchived()
    {
        try
        {
            var ownerId = GetCurrentUserId();
            var preferences = await _service.GetArchivedAsync(ownerId);

            return Ok(preferences);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving archived contact preferences.");

            return StatusCode(500, new {message = "An unexpected error occurred."});
        }
    }

    [HttpGet("{contactId:guid}")]
    public async Task<IActionResult> GetByContact(Guid contactId)
    {
        try
        {
            var ownerId = GetCurrentUserId();
            var preference = await _service.GetByContactAsync(ownerId, contactId);

            return Ok(preference);
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
            _logger.LogError(ex, "Error retrieving contact preference.");

            return StatusCode(500, new {message = "An unexpected error occurred."});
        }
    }

    [HttpPut("{contactId:guid}/pin")]
    public async Task<IActionResult> UpdatePin(Guid contactId, [FromBody] UpdateContactPinRequestDto request)
    {
        try
        {
            var ownerId = GetCurrentUserId();
            var preference = await _service.UpdatePinAsync(ownerId, contactId, request.IsPinned);

            return Ok(preference);
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
            _logger.LogError(ex, "Error updating contact pin preference.");

            return StatusCode(500, new {message = "An unexpected error occurred."});
        }
    }

    [HttpPut("{contactId:guid}/archive")]
    public async Task<IActionResult> UpdateArchive(Guid contactId, [FromBody] UpdateContactArchiveRequestDto request)
    {
        try
        {
            var ownerId = GetCurrentUserId();
            var preference = await _service.UpdateArchiveAsync(ownerId, contactId, request.IsArchived);

            return Ok(preference);
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
            _logger.LogError(ex, "Error updating contact archive preference.");

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