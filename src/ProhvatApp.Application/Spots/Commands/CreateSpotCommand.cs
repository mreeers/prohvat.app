using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NetTopologySuite.Geometries;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Spots.Commands;

public record CreateSpotCommand(
    Guid CreatorId,
    string Title,
    string Description,
    double Latitude,
    double Longitude,
    ComplexityType Complexity,
    SeasonType Season,
    Guid? CityId
) : IRequest<Guid>;

public class CreateSpotCommandHandler : IRequestHandler<CreateSpotCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateSpotCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateSpotCommand request, CancellationToken cancellationToken)
    {
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            CreatorId = request.CreatorId,
            Title = request.Title,
            Description = request.Description,
            Location = new Point(request.Longitude, request.Latitude) { SRID = 4326 },
            Complexity = request.Complexity,
            Season = request.Season,
            CityId = request.CityId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Spots.Add(spot);
        await _context.SaveChangesAsync(cancellationToken);

        return spot.Id;
    }
}
