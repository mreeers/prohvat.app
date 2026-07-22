using System;

namespace ProhvatApp.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    
    public string Text { get; set; } = string.Empty;
    public string? TelegramMessageId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
