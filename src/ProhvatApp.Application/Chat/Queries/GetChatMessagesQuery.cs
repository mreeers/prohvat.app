using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Application.Chat.Commands; // for ChatMessageDto

namespace ProhvatApp.Application.Chat.Queries;

public record GetChatMessagesQuery(Guid ConversationId) : IRequest<List<ChatMessageDto>>;

public class GetChatMessagesQueryHandler : IRequestHandler<GetChatMessagesQuery, List<ChatMessageDto>>
{
    private readonly IApplicationDbContext _context;

    public GetChatMessagesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChatMessageDto>> Handle(GetChatMessagesQuery request, CancellationToken cancellationToken)
    {
        var messages = await _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == request.ConversationId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto(
                m.Id,
                m.ConversationId,
                m.SenderId,
                m.Sender.Name,
                m.Sender.AvatarUrl,
                m.Text,
                m.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return messages;
    }
}
