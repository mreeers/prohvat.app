using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Rides.Commands;

public record JoinRideCommand(Guid RideId, Guid UserId) : IRequest;

public class JoinRideCommandHandler : IRequestHandler<JoinRideCommand>
{
    private readonly IApplicationDbContext _context;

    public JoinRideCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(JoinRideCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.RideMembers
            .FirstOrDefaultAsync(rm => rm.RideId == request.RideId && rm.UserId == request.UserId, cancellationToken);
            
        if (existing != null)
            throw new Exception("Already a member of this ride.");

        var membership = new RideMember
        {
            RideId = request.RideId,
            UserId = request.UserId,
            Status = MemberStatus.Approved, // Auto approve for MVP
            JoinedAt = DateTime.UtcNow
        };

        _context.RideMembers.Add(membership);

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.RideId == request.RideId, cancellationToken);
            
        if (conversation != null)
        {
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversation.Id && cp.UserId == request.UserId, cancellationToken);
                
            if (!isParticipant)
            {
                _context.ConversationParticipants.Add(new ConversationParticipant
                {
                    ConversationId = conversation.Id,
                    UserId = request.UserId,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
