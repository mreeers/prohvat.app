using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Rides.Queries.GetMyInvites;

public record GetMyInvitesQuery(Guid UserId) : IRequest<List<MyInviteDto>>;

public record MyInviteDto(
    Guid InviteId, 
    Guid RideId, 
    string RideTitle, 
    Guid InviterId, 
    string InviterName, 
    string? InviterAvatarUrl, 
    RideInviteStatus Status, 
    DateTime CreatedAt);

public class GetMyInvitesQueryHandler : IRequestHandler<GetMyInvitesQuery, List<MyInviteDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMyInvitesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MyInviteDto>> Handle(GetMyInvitesQuery request, CancellationToken cancellationToken)
    {
        return await _context.RideInvites
            .Include(ri => ri.Ride)
            .Include(ri => ri.Inviter)
            .Where(ri => ri.InviteeId == request.UserId && ri.Status == RideInviteStatus.Pending)
            .OrderByDescending(ri => ri.CreatedAt)
            .Select(ri => new MyInviteDto(
                ri.Id,
                ri.RideId,
                ri.Ride.Title,
                ri.InviterId,
                ri.Inviter.Name,
                ri.Inviter.AvatarUrl,
                ri.Status,
                ri.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
