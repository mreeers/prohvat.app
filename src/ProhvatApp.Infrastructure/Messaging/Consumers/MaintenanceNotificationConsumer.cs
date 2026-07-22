using MassTransit;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;
using ProhvatApp.Domain.Events;

namespace ProhvatApp.Infrastructure.Messaging.Consumers;

public class MaintenanceNotificationConsumer : IConsumer<MaintenanceExceededEvent>
{
    private readonly IApplicationDbContext _context;

    public MaintenanceNotificationConsumer(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Consume(ConsumeContext<MaintenanceExceededEvent> context)
    {
        var ev = context.Message;
        
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = ev.UserId,
            Message = $"Пора обслуживать {ev.Brand} {ev.Model}! Пройден интервал ТО ({ev.CurrentMetricsValue} {(ev.OdometerType == Domain.Enums.OdometerType.MotoHours ? "м/ч" : "км")}).",
            Link = $"/garage/{ev.VehicleId}"
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(context.CancellationToken);
    }
}
