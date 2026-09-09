using System;

namespace ProhvatApp.Domain.Entities;

public class Achievement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty; // Emoji or icon identifier
    public string Category { get; set; } = "General"; // "Enduro", "Drift", "General", "Snow"
    public int Points { get; set; } = 10;
}
