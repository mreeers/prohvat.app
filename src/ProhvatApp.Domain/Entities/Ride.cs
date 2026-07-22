using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Domain.Entities;

public class Ride
{
    public Guid Id { get; set; }
    public Guid OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TargetCategoryId { get; set; } // Ограничение по типу техники
    public VehicleCategory TargetCategory { get; set; } = null!;
    public DateTime EventDate { get; set; }
    public ComplexityType Complexity { get; set; }
    public RideType Type { get; set; }
    public SeasonType Season { get; set; }
    public RideStatus Status { get; set; } = RideStatus.Planned;
    public string? Report { get; set; }
    public int MaxMembers { get; set; }
    
    public Guid? CityId { get; set; }
    public City? City { get; set; }
    
    // Geo-данные PostGIS (NetTopologySuite)
    public Point StartPoint { get; set; } = null!; // Точка сбора
    public string GpxTrackPath { get; set; } = string.Empty; // Ссылка на файл трека в S3
    
    public List<RideMember> Members { get; set; } = new();
}
