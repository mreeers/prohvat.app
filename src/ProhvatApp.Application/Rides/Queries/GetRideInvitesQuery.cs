using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Rides.Queries;

public record GetRideInvitesQuery(Guid RideId, Guid OrganizerId) : IRequest<List<RideInviteDto>>;

public record RideInviteDto(Guid Id, Guid InviteeId, string InviteeName, string? InviteeAvatarUrl, RideInviteStatus Status, DateTime UpdatedAt);

public class GetRideInvitesQueryHandler : IRequestHandler<GetRideInvitesQuery, List<RideInviteDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRideInvitesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RideInviteDto>> Handle(GetRideInvitesQuery request, CancellationToken cancellationToken)
    {
        var ride = await _context.Rides.FindAsync(new object[] { request.RideId }, cancellationToken);
        if (ride == null || ride.OrganizerId != request.OrganizerId)
            return new List<RideInviteDto>();

        return await _context.RideInvites
            .Include(ri => ri.Invitee)
            .Where(ri => ri.RideId == request.RideId)
            .Select(ri => new RideInviteDto(
                ri.Id,
                ri.InviteeId,
                ri.Invitee.Name,
                ri.Invitee.AvatarUrl,
                ri.Status,
                ri.UpdatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
