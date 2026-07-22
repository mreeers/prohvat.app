using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace ProhvatApp.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    
    // Profile features
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public Guid? CityId { get; set; }
    public City? City { get; set; }
    public Point? LastKnownLocation { get; set; }
    public DateTime? LocationUpdatedAt { get; set; }
    
    // Navigation properties
    public List<Vehicle> Vehicles { get; set; } = new();
    public List<Ride> OrganizedRides { get; set; } = new();
    public List<RideMember> RideMemberships { get; set; } = new();
    public List<ConversationParticipant> Conversations { get; set; } = new();
    public List<UserBlock> BlockedUsers { get; set; } = new();
    public List<UserBlock> BlockedByUsers { get; set; } = new();
    public List<RideInvite> SentInvites { get; set; } = new();
    public List<RideInvite> ReceivedInvites { get; set; } = new();
}
