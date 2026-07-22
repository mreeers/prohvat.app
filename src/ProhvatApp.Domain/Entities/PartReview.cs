using System;

namespace ProhvatApp.Domain.Entities;

public class PartReview
{
    public Guid Id { get; set; }
    
    public Guid VehicleCategoryId { get; set; }
    public VehicleCategory Category { get; set; } = null!;
    
    public string PartName { get; set; } = string.Empty;
    public string? VendorCode { get; set; }
    public string? MarketplaceLink { get; set; }
    
    // Optional: Reference to a specific log entry where the author documented its installation
    public Guid? LogId { get; set; }
    public VehicleLog? Log { get; set; }
    
    public Guid AuthorId { get; set; } // The user who added it
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
