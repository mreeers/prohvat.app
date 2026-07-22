using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Enums;
using ProhvatApp.Application.Chat.Commands;

namespace ProhvatApp.Application.Chat.Queries;

public record GetConversationsQuery(Guid UserId) : IRequest<List<ConversationDto>>;

public record ConversationDto(Guid Id, ConversationType Type, Guid? RideId, string? Title, string? TelegramChatId, DateTime UpdatedAt, ChatMessageDto? LastMessage, List<ConversationParticipantDto> Participants);

public record ConversationParticipantDto(Guid UserId, string UserName, string? AvatarUrl);

public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, List<ConversationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetConversationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var conversations = await _context.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .ThenInclude(m => m.Sender)
            .Where(c => c.Participants.Any(p => p.UserId == request.UserId))
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationDto(
                c.Id,
                c.Type,
                c.RideId,
                c.Title,
                c.TelegramChatId,
                c.UpdatedAt,
                c.Messages.Select(m => new ProhvatApp.Application.Chat.Commands.ChatMessageDto(
                    m.Id,
                    m.ConversationId,
                    m.SenderId,
                    m.Sender.Name,
                    m.Sender.AvatarUrl,
                    m.Text,
                    m.CreatedAt
                )).FirstOrDefault(),
                c.Participants.Select(p => new ConversationParticipantDto(
                    p.UserId,
                    p.User.Name,
                    p.User.AvatarUrl
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return conversations;
    }
}
