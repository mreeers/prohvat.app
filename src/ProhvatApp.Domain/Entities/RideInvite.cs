using System;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Domain.Entities;

public class RideInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RideId { get; set; }
    public Ride Ride { get; set; } = null!;

    public Guid InviterId { get; set; }
    public User Inviter { get; set; } = null!;

    public Guid InviteeId { get; set; }
    public User Invitee { get; set; } = null!;

    public RideInviteStatus Status { get; set; } = RideInviteStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
