using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Application.Rides.Queries;
using ProhvatApp.Application.Rides.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProhvatApp.Application.Rides.Queries.GetFeed;

public class GetRidesFeedQueryHandler : IRequestHandler<GetRidesFeedQuery, List<RideDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRidesFeedQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RideDto>> Handle(GetRidesFeedQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Rides
            .Include(r => r.Organizer)
            .Include(r => r.TargetCategory)
            .Include(r => r.Members)
            .AsNoTracking()
            .Where(r => r.EventDate >= DateTime.UtcNow);

        if (request.CityId.HasValue)
        {
            query = query.Where(r => r.CityId == request.CityId.Value);
        }

        if (request.Season.HasValue)
        {
            query = query.Where(r => r.Season == request.Season.Value);
        }

        var rides = await query
            .OrderBy(r => r.EventDate)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        return rides.Select(r => new RideDto
        {
            Id = r.Id,
            OrganizerId = r.OrganizerId,
            Title = r.Title,
            Description = r.Description,
            EventDate = r.EventDate,
            Complexity = r.Complexity,
            Type = r.Type,
            Season = r.Season,
            Status = r.Status,
            StartLat = r.StartPoint.Y,
            StartLng = r.StartPoint.X,
            MaxMembers = r.MaxMembers,
            DistanceToUserInKm = 0 // Optional for feed or we can calculate later
        }).ToList();
    }
}
