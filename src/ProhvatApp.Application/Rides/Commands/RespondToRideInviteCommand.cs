using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Rides.Commands;

public record RespondToRideInviteCommand(Guid InviteId, Guid UserId, bool Accept) : IRequest<bool>;

public class RespondToRideInviteCommandHandler : IRequestHandler<RespondToRideInviteCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public RespondToRideInviteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RespondToRideInviteCommand request, CancellationToken cancellationToken)
    {
        var invite = await _context.RideInvites
            .FirstOrDefaultAsync(ri => ri.Id == request.InviteId, cancellationToken);

        if (invite == null || invite.InviteeId != request.UserId)
            return false;

        invite.Status = request.Accept ? RideInviteStatus.Accepted : RideInviteStatus.Declined;
        invite.UpdatedAt = DateTime.UtcNow;

        if (request.Accept)
            await AddMemberToRide(invite.RideId, invite.InviteeId, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task AddMemberToRide(Guid rideId, Guid userId, CancellationToken cancellationToken)
    {
        var isMember = await _context.RideMembers
            .AnyAsync(rm => rm.RideId == rideId && rm.UserId == userId, cancellationToken);

        if (!isMember)
        {
            _context.RideMembers.Add(new RideMember
            {
                RideId = rideId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });
            
            // Also add to Conversation if it exists
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.RideId == rideId, cancellationToken);
                
            if (conversation != null)
            {
                var isParticipant = await _context.ConversationParticipants
                    .AnyAsync(cp => cp.ConversationId == conversation.Id && cp.UserId == userId, cancellationToken);
                    
                if (!isParticipant)
                {
                    _context.ConversationParticipants.Add(new ConversationParticipant
                    {
                        ConversationId = conversation.Id,
                        UserId = userId,
                        JoinedAt = DateTime.UtcNow
                    });
                }
            }
        }
    }
}
