using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Application.Profile.DTOs;
using ProhvatApp.Application.Vehicles.Queries.GetMyVehicles;

namespace ProhvatApp.Application.Profile.Queries;

public record GetUserProfileQuery(string Username) : IRequest<UserProfileDto?>;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto?>
{
    private readonly IApplicationDbContext _context;

    public GetUserProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileDto?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Vehicles)
                .ThenInclude(v => v.Category)
            .Include(u => u.OrganizedRides)
                .ThenInclude(r => r.TargetCategory)
            .Include(u => u.OrganizedRides)
                .ThenInclude(r => r.Members)
            .Include(u => u.City)
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        if (user == null)
        {
            return null;
        }

        return new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Username = user.Username ?? string.Empty,
            AvatarUrl = user.AvatarUrl ?? string.Empty,
            Bio = user.Bio ?? string.Empty,
            IsVisibleOnMap = user.IsVisibleOnMap,
            CityId = user.CityId,
            CityName = user.City != null ? user.City.Name : null,
            CityLat = user.City != null ? user.City.CenterLat : null,
            CityLng = user.City != null ? user.City.CenterLng : null,
            OrganizedRidesCount = user.OrganizedRides.Count,
            Vehicles = user.Vehicles.Select(v => new VehicleDto(v.Id, v.Brand, v.Model, v.Year, v.CurrentMetricsValue, v.Category.Name)).ToList(),
            OrganizedRides = user.OrganizedRides
                .OrderByDescending(r => r.EventDate)
                .Select(r => new RideProfileDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Description = r.Description ?? string.Empty,
                    EventDate = r.EventDate,
                    MaxMembers = r.MaxMembers,
                    MembersCount = r.Members?.Count ?? 0,
                    Complexity = (int)r.Complexity,
                    CategoryName = r.TargetCategory?.Name
                }).ToList()
        };
    }
}
