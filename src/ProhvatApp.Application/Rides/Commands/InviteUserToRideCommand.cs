using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Rides.Commands;

public record InviteUserToRideCommand(Guid RideId, Guid InviterId, Guid InviteeId) : IRequest<bool>;

public class InviteUserToRideCommandHandler : IRequestHandler<InviteUserToRideCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public InviteUserToRideCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(InviteUserToRideCommand request, CancellationToken cancellationToken)
    {
        // Validate
        var ride = await _context.Rides.FindAsync(new object[] { request.RideId }, cancellationToken);
        if (ride == null || ride.OrganizerId != request.InviterId)
            return false;

        var invitee = await _context.Users.FindAsync(new object[] { request.InviteeId }, cancellationToken);
        if (invitee == null)
            return false;

        // Check if already invited
        var existingInvite = await _context.RideInvites
            .FirstOrDefaultAsync(ri => ri.RideId == request.RideId && ri.InviteeId == request.InviteeId, cancellationToken);
            
        if (existingInvite != null)
            return true;

        // Check if already a member
        var isMember = await _context.RideMembers
            .AnyAsync(rm => rm.RideId == request.RideId && rm.UserId == request.InviteeId, cancellationToken);
            
        if (isMember)
            return true;

        var invite = new RideInvite
        {
            RideId = request.RideId,
            InviterId = request.InviterId,
            InviteeId = request.InviteeId,
            Status = RideInviteStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.RideInvites.Add(invite);
        
        // Notify user
        var notification = new Notification
        {
            UserId = request.InviteeId,
            Message = $"Тебя пригласили на покатушку: {ride.Title}!",
            Link = $"/ride/{request.RideId}"
        };
        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
