using MensajeService.DTOs.Contacts;
using MensajeService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MensajeService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactController : ControllerBase
{
    private readonly ContactServices _contactServices;
    private readonly ILogger<ContactController> _logger;

    public ContactController(ContactServices contactServices, ILogger<ContactController> logger)
    {
        _contactServices = contactServices;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SendRequest([FromBody] CreateContactRequestDto request)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized("Cannot identify the user.");

        try
        {
            var result = await _contactServices.SendRequestAsync(userId.Value, request.AddresseeId);
            return Ok(result);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("requests/received")]
    public async Task<IActionResult> GetReceivedRequests()
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized("Cannot identify the user.");

        try
        {
            var result = await _contactServices.GetReceivedRequestsAsync(userId.Value);
            return Ok(result);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("requests/sent")]
    public async Task<IActionResult> GetSentRequests()
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized("Cannot identify the user.");

        try
        {
            var result = await _contactServices.GetSentRequestsAsync(userId.Value);
            return Ok(result);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPut("{id:guid}/accept")]
    public async Task<IActionResult> AcceptRequest(Guid id)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized("Cannot identify the user.");

        try
        {
            var result = await _contactServices.AcceptRequestAsync(userId.Value, id);
            return Ok(result);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpPut("{id:guid}/reject")]
    public async Task<IActionResult> RejectRequest(Guid id)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized("Cannot identify the user.");

        try
        {
            var result = await _contactServices.RejectRequestAsync(userId.Value, id);
            return Ok(result);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpDelete("requests/{id:guid}")]
    public async Task<IActionResult> CancelRequest(Guid id)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized("Cannot identify the user.");

        try
        {
            var result = await _contactServices.CancelRequestAsync(userId.Value, id);
            return Ok(result);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetContacts()
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized("Cannot identify the user.");

        try
        {
            var result = await _contactServices.GetContactsAsync(userId.Value);
            return Ok(result);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RemoveContact(Guid id)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized("Cannot identify the user.");

        try
        {
            await _contactServices.RemoveContactAsync(userId.Value, id);
            return NoContent();
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    private Guid? GetAuthenticatedUserId()
    {
        var userId = User.FindFirstValue("sub");
        if (!Guid.TryParse(userId, out var parsedUserId))
            return null;

        return parsedUserId;
    }

    private IActionResult HandleException(Exception exception)
    {
        if (exception is not (KeyNotFoundException or UnauthorizedAccessException or InvalidOperationException or ArgumentException))
        {
            _logger.LogError(exception, "Unexcepted error in ContactController.");
            return StatusCode(500, new{message = "Has occurred an internal error."});
        }

        return exception switch
        {
            KeyNotFoundException =>NotFound(new { message = exception.Message }),
            UnauthorizedAccessException =>Forbid(),
            InvalidOperationException =>Conflict(new { message = exception.Message }),
            ArgumentException =>BadRequest(new { message = exception.Message }),
            _ => StatusCode(500)
        };
    }
}