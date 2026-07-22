using System;

namespace ProhvatApp.Domain.Entities;

public class City
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid RegionId { get; set; }
    public Region Region { get; set; } = null!;
    
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public int Population { get; set; }
}
