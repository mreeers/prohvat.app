using System;
using System.Collections.Generic;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ConversationType Type { get; set; }
    
    // For RideGroup type
    public Guid? RideId { get; set; }
    public Ride? Ride { get; set; }
    
    // Telegram integration fields (prepared for future)
    public string? TelegramChatId { get; set; }
    
    // For RideGroup type
    public string? Title { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ConversationParticipant> Participants { get; set; } = new();
    public List<ChatMessage> Messages { get; set; } = new();
}
