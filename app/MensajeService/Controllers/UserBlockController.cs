using MensajeService.DTOs.UserBlocks;
using MensajeService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MensajeService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserBlockController : ControllerBase
{
    private readonly UserBlockService _service;
    private readonly ILogger<UserBlockController> _logger;

    public UserBlockController(UserBlockService service, ILogger<UserBlockController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> BlockUser([FromBody] CreateUserBlockRequestDto request)
    {
        try
        {
            var blockerId = GetCurrentUserId();
            var block = await _service.BlockUserAsync(blockerId, request.BlockedId);

            return CreatedAtAction(nameof(GetBlockStatus), new { blockedId = block.BlockedId }, block);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking user.");

            return StatusCode(500, new {message = "An unexpected error occurred."});
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetBlockedUsers()
    {
        try
        {
            var blockerId = GetCurrentUserId();

            var blocks = await _service.GetBlockedUsersAsync(blockerId);

            return Ok(blocks);
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
            _logger.LogError(ex, "Error retrieving blocked users.");

            return StatusCode(500, new {message = "An unexpected error occurred."});
        }
    }

    [HttpGet("{blockedId:guid}")]
    public async Task<IActionResult> GetBlockStatus(Guid blockedId)
    {
        try
        {
            var blockerId = GetCurrentUserId();
            var isBlocked = await _service.HasBlockedAsync(blockerId, blockedId);

            return Ok(new {blockedId, isBlocked});
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
            _logger.LogError(ex, "Error checking user block status.");

            return StatusCode(500, new {message = "An unexpected error occurred."});
        }
    }

    [HttpDelete("{blockedId:guid}")]
    public async Task<IActionResult> UnblockUser(Guid blockedId)
    {
        try
        {
            var blockerId = GetCurrentUserId();

            await _service.UnblockUserAsync(blockerId, blockedId);

            return NoContent();
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
            _logger.LogError(ex, "Error unblocking user.");

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