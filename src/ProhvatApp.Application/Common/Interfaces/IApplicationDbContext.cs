using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<VehicleCategory> VehicleCategories { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<Ride> Rides { get; }
    DbSet<RideMember> RideMembers { get; }
    DbSet<PartReview> PartReviews { get; }
    DbSet<Comment> Comments { get; }
    DbSet<RideReview> RideReviews { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Region> Regions { get; }
    DbSet<City> Cities { get; }
    DbSet<Spot> Spots { get; }
    DbSet<Friendship> Friendships { get; }
    DbSet<SosSignal> SosSignals { get; }
    DbSet<Like> Likes { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<ConversationParticipant> ConversationParticipants { get; }
    DbSet<UserBlock> UserBlocks { get; }
    DbSet<RideInvite> RideInvites { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
