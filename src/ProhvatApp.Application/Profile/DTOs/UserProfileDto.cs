using System;
using System.Collections.Generic;
using ProhvatApp.Application.Vehicles.Queries.GetMyVehicles;

namespace ProhvatApp.Application.Profile.DTOs;

public class RideProfileDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int MembersCount { get; set; }
    public int MaxMembers { get; set; }
    public int Complexity { get; set; }
    public string? CategoryName { get; set; }
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public Guid? CityId { get; set; }
    public string? CityName { get; set; }
    public double? CityLat { get; set; }
    public double? CityLng { get; set; }
    
    public List<VehicleDto> Vehicles { get; set; } = new();
    public List<RideProfileDto> OrganizedRides { get; set; } = new();
    public int OrganizedRidesCount { get; set; }
}
