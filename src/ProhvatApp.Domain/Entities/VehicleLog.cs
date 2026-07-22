namespace ProhvatApp.Domain.Entities;

public class VehicleLog
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public decimal MetricsValueAtLog { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int LikeCount { get; set; }
    
    public List<string> ImageUrls { get; set; } = new();
    public List<string> VideoUrls { get; set; } = new();
}
