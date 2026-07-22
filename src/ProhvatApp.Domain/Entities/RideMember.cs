using System;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Domain.Entities;

public class RideMember
{
    public Guid RideId { get; set; }
    public Ride Ride { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public MemberStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }
}
