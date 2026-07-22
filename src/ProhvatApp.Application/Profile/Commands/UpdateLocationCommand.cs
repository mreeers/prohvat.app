using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using ProhvatApp.Application.Common.Interfaces;

namespace ProhvatApp.Application.Profile.Commands;

public record UpdateLocationCommand(Guid UserId, double Latitude, double Longitude) : IRequest<bool>;

public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateLocationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return false;

        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        user.LastKnownLocation = geometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));
        user.LocationUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
