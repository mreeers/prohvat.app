using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProhvatApp.Application.Profile.Queries;

public class ActiveRiderDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? VehicleName { get; set; }
    public string? VehicleCategoryName { get; set; }
    public DateTime LocationUpdatedAt { get; set; }
}

public class GetActiveRidersQuery : IRequest<List<ActiveRiderDto>>
{
}

public class GetActiveRidersQueryHandler : IRequestHandler<GetActiveRidersQuery, List<ActiveRiderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetActiveRidersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ActiveRiderDto>> Handle(GetActiveRidersQuery request, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);

        var activeUsers = await _context.Users
            .Include(u => u.Vehicles)
            .ThenInclude(v => v.Category)
            .Where(u => u.IsVisibleOnMap 
                     && u.LastKnownLocation != null 
                     && u.LocationUpdatedAt != null 
                     && u.LocationUpdatedAt > cutoff)
            .ToListAsync(cancellationToken);

        return activeUsers.Select(u => {
            var primaryVehicle = u.Vehicles.FirstOrDefault();
            return new ActiveRiderDto
            {
                UserId = u.Id,
                Username = u.Username ?? u.Name,
                AvatarUrl = u.AvatarUrl,
                Latitude = u.LastKnownLocation!.Y, // NetTopologySuite: Y is Latitude
                Longitude = u.LastKnownLocation!.X, // NetTopologySuite: X is Longitude
                LocationUpdatedAt = u.LocationUpdatedAt!.Value,
                VehicleName = primaryVehicle != null ? $"{primaryVehicle.Brand} {primaryVehicle.Model}" : null,
                VehicleCategoryName = primaryVehicle?.Category?.Name
            };
        }).ToList();
    }
}
