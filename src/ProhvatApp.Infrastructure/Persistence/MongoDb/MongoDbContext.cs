using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Infrastructure.Persistence.MongoDb;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<VehicleLog> VehicleLogs => _database.GetCollection<VehicleLog>("VehicleLogs");
}
