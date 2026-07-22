using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Application.Vehicles.Queries.GetMyVehicles;

public record VehicleDto(Guid Id, string Brand, string Model, int Year, decimal CurrentMetricsValue, string CategoryName);

public record GetMyVehiclesQuery(Guid UserId) : IRequest<List<VehicleDto>>;

public class GetMyVehiclesQueryHandler : IRequestHandler<GetMyVehiclesQuery, List<VehicleDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMyVehiclesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<VehicleDto>> Handle(GetMyVehiclesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Vehicles
            .Include(v => v.Category)
            .Where(v => v.UserId == request.UserId)
            .Select(v => new VehicleDto(v.Id, v.Brand, v.Model, v.Year, v.CurrentMetricsValue, v.Category.Name))
            .ToListAsync(cancellationToken);
    }
}
