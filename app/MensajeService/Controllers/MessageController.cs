using MensajeService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MensajeService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly MessageService _messageService;

    public MessageController(MessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpGet("conversation/{conversationId:guid}")]
    public async Task<IActionResult> GetHistory(Guid conversationId)
    {
        var claim = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(claim, out var userId))
            return Unauthorized();

        try
        {
            var messages = await _messageService.GetHistoryAsync(userId, conversationId);

            return Ok(messages);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Conversation not found." });
        }
    }
}
