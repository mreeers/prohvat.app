using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ProhvatApp.Application.Chat.Commands;

namespace ProhvatApp.WebAPI.Hubs;

[Authorize]
public class ProhvatHub : Hub
{
    private readonly IMediator _mediator;

    public ProhvatHub(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Called when a user opens the chat for a specific conversation
    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
    }

    // Called when a user leaves the chat or closes the conversation page
    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
    }

    // Called when a user sends a message
    public async Task SendMessage(Guid conversationId, string text)
    {
        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var command = new SendChatMessageCommand(conversationId, userId, text);
            var result = await _mediator.Send(command);

            if (result != null)
            {
                // Broadcast to all users in the conversation's group
                await Clients.Group($"Conversation_{conversationId}").SendAsync("ReceiveMessage", result);
            }
        }
    }
}
