using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Application.Rides.DTOs;

namespace ProhvatApp.Application.Rides.Queries;

public record GetRidesInBoundsQuery(
    double MinLat, 
    double MinLng, 
    double MaxLat, 
    double MaxLng,
    ProhvatApp.Domain.Enums.SeasonType? Season) : IRequest<List<RideDto>>;

public class GetRidesInBoundsQueryHandler : IRequestHandler<GetRidesInBoundsQuery, List<RideDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRidesInBoundsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RideDto>> Handle(GetRidesInBoundsQuery request, CancellationToken cancellationToken)
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        
        var polygon = geometryFactory.CreatePolygon(new[]
        {
            new Coordinate(request.MinLng, request.MinLat),
            new Coordinate(request.MaxLng, request.MinLat),
            new Coordinate(request.MaxLng, request.MaxLat),
            new Coordinate(request.MinLng, request.MaxLat),
            new Coordinate(request.MinLng, request.MinLat)
        });

        var query = _context.Rides
            .AsNoTracking()
            .Where(r => r.StartPoint.Intersects(polygon));

        if (request.Season.HasValue)
        {
            query = query.Where(r => r.Season == request.Season.Value);
        }

        var rawRides = await query
            .Select(r => new  
            {
                r.Id,
                r.Title,
                r.Description,
                r.EventDate,
                r.Complexity,
                r.Type,
                r.MaxMembers,
                r.Season,
                r.Status,
                r.OrganizerId,
                r.StartPoint
            })
            .ToListAsync(cancellationToken);

        var rides = rawRides.Select(r => new RideDto
        {
            Id = r.Id,
            Title = r.Title,
            Description = r.Description,
            EventDate = r.EventDate,
            Complexity = r.Complexity,
            Type = r.Type,
            Season = r.Season,
            Status = r.Status,
            OrganizerId = r.OrganizerId,
            MaxMembers = r.MaxMembers,
            StartLat = r.StartPoint.Y,
            StartLng = r.StartPoint.X,
            DistanceToUserInKm = 0
        }).ToList();

        return rides;
    }
}
