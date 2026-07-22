using System;

namespace ProhvatApp.Domain.Entities;

public class RideReview
{
    public Guid Id { get; set; }
    
    public Guid RideId { get; set; }
    public Ride Ride { get; set; } = null!;
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public int Rating { get; set; } // 1-5
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
