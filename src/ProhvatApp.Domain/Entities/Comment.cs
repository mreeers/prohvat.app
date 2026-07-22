using System;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Polymorphic relationship
    public Guid TargetId { get; set; }
    public TargetType TargetType { get; set; }
}
