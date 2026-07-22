using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Domain.Entities;
using ProhvatApp.Infrastructure.Persistence;

namespace ProhvatApp.WebAPI.Services;

public class DataSeederService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataSeederService> _logger;

    public DataSeederService(IServiceProvider serviceProvider, ILogger<DataSeederService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure database is created and migrated
        await context.Database.MigrateAsync(cancellationToken);
        
        if (context.Regions.Any())
        {
            _logger.LogInformation("Database already seeded with regions.");
            return;
        }

        _logger.LogInformation("Seeding regions and cities from JSON...");

        // Try to locate russian-cities.json. It's in src/ProhvatApp.Infrastructure/Persistence
        // In the docker container, it might be in the root or in a specific path.
        // During build, we should copy it to the output directory.
        var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "russian-cities.json");
        
        if (!File.Exists(jsonPath))
        {
            // Fallback for local development
            jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "ProhvatApp.Infrastructure", "Persistence", "russian-cities.json");
        }

        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning($"russian-cities.json not found at {jsonPath}. Skipping seed.");
            return;
        }

        var jsonStr = await File.ReadAllTextAsync(jsonPath, cancellationToken);
        
        // Define DTO to match JSON
        var items = JsonSerializer.Deserialize<List<CityJsonDto>>(jsonStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        if (items == null) return;

        var regionsDict = new Dictionary<string, Region>();

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Subject) || string.IsNullOrWhiteSpace(item.Name)) continue;

            if (!regionsDict.TryGetValue(item.Subject, out var region))
            {
                region = new Region
                {
                    Id = Guid.NewGuid(),
                    Name = item.Subject,
                    District = item.District ?? ""
                };
                regionsDict[item.Subject] = region;
                context.Regions.Add(region);
            }

            if (double.TryParse(item.Coords?.Lat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(item.Coords?.Lon, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lon))
            {
                var city = new City
                {
                    Id = Guid.NewGuid(),
                    Name = item.Name,
                    RegionId = region.Id,
                    CenterLat = lat,
                    CenterLng = lon,
                    Population = item.Population
                };
                context.Cities.Add(city);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeding completed successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private class CityJsonDto
    {
        public CoordsDto Coords { get; set; }
        public string District { get; set; }
        public string Name { get; set; }
        public int Population { get; set; }
        public string Subject { get; set; }
    }

    private class CoordsDto
    {
        public string Lat { get; set; }
        public string Lon { get; set; }
    }
}
