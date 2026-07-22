using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Domain.Entities;

public class VehicleCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SeasonType Season { get; set; }
    public MetricType Metric { get; set; }
}
