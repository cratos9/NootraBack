using MensajeService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MensajeService.Hubs;
using MensajeService.DTOs.Messages;

namespace MensajeService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly MessageService _messageService;
    private readonly IHubContext<MessageHub> _hubContext;

    public MessageController(MessageService messageService, IHubContext<MessageHub> hubContext)
    {
        _messageService = messageService;
        _hubContext = hubContext;
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

    [HttpPut("{messageId:guid}")]
    public async Task<IActionResult> UpdateMessage(Guid messageId, [FromBody] UpdateMessageRequestDto request)
    {
        var userId = GetUserId();

        if (userId == Guid.Empty)
            return Unauthorized();

        try
        {
            var updatedMessage = await _messageService.UpdateTextAsync(userId, messageId, request);
            var participantIds = await _messageService.GetActiveParticipantIdsAsync(updatedMessage.ConversationId);

            foreach (var participantId in participantIds)
            {
                await _hubContext.Clients.Group($"user:{participantId}").SendAsync("MessageUpdated", updatedMessage);
            }

            return Ok(updatedMessage);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Message not found." });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode( StatusCodes.Status403Forbidden, new { message = "You cannot edit this message." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid messageId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        try
        {
            var deletedMessage = await _messageService.DeleteAsync(userId, messageId);
            var participantIds = await _messageService.GetActiveParticipantIdsAsync(deletedMessage.ConversationId);

            foreach (var participantId in participantIds)
            {
                await _hubContext.Clients.Group($"user:{participantId}").SendAsync("MessageDeleted", deletedMessage);
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Message not found." });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode( StatusCodes.Status403Forbidden, new { message = "You cannot delete this message." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }


    [HttpPost("conversation/{conversationId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(28311552)]
    [RequestFormLimits(MultipartBodyLengthLimit = 28311552)]
    public async Task<IActionResult> SendAttachment(Guid conversationId, IFormFile file, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        try
        {
            var message = await _messageService.SendAttachmentAsync(userId, conversationId, file, cancellationToken);
            var participantIds = await _messageService.GetActiveParticipantIdsAsync(conversationId);
            foreach (var participantId in participantIds)
            {
                await _hubContext.Clients.Group($"user:{participantId}").SendAsync("MessageReceived", message);
            }

            return Ok(message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Conversation not found." });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode( StatusCodes.Status403Forbidden, new { message = "You are not an active participant." });
        }
    }

    [HttpGet("attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid attachmentId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        try
        {
            var attachment = await _messageService.GetAttachmentForDownloadAsync(userId, attachmentId);
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return File(attachment.Content, attachment.ContentType, attachment.FileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Attachment not found." });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Attachment file not found." });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode( StatusCodes.Status403Forbidden, new { message = "You cannot download this attachment." });
        }
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Guid.Empty;
        return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty;
    }
}