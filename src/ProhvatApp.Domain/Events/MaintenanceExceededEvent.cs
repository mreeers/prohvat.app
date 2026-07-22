namespace ProhvatApp.Domain.Events;

public class MaintenanceExceededEvent
{
    public Guid VehicleId { get; set; }
    public Guid UserId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public decimal CurrentMetricsValue { get; set; }
    public ProhvatApp.Domain.Enums.OdometerType OdometerType { get; set; }
}
