using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Application.Common.Interfaces;

public interface IVehicleLogRepository
{
    Task<VehicleLog> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<VehicleLog>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task AddAsync(VehicleLog log, CancellationToken cancellationToken = default);
    Task UpdateAsync(VehicleLog log, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
