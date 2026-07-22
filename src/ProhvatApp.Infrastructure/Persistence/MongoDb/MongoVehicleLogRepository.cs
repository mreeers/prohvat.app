using MongoDB.Driver;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Infrastructure.Persistence.MongoDb;

public class MongoVehicleLogRepository : IVehicleLogRepository
{
    private readonly MongoDbContext _context;

    public MongoVehicleLogRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<VehicleLog> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.VehicleLogs.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<VehicleLog>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        return await _context.VehicleLogs.Find(x => x.VehicleId == vehicleId).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(VehicleLog log, CancellationToken cancellationToken = default)
    {
        await _context.VehicleLogs.InsertOneAsync(log, new InsertOneOptions(), cancellationToken);
    }

    public async Task UpdateAsync(VehicleLog log, CancellationToken cancellationToken = default)
    {
        await _context.VehicleLogs.ReplaceOneAsync(x => x.Id == log.Id, log, new ReplaceOptions(), cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _context.VehicleLogs.DeleteOneAsync(x => x.Id == id, cancellationToken);
    }
}
