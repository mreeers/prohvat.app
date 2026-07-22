using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;

namespace ProhvatApp.Application.Vehicles.Queries;

/// <summary>
/// UserId is optional — if null, fetch by vehicleId only (public view).
/// If provided, ensures the vehicle belongs to that user (ownership check).
/// </summary>
public record GetVehicleByIdQuery(Guid VehicleId, Guid? UserId) : IRequest<VehicleDetailDto?>;

public class VehicleDetailDto
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? TechnicalConfigJson { get; set; }
    public string? ImageUrl { get; set; }
    public decimal CurrentMetricsValue { get; set; }
    public ProhvatApp.Domain.Enums.OdometerType OdometerType { get; set; }
    public decimal MaintenanceInterval { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<VehicleLogDto> Logs { get; set; } = new();
}

public class VehicleLogDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public decimal MetricsValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}

public class GetVehicleByIdQueryHandler : IRequestHandler<GetVehicleByIdQuery, VehicleDetailDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IVehicleLogRepository _vehicleLogRepository;

    public GetVehicleByIdQueryHandler(IApplicationDbContext context, IVehicleLogRepository vehicleLogRepository)
    {
        _context = context;
        _vehicleLogRepository = vehicleLogRepository;
    }

    public async Task<VehicleDetailDto?> Handle(GetVehicleByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Vehicles
            .AsNoTracking()
            .Include(v => v.Category)
            .Where(v => v.Id == request.VehicleId);

        // If UserId provided, enforce ownership
        if (request.UserId.HasValue)
            query = query.Where(v => v.UserId == request.UserId.Value);

        var vehicle = await query.FirstOrDefaultAsync(cancellationToken);
        if (vehicle == null) return null;

        var mongoLogs = await _vehicleLogRepository.GetByVehicleIdAsync(vehicle.Id, cancellationToken);

        return new VehicleDetailDto
        {
            Id = vehicle.Id,
            OwnerId = vehicle.UserId,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Year = vehicle.Year,
            TechnicalConfigJson = vehicle.TechnicalConfigJson,
            ImageUrl = vehicle.ImageUrl,
            CurrentMetricsValue = vehicle.CurrentMetricsValue,
            OdometerType = vehicle.OdometerType,
            MaintenanceInterval = vehicle.MaintenanceInterval,
            CategoryName = vehicle.Category?.Name ?? string.Empty,
            Logs = mongoLogs
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new VehicleLogDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    Content = l.Content ?? string.Empty,
                    MetricsValue = l.MetricsValueAtLog,
                    CreatedAt = l.CreatedAt,
                    ImageUrls = l.ImageUrls ?? new List<string>()
                }).ToList()
        };
    }
}
