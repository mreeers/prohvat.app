using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<VehicleCategory> VehicleCategories => Set<VehicleCategory>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<RideMember> RideMembers => Set<RideMember>();
    public DbSet<PartReview> PartReviews => Set<PartReview>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<RideReview> RideReviews => Set<RideReview>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Spot> Spots => Set<Spot>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<SosSignal> SosSignals => Set<SosSignal>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<RideInvite> RideInvites => Set<RideInvite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Add PostGIS extension for spatial queries
        modelBuilder.HasPostgresExtension("postgis");

        // ------------------------------
        // User Configuration
        // ------------------------------
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasOne(e => e.City).WithMany().HasForeignKey(e => e.CityId);
        });

        // ------------------------------
        // Ride Configuration
        // ------------------------------
        modelBuilder.Entity<Ride>(entity =>
        {
            // Spatial mapping: Geography with SRID 4326 (WGS84)
            entity.Property(e => e.StartPoint)
                  .HasColumnType("geography(Point, 4326)");

            // GiST spatial index for fast geographical searches
            entity.HasIndex(e => e.StartPoint)
                  .HasMethod("gist");

            entity.HasOne(e => e.City).WithMany().HasForeignKey(e => e.CityId);

            // Organizer relationship
            entity.HasOne(e => e.Organizer)
                  .WithMany(u => u.OrganizedRides)
                  .HasForeignKey(e => e.OrganizerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ------------------------------
        // Spot Configuration
        // ------------------------------
        modelBuilder.Entity<Spot>(entity =>
        {
            entity.Property(e => e.Location)
                  .HasColumnType("geography(Point, 4326)");

            entity.HasIndex(e => e.Location)
                  .HasMethod("gist");

            entity.HasOne(e => e.City).WithMany().HasForeignKey(e => e.CityId);
        });

        // ------------------------------
        // RideMember Configuration (Many-to-Many join entity)
        // ------------------------------
        modelBuilder.Entity<RideMember>(entity =>
        {
            entity.HasKey(e => new { e.RideId, e.UserId });

            entity.HasOne(e => e.Ride)
                  .WithMany(r => r.Members)
                  .HasForeignKey(e => e.RideId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.RideMemberships)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ------------------------------
        // Vehicle Configuration
        // ------------------------------
        modelBuilder.Entity<Vehicle>(entity =>
        {
            // JSON-B mapping for dynamic technical config
            entity.Property(e => e.TechnicalConfigJson)
                  .HasColumnType("jsonb");

            entity.HasOne(e => e.Owner)
                  .WithMany(u => u.Vehicles)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ------------------------------
        // Comment Configuration
        // ------------------------------
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasIndex(e => new { e.TargetType, e.TargetId });
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ------------------------------
        // Like Configuration
        // ------------------------------
        modelBuilder.Entity<Like>(entity =>
        {
            // Composite index for fast lookups
            entity.HasIndex(e => new { e.TargetType, e.TargetId });
            
            // Unique constraint: A user can only like a specific target once
            entity.HasIndex(e => new { e.UserId, e.TargetType, e.TargetId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ------------------------------
        // Conversation Configuration
        // ------------------------------
        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(e => new { e.ConversationId, e.UserId });
            entity.HasOne(e => e.Conversation)
                  .WithMany(c => c.Participants)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Conversations)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasOne(e => e.Conversation)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Sender)
                  .WithMany()
                  .HasForeignKey(e => e.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ------------------------------
        // UserBlock Configuration
        // ------------------------------
        modelBuilder.Entity<UserBlock>(entity =>
        {
            entity.HasKey(e => new { e.BlockerId, e.BlockedId });
            
            entity.HasOne(e => e.Blocker)
                  .WithMany(u => u.BlockedUsers)
                  .HasForeignKey(e => e.BlockerId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Blocked)
                  .WithMany(u => u.BlockedByUsers)
                  .HasForeignKey(e => e.BlockedId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ------------------------------
        // RideInvite Configuration
        // ------------------------------
        modelBuilder.Entity<RideInvite>(entity =>
        {
            entity.HasOne(e => e.Inviter)
                  .WithMany(u => u.SentInvites)
                  .HasForeignKey(e => e.InviterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Invitee)
                  .WithMany(u => u.ReceivedInvites)
                  .HasForeignKey(e => e.InviteeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Ride)
                  .WithMany()
                  .HasForeignKey(e => e.RideId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ------------------------------
        // Seed Data
        // ------------------------------
        modelBuilder.Entity<VehicleCategory>().HasData(
            new VehicleCategory { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Эндуро", Season = ProhvatApp.Domain.Enums.SeasonType.Summer, Metric = ProhvatApp.Domain.Enums.MetricType.Hours },
            new VehicleCategory { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Снегоходы", Season = ProhvatApp.Domain.Enums.SeasonType.Winter, Metric = ProhvatApp.Domain.Enums.MetricType.Kilometers },
            new VehicleCategory { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Боевая Классика", Season = ProhvatApp.Domain.Enums.SeasonType.AllSeason, Metric = ProhvatApp.Domain.Enums.MetricType.Kilometers },
            new VehicleCategory { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Питбайк", Season = ProhvatApp.Domain.Enums.SeasonType.Summer, Metric = ProhvatApp.Domain.Enums.MetricType.Hours },
            new VehicleCategory { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Квадроцикл", Season = ProhvatApp.Domain.Enums.SeasonType.AllSeason, Metric = ProhvatApp.Domain.Enums.MetricType.Kilometers }
        );
    }
}
