using MensajeService.DTOs.Conversations;
using MensajeService.Hubs;
using MensajeService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MensajeService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationController : ControllerBase
{
    private readonly ConversationService _conversationService;
    private readonly IHubContext<MessageHub> _hubContext;
    private readonly ILogger<ConversationController> _logger;

    public ConversationController(
        ConversationService conversationService,
        IHubContext<MessageHub> hubContext,
        ILogger<ConversationController> logger)
    {
        _conversationService = conversationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost("direct")]
    public async Task<IActionResult> CreateDirect([FromBody] CreateDirectConversationRequestDto request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var result = await _conversationService.CreateDirectAsync(userId, request.OtherUserId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequestDto request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var result = await _conversationService.CreateGroupAsync(userId, request);

            await NotifyParticipantsAsync(result.Participants.Select(p => p.UserId), "GroupCreated", result);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyConversations()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var result = await _conversationService.GetMyConversationsAsync(userId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var result = await _conversationService.GetByIdAsync(userId, id);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{id:guid}/participants")]
    public async Task<IActionResult> GetParticipants(Guid id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var result = await _conversationService.GetParticipantsAsync(userId, id);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}/group")]
    public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] UpdateGroupRequestDto request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var result = await _conversationService.UpdateGroupAsync(userId, id, request);

            await NotifyParticipantsAsync(result.Participants.Select(p => p.UserId), "GroupUpdated", result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/participants")]
    public async Task<IActionResult> AddParticipants(Guid id, [FromBody] AddGroupParticipantsRequestDto request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var result = await _conversationService.AddParticipantsAsync(userId, id, request);

            await NotifyParticipantsAsync(result.Participants.Select(p => p.UserId), "ParticipantAdded", new { ConversationId = id, Participants = request.ParticipantIds });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpDelete("{id:guid}/participants/{userId:guid}")]
    public async Task<IActionResult> RemoveParticipant(Guid id, Guid userId)
    {
        if (!TryGetUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var participants = await _conversationService.GetParticipantsAsync(currentUserId, id);

            await _conversationService.RemoveParticipantAsync(currentUserId, id, userId);
            await NotifyParticipantsAsync(participants.Select(p => p.UserId), "ParticipantRemoved", new { ConversationId = id, UserId = userId, RemovedBy = currentUserId});

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:guid}/participants/{userId:guid}/role")]
    public async Task<IActionResult> UpdateParticipantRole(Guid id, Guid userId, [FromBody] UpdateParticipantRoleRequestDto request)
    {
        if (!TryGetUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            await _conversationService.UpdateParticipantRoleAsync(currentUserId, id, userId, request);

            var participants = await _conversationService.GetParticipantsAsync(currentUserId, id);

            await NotifyParticipantsAsync(participants.Select(p => p.UserId), "ParticipantRoleUpdated", new { ConversationId = id, UserId = userId, Role = request.Role, UpdatedBy = currentUserId});

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> LeaveGroup(Guid id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var participants = await _conversationService.GetParticipantsAsync(userId, id);

            await _conversationService.LeaveGroupAsync(userId, id);
            await NotifyParticipantsAsync( participants.Select(p => p.UserId), "ParticipantLeft", new{ ConversationId = id, UserId = userId});

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteForUser(Guid id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var participants = await _conversationService.GetParticipantsAsync(userId, id);

            await _conversationService.DeleteForUserAsync(userId, id);
            await NotifyParticipantsAsync(participants.Select(p => p.UserId), "ParticipantLeft", new { ConversationId = id, UserId = userId});

            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private async Task NotifyParticipantsAsync(IEnumerable<Guid> participantIds, string eventName, object payload)
    {
        foreach (var participantId in participantIds.Distinct())
        {
            try
            {
                await _hubContext.Clients.Group($"user:{participantId}").SendAsync(eventName, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send SignalR event {EventName} to user {UserId}", eventName, participantId);
            }
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirst("sub")?.Value;

        return Guid.TryParse(claim, out userId);
    }

    private IActionResult HandleException(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException =>
                NotFound(new { message = exception.Message }),

            UnauthorizedAccessException =>
                Forbid(),

            InvalidOperationException =>
                Conflict(new { message = exception.Message }),

            ArgumentException =>
                BadRequest(new { message = exception.Message }),

            _ =>
                StatusCode(500, new { message = "An internal server error occurred."})
        };
    }
}