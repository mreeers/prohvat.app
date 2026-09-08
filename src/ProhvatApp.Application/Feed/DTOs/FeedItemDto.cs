using System;
using System.Collections.Generic;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Feed.DTOs;

public class FeedItemDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "vehicle_log"; // "vehicle_log" or "ride"
    public TargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    
    // Author
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorUsername { get; set; }
    public string? AuthorAvatarUrl { get; set; }
    
    // Content
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public List<string> VideoUrls { get; set; } = new();
    
    // Vehicle specifics
    public Guid? VehicleId { get; set; }
    public string? VehicleBrand { get; set; }
    public string? VehicleModel { get; set; }
    public int? VehicleYear { get; set; }
    public decimal? MetricsValue { get; set; }
    public string? MetricUnit { get; set; } // "км" / "м/ч"
    public string? CategoryName { get; set; }
    public SeasonType? Season { get; set; }
    
    // Tuning / Parts installed
    public List<FeedPartReviewDto> PartReviews { get; set; } = new();
    
    // Ride specifics
    public DateTime? EventDate { get; set; }
    public string? Complexity { get; set; }
    public string? RideType { get; set; }
    public string? CityName { get; set; }
    public int? MembersCount { get; set; }
    public int? MaxMembers { get; set; }
    public bool IsJoined { get; set; }
    
    // Interactions
    public int LikesCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public int CommentsCount { get; set; }
}

public class FeedPartReviewDto
{
    public Guid Id { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? VendorCode { get; set; }
    public string? MarketplaceLink { get; set; }
}
