using MediatR;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Application.Vehicles.Commands.CreateVehicle;

public record CreateVehicleCommand(
    Guid UserId, 
    Guid CategoryId, 
    string Brand, 
    string Model, 
    int Year, 
    string? TechnicalConfigJson,
    string? ImageUrl,
    ProhvatApp.Domain.Enums.OdometerType OdometerType,
    decimal MaintenanceInterval) : IRequest<Guid>;

public class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateVehicleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CategoryId = request.CategoryId,
            Brand = request.Brand,
            Model = request.Model,
            Year = request.Year,
            CurrentMetricsValue = 0,
            OdometerType = request.OdometerType,
            MaintenanceInterval = request.MaintenanceInterval,
            TechnicalConfigJson = request.TechnicalConfigJson,
            ImageUrl = request.ImageUrl
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync(cancellationToken);

        return vehicle.Id;
    }
}
