using System;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Rides.DTOs;

public class RideDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public ProhvatApp.Domain.Enums.ComplexityType Complexity { get; set; }
    public ProhvatApp.Domain.Enums.RideType Type { get; set; }
    public ProhvatApp.Domain.Enums.SeasonType Season { get; set; }
    public ProhvatApp.Domain.Enums.RideStatus Status { get; set; }
    public Guid OrganizerId { get; set; }
    public int MaxMembers { get; set; }
    public double StartLat { get; set; }
    public double StartLng { get; set; }
    public double DistanceToUserInKm { get; set; }
}
