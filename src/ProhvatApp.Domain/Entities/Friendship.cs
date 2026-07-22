using System;

namespace ProhvatApp.Domain.Entities;

public class Friendship
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public User Requester { get; set; } = null!;

    public Guid AddresseeId { get; set; }
    public User Addressee { get; set; } = null!;

    // 0 = Pending, 1 = Accepted, 2 = Rejected/Blocked
    public int Status { get; set; }
}
