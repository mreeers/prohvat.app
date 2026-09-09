using System;
using System.Collections.Generic;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Rides.DTOs;

public class RideDetailDto
{
    public Guid Id { get; set; }
    public Guid OrganizerId { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public string OrganizerUsername { get; set; } = string.Empty;
    public string OrganizerAvatarUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TargetCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public ComplexityType Complexity { get; set; }
    public RideType Type { get; set; }
    public SeasonType Season { get; set; }
    public RideStatus Status { get; set; }
    public string? Report { get; set; }
    public int MaxMembers { get; set; }
    public string? CityName { get; set; }
    public double StartLat { get; set; }
    public double StartLng { get; set; }
    public string GpxTrackPath { get; set; } = string.Empty;
    public double? TotalDistanceKm { get; set; }
    public List<GpxPointDto> TrackPoints { get; set; } = new();
    public List<RideMemberItemDto> Members { get; set; } = new();
    public bool IsJoined { get; set; }
}

public class GpxPointDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double? Ele { get; set; }
}

public class RideMemberItemDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}
