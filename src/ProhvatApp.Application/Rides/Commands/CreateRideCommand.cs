using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Rides.Commands;

public record CreateRideCommand(
    Guid OrganizerId,
    string Title,
    string Description,
    Guid TargetCategoryId,
    DateTime EventDate,
    ComplexityType Complexity,
    SeasonType Season,
    double Latitude,
    double Longitude,
    Guid? CityId,
    int MaxMembers = 10,
    string? GpxTrackPath = null) : IRequest<Guid>;

public class CreateRideCommandHandler : IRequestHandler<CreateRideCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateRideCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateRideCommand request, CancellationToken cancellationToken)
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var point = geometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));

        var ride = new Ride
        {
            Id = Guid.NewGuid(),
            OrganizerId = request.OrganizerId,
            Title = request.Title,
            Description = request.Description,
            TargetCategoryId = request.TargetCategoryId,
            EventDate = request.EventDate,
            Complexity = request.Complexity,
            Season = request.Season,
            Type = RideType.Friendly, // For MVP, all are friendly
            MaxMembers = request.MaxMembers,
            StartPoint = point,
            CityId = request.CityId,
            GpxTrackPath = request.GpxTrackPath ?? string.Empty
        };

        _context.Rides.Add(ride);

        // Add organizer as member
        _context.RideMembers.Add(new RideMember
        {
            RideId = ride.Id,
            UserId = request.OrganizerId,
            JoinedAt = DateTime.UtcNow
        });

        // Create Group Conversation for Ride
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = ProhvatApp.Domain.Enums.ConversationType.RideGroup,
            RideId = ride.Id,
            Title = request.Title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Conversations.Add(conversation);
        
        // Add organizer to conversation
        _context.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = request.OrganizerId,
            JoinedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return ride.Id;
    }
}
