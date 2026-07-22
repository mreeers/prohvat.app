using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Application.Chat.Commands;

public record SendChatMessageCommand(Guid ConversationId, Guid SenderId, string Text) : IRequest<ChatMessageDto?>;

public record ChatMessageDto(Guid Id, Guid ConversationId, Guid SenderId, string UserName, string? UserAvatarUrl, string Text, DateTime CreatedAt);

public class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, ChatMessageDto?>
{
    private readonly IApplicationDbContext _context;

    public SendChatMessageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChatMessageDto?> Handle(SendChatMessageCommand request, CancellationToken cancellationToken)
    {
        // Verify conversation and participation
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == request.ConversationId && cp.UserId == request.SenderId, cancellationToken);
            
        if (participant == null) return null; // Not part of this conversation
        
        // In case of direct chat, verify blocking
        var conversation = await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);
            
        if (conversation == null) return null;
        
        if (conversation.Type == ProhvatApp.Domain.Enums.ConversationType.Direct)
        {
            var otherParticipant = conversation.Participants.FirstOrDefault(p => p.UserId != request.SenderId);
            if (otherParticipant != null)
            {
                // Check if the other participant blocked the sender
                var isBlocked = await _context.UserBlocks
                    .AnyAsync(ub => ub.BlockerId == otherParticipant.UserId && ub.BlockedId == request.SenderId, cancellationToken);
                if (isBlocked) throw new Exception("You are blocked by this user.");
            }
        }

        var user = await _context.Users.FindAsync(new object[] { request.SenderId }, cancellationToken);
        if (user == null) return null;

        var message = new ChatMessage
        {
            ConversationId = request.ConversationId,
            SenderId = request.SenderId,
            Text = request.Text,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        return new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            user.Name,
            user.AvatarUrl,
            message.Text,
            message.CreatedAt
        );
    }
}
