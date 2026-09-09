using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Helpers;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Application.Rides.DTOs;

namespace ProhvatApp.Application.Rides.Queries.GetRideById;

public class GetRideByIdQueryHandler : IRequestHandler<GetRideByIdQuery, RideDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetRideByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RideDetailDto?> Handle(GetRideByIdQuery request, CancellationToken cancellationToken)
    {
        var ride = await _context.Rides
            .AsNoTracking()
            .Include(r => r.Organizer)
            .Include(r => r.TargetCategory)
            .Include(r => r.City)
            .Include(r => r.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (ride == null) return null;

        var isJoined = request.CurrentUserId.HasValue &&
            ride.Members.Any(m => m.UserId == request.CurrentUserId.Value);

        var dto = new RideDetailDto
        {
            Id = ride.Id,
            OrganizerId = ride.OrganizerId,
            OrganizerName = ride.Organizer?.Name ?? string.Empty,
            OrganizerUsername = ride.Organizer?.Username ?? string.Empty,
            OrganizerAvatarUrl = ride.Organizer?.AvatarUrl ?? string.Empty,
            Title = ride.Title,
            Description = ride.Description,
            TargetCategoryId = ride.TargetCategoryId,
            CategoryName = ride.TargetCategory?.Name ?? string.Empty,
            EventDate = ride.EventDate,
            Complexity = ride.Complexity,
            Type = ride.Type,
            Season = ride.Season,
            Status = ride.Status,
            Report = ride.Report,
            MaxMembers = ride.MaxMembers,
            CityName = ride.City?.Name,
            StartLat = ride.StartPoint != null ? ride.StartPoint.Y : 0,
            StartLng = ride.StartPoint != null ? ride.StartPoint.X : 0,
            GpxTrackPath = ride.GpxTrackPath ?? string.Empty,
            IsJoined = isJoined,
            Members = ride.Members.Select(m => new RideMemberItemDto
            {
                UserId = m.UserId,
                Name = m.User?.Name ?? string.Empty,
                Username = m.User?.Username ?? string.Empty,
                AvatarUrl = m.User?.AvatarUrl ?? string.Empty,
                JoinedAt = m.JoinedAt
            }).ToList()
        };

        // If GpxTrackPath contains XML string directly (e.g. in test seed)
        if (!string.IsNullOrWhiteSpace(ride.GpxTrackPath) && ride.GpxTrackPath.TrimStart().StartsWith("<"))
        {
            var parsed = GpxHelper.ParseGpx(ride.GpxTrackPath);
            dto.TrackPoints = parsed.Points;
            dto.TotalDistanceKm = parsed.TotalDistanceKm;
        }

        return dto;
    }
}
