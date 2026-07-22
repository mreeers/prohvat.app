using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;

namespace ProhvatApp.Application.Rides.Queries;

public record GetRideMembershipQuery(Guid RideId, Guid UserId) : IRequest<bool>;

public class GetRideMembershipQueryHandler : IRequestHandler<GetRideMembershipQuery, bool>
{
    private readonly IApplicationDbContext _context;

    public GetRideMembershipQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(GetRideMembershipQuery request, CancellationToken cancellationToken)
    {
        var ride = await _context.Rides.FindAsync(new object[] { request.RideId }, cancellationToken);
        if (ride != null && ride.OrganizerId == request.UserId) return true;

        return await _context.RideMembers.AnyAsync(m => m.RideId == request.RideId && m.UserId == request.UserId, cancellationToken);
    }
}
