using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;

namespace ProhvatApp.Application.Rides.Queries.GetMyRides;

public record GetMyRidesQuery(Guid UserId) : IRequest<List<MyRideDto>>;

public record MyRideDto(Guid Id, string Title);

public class GetMyRidesQueryHandler : IRequestHandler<GetMyRidesQuery, List<MyRideDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMyRidesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MyRideDto>> Handle(GetMyRidesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Rides
            .Where(r => r.OrganizerId == request.UserId && r.Status != Domain.Enums.RideStatus.Completed && r.Status != Domain.Enums.RideStatus.Failed)
            .OrderBy(r => r.EventDate)
            .Select(r => new MyRideDto(r.Id, r.Title))
            .ToListAsync(cancellationToken);
    }
}
