using System;
using NetTopologySuite.Geometries;

namespace ProhvatApp.Domain.Entities;

public class SosSignal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Point Location { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
