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

public class GetNearbyRidesQueryHandler : IRequestHandler<GetNearbyRidesQuery, List<RideDto>>
{
    private readonly IApplicationDbContext _context;

    public GetNearbyRidesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RideDto>> Handle(GetNearbyRidesQuery request, CancellationToken cancellationToken)
    {
        // 4326 is the SRID for WGS84 (GPS coordinates)
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var userLocation = geometryFactory.CreatePoint(new Coordinate(request.Lng, request.Lat));
        
        // Convert radius to meters since Geography uses meters
        var radiusInMeters = request.RadiusInKm * 1000;

        var rides = await _context.Rides
            // EF Core translates IsWithinDistance to ST_DWithin in PostGIS
            .Where(r => r.StartPoint.IsWithinDistance(userLocation, radiusInMeters))
            .Select(r => new RideDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                EventDate = r.EventDate,
                Complexity = r.Complexity,
                Type = r.Type,
                MaxMembers = r.MaxMembers,
                StartLat = r.StartPoint.Y,
                StartLng = r.StartPoint.X,
                // Optional: calculate actual distance if needed, EF Npgsql translates Distance to ST_Distance
                DistanceToUserInKm = r.StartPoint.Distance(userLocation) / 1000.0
            })
            // Sort by distance using Distance() which translates to ST_Distance or <-> operator
            .OrderBy(r => r.DistanceToUserInKm)
            .ToListAsync(cancellationToken);

        return rides;
    }
}
