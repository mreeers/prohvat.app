using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Application.Feed.DTOs;
using ProhvatApp.Domain.Entities;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Feed.Queries.GetFeed;

public class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, List<FeedItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IVehicleLogRepository _vehicleLogRepository;

    public GetFeedQueryHandler(IApplicationDbContext context, IVehicleLogRepository vehicleLogRepository)
    {
        _context = context;
        _vehicleLogRepository = vehicleLogRepository;
    }

    public async Task<List<FeedItemDto>> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        List<Guid>? allowedAuthorIds = null;

        if (request.OnlyFriends && request.CurrentUserId.HasValue)
        {
            var userId = request.CurrentUserId.Value;
            var friendIds = await _context.Friendships
                .AsNoTracking()
                .Where(f => f.Status == 1 && (f.RequesterId == userId || f.AddresseeId == userId))
                .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
                .ToListAsync(cancellationToken);

            friendIds.Add(userId); // include user's own posts in friends feed
            allowedAuthorIds = friendIds;
        }

        // 1. Fetch Vehicles matching criteria
        var vehiclesQuery = _context.Vehicles
            .AsNoTracking()
            .Include(v => v.Owner)
            .Include(v => v.Category)
            .AsQueryable();

        if (allowedAuthorIds != null)
        {
            vehiclesQuery = vehiclesQuery.Where(v => allowedAuthorIds.Contains(v.UserId));
        }

        if (request.Season.HasValue)
        {
            vehiclesQuery = vehiclesQuery.Where(v => v.Category.Season == request.Season.Value || v.Category.Season == SeasonType.AllSeason);
        }

        var vehicles = await vehiclesQuery.ToListAsync(cancellationToken);
        var vehicleDict = vehicles.ToDictionary(v => v.Id);
        var vehicleIds = vehicles.Select(v => v.Id).ToList();

        // 2. Fetch VehicleLogs from Mongo
        var logs = await _vehicleLogRepository.GetRecentLogsAsync(
            allowedAuthorIds != null ? vehicleIds : null, 
            100, 
            cancellationToken);

        // Filter logs whose vehicle is in the matched vehicle dictionary
        var validLogs = logs.Where(l => vehicleDict.ContainsKey(l.VehicleId)).ToList();

        // 3. Fetch Rides matching criteria
        var ridesQuery = _context.Rides
            .AsNoTracking()
            .Include(r => r.Organizer)
            .Include(r => r.TargetCategory)
            .Include(r => r.City)
            .Include(r => r.Members)
            .AsQueryable();

        if (allowedAuthorIds != null)
        {
            ridesQuery = ridesQuery.Where(r => allowedAuthorIds.Contains(r.OrganizerId));
        }

        if (request.Season.HasValue)
        {
            ridesQuery = ridesQuery.Where(r => r.Season == request.Season.Value || r.Season == SeasonType.AllSeason);
        }

        var rides = await ridesQuery
            .OrderByDescending(r => r.EventDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        // 4. Fetch PartReviews for these vehicle categories
        var categoryIds = vehicles.Select(v => v.CategoryId).Distinct().ToList();
        var partReviews = await _context.PartReviews
            .AsNoTracking()
            .Where(p => categoryIds.Contains(p.VehicleCategoryId))
            .ToListAsync(cancellationToken);
        var partReviewLookup = partReviews.ToLookup(p => p.VehicleCategoryId);

        // 5. Interactions: Likes & Comments
        var targetIds = validLogs.Select(l => l.Id).Concat(rides.Select(r => r.Id)).ToList();

        var likesCounts = await _context.Likes
            .AsNoTracking()
            .Where(l => targetIds.Contains(l.TargetId))
            .GroupBy(l => l.TargetId)
            .Select(g => new { TargetId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TargetId, x => x.Count, cancellationToken);

        HashSet<Guid> userLikedIds = new();
        if (request.CurrentUserId.HasValue)
        {
            var liked = await _context.Likes
                .AsNoTracking()
                .Where(l => l.UserId == request.CurrentUserId.Value && targetIds.Contains(l.TargetId))
                .Select(l => l.TargetId)
                .ToListAsync(cancellationToken);
            userLikedIds = new HashSet<Guid>(liked);
        }

        var commentsCounts = await _context.Comments
            .AsNoTracking()
            .Where(c => targetIds.Contains(c.TargetId))
            .GroupBy(c => c.TargetId)
            .Select(g => new { TargetId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TargetId, x => x.Count, cancellationToken);

        // 6. Build combined feed items
        var feedItems = new List<FeedItemDto>();

        foreach (var log in validLogs)
        {
            var vehicle = vehicleDict[log.VehicleId];
            var author = vehicle.Owner;

            likesCounts.TryGetValue(log.Id, out var lCount);
            commentsCounts.TryGetValue(log.Id, out var cCount);

            var item = new FeedItemDto
            {
                Id = log.Id,
                Type = "vehicle_log",
                TargetType = TargetType.VehicleLog,
                TargetId = log.Id,
                AuthorId = author.Id,
                AuthorName = author.Name,
                AuthorUsername = author.Username,
                AuthorAvatarUrl = author.AvatarUrl,
                Title = log.Title,
                Content = log.Content,
                CreatedAt = log.CreatedAt,
                ImageUrls = log.ImageUrls ?? new List<string>(),
                VideoUrls = log.VideoUrls ?? new List<string>(),
                VehicleId = vehicle.Id,
                VehicleBrand = vehicle.Brand,
                VehicleModel = vehicle.Model,
                VehicleYear = vehicle.Year,
                MetricsValue = log.MetricsValueAtLog > 0 ? log.MetricsValueAtLog : vehicle.CurrentMetricsValue,
                MetricUnit = vehicle.OdometerType == OdometerType.MotoHours ? "м/ч" : "км",
                CategoryName = vehicle.Category?.Name,
                Season = vehicle.Category?.Season,
                PartReviews = partReviewLookup[vehicle.CategoryId].Select(pr => new FeedPartReviewDto
                {
                    Id = pr.Id,
                    PartName = pr.PartName,
                    VendorCode = pr.VendorCode,
                    MarketplaceLink = pr.MarketplaceLink
                }).ToList(),
                LikesCount = Math.Max(log.LikeCount, lCount),
                IsLikedByCurrentUser = userLikedIds.Contains(log.Id),
                CommentsCount = cCount
            };
            feedItems.Add(item);
        }

        foreach (var ride in rides)
        {
            likesCounts.TryGetValue(ride.Id, out var lCount);
            commentsCounts.TryGetValue(ride.Id, out var cCount);

            var item = new FeedItemDto
            {
                Id = ride.Id,
                Type = "ride",
                TargetType = TargetType.Ride,
                TargetId = ride.Id,
                AuthorId = ride.OrganizerId,
                AuthorName = ride.Organizer.Name,
                AuthorUsername = ride.Organizer.Username,
                AuthorAvatarUrl = ride.Organizer.AvatarUrl,
                Title = ride.Title,
                Content = ride.Description,
                CreatedAt = ride.EventDate.AddHours(-24), // Created before the event
                EventDate = ride.EventDate,
                Complexity = ride.Complexity.ToString(),
                RideType = ride.Type.ToString(),
                CityName = ride.City?.Name,
                CategoryName = ride.TargetCategory?.Name,
                Season = ride.Season,
                MembersCount = ride.Members.Count,
                MaxMembers = ride.MaxMembers,
                IsJoined = request.CurrentUserId.HasValue && ride.Members.Any(m => m.UserId == request.CurrentUserId.Value),
                LikesCount = lCount,
                IsLikedByCurrentUser = userLikedIds.Contains(ride.Id),
                CommentsCount = cCount
            };
            feedItems.Add(item);
        }

        return feedItems
            .OrderByDescending(f => f.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
    }
}
