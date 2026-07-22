using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ProhvatApp.Application.Common.Interfaces;

namespace ProhvatApp.Application.Spots.Queries;

public class SpotDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Complexity { get; set; }
    public ProhvatApp.Domain.Enums.SeasonType Season { get; set; }
}

public record GetSpotsInBoundsQuery(
    double MinLat, 
    double MinLng, 
    double MaxLat, 
    double MaxLng,
    ProhvatApp.Domain.Enums.SeasonType? Season
) : IRequest<List<SpotDto>>;

public class GetSpotsInBoundsQueryHandler : IRequestHandler<GetSpotsInBoundsQuery, List<SpotDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSpotsInBoundsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SpotDto>> Handle(GetSpotsInBoundsQuery request, CancellationToken cancellationToken)
    {
        // Create polygon for viewport bounds
        var ring = new LinearRing(new Coordinate[]
        {
            new Coordinate(request.MinLng, request.MinLat),
            new Coordinate(request.MaxLng, request.MinLat),
            new Coordinate(request.MaxLng, request.MaxLat),
            new Coordinate(request.MinLng, request.MaxLat),
            new Coordinate(request.MinLng, request.MinLat) // Close ring
        });
        var viewport = new Polygon(ring) { SRID = 4326 };

        var query = _context.Spots.Where(s => s.Location.Intersects(viewport));

        if (request.Season.HasValue)
        {
            query = query.Where(s => s.Season == request.Season.Value);
        }

        var spots = await query
            .Take(100)
            .ToListAsync(cancellationToken);

        return spots.Select(s => new SpotDto
        {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            Latitude = s.Location.Y,
            Longitude = s.Location.X,
            Complexity = (int)s.Complexity,
            Season = s.Season
        }).ToList();
    }
}
