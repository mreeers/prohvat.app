using System;
using NetTopologySuite.Geometries;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Domain.Entities;

public class Spot
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public User Creator { get; set; } = null!;
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public Point Location { get; set; } = null!;
    
    public Guid? CityId { get; set; }
    public City? City { get; set; }
    
    public ComplexityType Complexity { get; set; }
    public SeasonType Season { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
