namespace ProhvatApp.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User Owner { get; set; } = null!;
    
    public Guid CategoryId { get; set; }
    public VehicleCategory Category { get; set; } = null!;
    
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal CurrentMetricsValue { get; set; }
    public ProhvatApp.Domain.Enums.OdometerType OdometerType { get; set; } = ProhvatApp.Domain.Enums.OdometerType.Kilometers;
    public decimal MaintenanceInterval { get; set; } // e.g. 15 for moto hours, 1000 for km
    
    public string? TechnicalConfigJson { get; set; }
    public string? ImageUrl { get; set; }
}
