using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;
using MassTransit;
using ProhvatApp.Domain.Events;

namespace ProhvatApp.Application.Vehicles.Commands.AddVehicleLog;

public record AddVehicleLogCommand(
    Guid VehicleId,
    Guid UserId,
    string Title,
    string Content,
    decimal MetricsValue,
    List<string> ImageUrls) : IRequest<Guid>;

public class AddVehicleLogCommandHandler : IRequestHandler<AddVehicleLogCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IVehicleLogRepository _vehicleLogRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public AddVehicleLogCommandHandler(IApplicationDbContext context, IVehicleLogRepository vehicleLogRepository, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _vehicleLogRepository = vehicleLogRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Guid> Handle(AddVehicleLogCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.Category)
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.UserId == request.UserId, cancellationToken);
        if (vehicle == null) throw new UnauthorizedAccessException("Vehicle not found or you don't own it.");

        if (request.MetricsValue < vehicle.CurrentMetricsValue)
        {
            throw new ArgumentException("New metric value cannot be less than the current one.");
        }

        var log = new VehicleLog
        {
            Id = Guid.NewGuid(),
            VehicleId = request.VehicleId,
            Title = request.Title,
            Content = request.Content,
            MetricsValueAtLog = request.MetricsValue,
            CreatedAt = DateTime.UtcNow,
            ImageUrls = request.ImageUrls
        };

        if (vehicle.MaintenanceInterval > 0)
        {
            var oldIntervalsCount = Math.Floor(vehicle.CurrentMetricsValue / vehicle.MaintenanceInterval);
            var newIntervalsCount = Math.Floor(request.MetricsValue / vehicle.MaintenanceInterval);

            if (newIntervalsCount > oldIntervalsCount)
            {
                // Interval crossed!
                await _publishEndpoint.Publish(new MaintenanceExceededEvent
                {
                    VehicleId = vehicle.Id,
                    UserId = request.UserId,
                    Brand = vehicle.Brand,
                    Model = vehicle.Model,
                    CurrentMetricsValue = request.MetricsValue,
                    OdometerType = vehicle.OdometerType
                }, cancellationToken);
            }
        }

        vehicle.CurrentMetricsValue = request.MetricsValue; // Update vehicle's current mileage/hours
        
        await _vehicleLogRepository.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return log.Id;
    }
}
