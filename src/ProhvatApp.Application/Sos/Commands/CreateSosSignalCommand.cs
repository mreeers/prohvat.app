using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Application.Sos.Commands;

public record CreateSosSignalCommand(Guid UserId, double Latitude, double Longitude, string Message) : IRequest<Guid>;

public class CreateSosSignalCommandHandler : IRequestHandler<CreateSosSignalCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IRealtimeNotificationService _notificationService;

    public CreateSosSignalCommandHandler(IApplicationDbContext context, IRealtimeNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Guid> Handle(CreateSosSignalCommand request, CancellationToken cancellationToken)
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var location = geometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));

        var sos = new SosSignal
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Location = location,
            Message = request.Message,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.SosSignals.Add(sos);
        await _context.SaveChangesAsync(cancellationToken);

        // Find users within 15 km
        // Note: 15000 meters
        var usersToNotify = await _context.Users
            .Where(u => u.Id != request.UserId && u.LastKnownLocation != null && u.LastKnownLocation.IsWithinDistance(location, 15000))
            .ToListAsync(cancellationToken);

        var sender = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        var senderName = sender?.Username ?? "Unknown User";

        foreach (var user in usersToNotify)
        {
            var notif = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Message = $"🚨 SOS от @{senderName}! {request.Message}",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Link = $"/map?lat={request.Latitude}&lng={request.Longitude}&zoom=15"
            };
            _context.Notifications.Add(notif);
            
            // Broadcast via SignalR
            await _notificationService.SendNotificationAsync(user.Id, "SOS", notif.Message, notif.Link);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return sos.Id;
    }
}
