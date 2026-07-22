using MediatR;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;
using ProhvatApp.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ProhvatApp.Application.Interactions.Commands.AddComment;

public record AddCommentCommand(Guid UserId, Guid TargetId, TargetType TargetType, string Text) : IRequest<Guid>;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IRealtimeNotificationService _notificationService;
    private readonly IVehicleLogRepository _vehicleLogRepository;

    public AddCommentCommandHandler(IApplicationDbContext context, IRealtimeNotificationService notificationService, IVehicleLogRepository vehicleLogRepository)
    {
        _context = context;
        _notificationService = notificationService;
        _vehicleLogRepository = vehicleLogRepository;
    }

    public async Task<Guid> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TargetId = request.TargetId,
            TargetType = request.TargetType,
            Text = request.Text,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        // Fetch user info for notification
        var author = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        var authorName = author?.Name ?? "Кто-то";

        // Determine target owner to send notification
        Guid? ownerId = null;
        string? url = null;
        string targetName = "вашей записи";

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
                    targetName = "вашем бортжурнале";
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
                targetName = "вашем мотоцикле";
            }
        }
        else if (request.TargetType == TargetType.Ride)
        {
            var r = await _context.Rides.FindAsync(new object[] { request.TargetId }, cancellationToken);
            if (r != null)
            {
                ownerId = r.OrganizerId;
                url = $"/rides/{r.Id}";
                targetName = "вашей покатушке";
            }
        }
        else if (request.TargetType == TargetType.Spot)
        {
            var s = await _context.Spots.FindAsync(new object[] { request.TargetId }, cancellationToken);
            if (s != null)
            {
                ownerId = s.CreatorId;
                url = $"/spots/{s.Id}";
                targetName = "вашем споте";
            }
        }

        // Send Real-time notification if owner is not the author
        if (ownerId.HasValue && ownerId.Value != request.UserId)
        {
            await _notificationService.SendNotificationAsync(
                ownerId.Value,
                "Новый комментарий",
                $"{authorName} оставил(а) комментарий к {targetName}.",
                url
            );
            
            // Also save to DB Notification table
            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = ownerId.Value,
                Message = $"{authorName} оставил(а) комментарий к {targetName}.",
                Link = url ?? string.Empty,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        return comment.Id;
    }
}
