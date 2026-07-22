using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;

namespace ProhvatApp.Application.Vehicles.Commands;

public record UpdateVehicleConfigCommand(Guid VehicleId, Guid UserId, string TechnicalConfigJson) : IRequest;

public class UpdateVehicleConfigCommandHandler : IRequestHandler<UpdateVehicleConfigCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateVehicleConfigCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateVehicleConfigCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken);
        
        if (vehicle == null || vehicle.UserId != request.UserId)
            throw new Exception("Vehicle not found or unauthorized");

        vehicle.TechnicalConfigJson = request.TechnicalConfigJson;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
