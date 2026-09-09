using System;

namespace ProhvatApp.Application.Achievements.DTOs;

public class UserAchievementDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool IsEarned { get; set; }
    public DateTime? EarnedAt { get; set; }
}
