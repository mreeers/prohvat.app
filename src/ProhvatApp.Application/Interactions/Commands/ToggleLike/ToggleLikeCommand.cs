using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;
using ProhvatApp.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProhvatApp.Application.Interactions.Commands.ToggleLike;

public record ToggleLikeCommand(Guid UserId, Guid TargetId, TargetType TargetType) : IRequest<bool>;

public class ToggleLikeCommandHandler : IRequestHandler<ToggleLikeCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IRealtimeNotificationService _notificationService;
    private readonly IVehicleLogRepository _vehicleLogRepository;

    public ToggleLikeCommandHandler(IApplicationDbContext context, IRealtimeNotificationService notificationService, IVehicleLogRepository vehicleLogRepository)
    {
        _context = context;
        _notificationService = notificationService;
        _vehicleLogRepository = vehicleLogRepository;
    }

    public async Task<bool> Handle(ToggleLikeCommand request, CancellationToken cancellationToken)
    {
        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.UserId == request.UserId && l.TargetId == request.TargetId && l.TargetType == request.TargetType, cancellationToken);

        if (existingLike != null)
        {
            _context.Likes.Remove(existingLike);
            await _context.SaveChangesAsync(cancellationToken);
            return false; // unliked
        }

        var like = new Like
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TargetId = request.TargetId,
            TargetType = request.TargetType,
            CreatedAt = DateTime.UtcNow
        };

        _context.Likes.Add(like);
        await _context.SaveChangesAsync(cancellationToken);

        // Send notification for new like
        var author = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        var authorName = author?.Name ?? "Кто-то";

        Guid? ownerId = null;
        string? url = null;
        string targetName = "вашу запись";

        if (request.TargetType == TargetType.VehicleLog)
        {
            var log = await _vehicleLogRepository.GetByIdAsync(request.TargetId, cancellationToken);
            if (log != null)
            {
                var vehicle = await _context.Vehicles.FindAsync(new object[] { log.VehicleId }, cancellationToken);
                if (vehicle != null)
                {
                    ownerId = vehicle.UserId;
                    url = $"/garage/{vehicle.Id}";
                    targetName = "ваш бортжурнал";
                }
            }
        }
        else if (request.TargetType == TargetType.Vehicle)
        {
            var v = await _context.Vehicles.FindAsync(new object[] { request.TargetId }, cancellationToken);
            if (v != null)
            {
                ownerId = v.UserId;
                url = $"/garage/{v.Id}";
                targetName = "ваш мотоцикл";
            }
        }
        else if (request.TargetType == TargetType.Ride)
        {
            var r = await _context.Rides.FindAsync(new object[] { request.TargetId }, cancellationToken);
            if (r != null)
            {
                ownerId = r.OrganizerId;
                url = $"/rides/{r.Id}";
                targetName = "вашу покатушку";
            }
        }
        else if (request.TargetType == TargetType.Spot)
        {
            var s = await _context.Spots.FindAsync(new object[] { request.TargetId }, cancellationToken);
            if (s != null)
            {
                ownerId = s.CreatorId;
                url = $"/spots/{s.Id}";
                targetName = "ваш спот";
            }
        }

        if (ownerId.HasValue && ownerId.Value != request.UserId)
        {
            await _notificationService.SendNotificationAsync(
                ownerId.Value,
                "Новый лайк",
                $"{authorName} оценил(а) {targetName}.",
                url
            );

            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = ownerId.Value,
                Message = $"{authorName} оценил(а) {targetName}.",
                Link = url ?? string.Empty,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true; // liked
    }
}
