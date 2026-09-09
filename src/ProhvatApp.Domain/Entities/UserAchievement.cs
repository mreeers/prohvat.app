using System;

namespace ProhvatApp.Domain.Entities;

public class UserAchievement
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid AchievementId { get; set; }
    public Achievement Achievement { get; set; } = null!;

    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}
