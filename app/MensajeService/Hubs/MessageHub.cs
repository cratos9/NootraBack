using MensajeService.DTOs.Messages;
using MensajeService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MensajeService.Hubs;

[Authorize]
public class MessageHub : Hub
{
    private readonly MessageService _messageService;

    public MessageHub(MessageService messageService)
    {
        _messageService = messageService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();

        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));

        await base.OnConnectedAsync();
    }

    public async Task<MessageResponseDto> SendText(SendMessageRequestDto request)
    {
        var userId = GetUserId();

        try
        {
            var message = await _messageService.SendTextAsync(userId, request);
            var activeParticipants = await _messageService.GetActiveParticipantIdsAsync(request.ConversationId);

            foreach (var participantId in activeParticipants)
            {
                await Clients.Group(GetUserGroupName(participantId)).SendAsync("MessageReceived", message);
            }

            return message;
        }
        catch (ArgumentException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            throw new HubException("Conversation not found.");
        }
        catch (UnauthorizedAccessException)
        {
            throw new HubException("You are not an active participant of this conversation.");
        }
    }

    public async Task MarkDelivered(Guid messageId)
    {
        var recipientId = GetUserId();

        try
        {
            var status = await _messageService.MarkDeliveredAsync(recipientId, messageId);
            await Clients.Group(GetUserGroupName(status.SenderId)).SendAsync("MessageDelivered", status);
        }
        catch (ArgumentException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            throw new HubException("Message receipt not found.");
        }
    }

    public async Task MarkRead(Guid messageId)
    {
        var recipientId = GetUserId();

        try
        {
            var status = await _messageService.MarkReadAsync(recipientId, messageId);
            await Clients.Group(GetUserGroupName(status.SenderId)).SendAsync("MessageRead", status);
        }
        catch (ArgumentException ex)
        {
            throw new HubException(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            throw new HubException("Message receipt not found.");
        }
    }

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirst("sub")?.Value;

        if (!Guid.TryParse(claim, out var userId))
            throw new HubException("Invalid user identity.");

        return userId;
    }

    private static string GetUserGroupName(Guid userId)
    {
        return $"user:{userId}";
    }
}
