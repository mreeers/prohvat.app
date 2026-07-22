using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Infrastructure.Persistence;
using ProhvatApp.Infrastructure.Auth;
using ProhvatApp.Infrastructure.Services;
using ProhvatApp.Infrastructure.Persistence.MongoDb;
using Microsoft.Extensions.Options;
using MassTransit;

namespace ProhvatApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.Configure<ProhvatApp.Infrastructure.Auth.JwtOptions>(configuration.GetSection(ProhvatApp.Infrastructure.Auth.JwtOptions.SectionName));
        services.AddSingleton<IJwtProvider, ProhvatApp.Infrastructure.Auth.JwtProvider>();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString,
                builder => builder.UseNetTopologySuite()));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        
        services.AddScoped<IFileService, MinioFileService>();

        services.AddScoped<IJwtProvider, JwtProvider>();

        // MongoDB
        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDB"));
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IVehicleLogRepository, MongoVehicleLogRepository>();

        // Redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetSection("Redis:ConnectionString").Value;
        });

        // MassTransit & RabbitMQ
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ProhvatApp.Infrastructure.Messaging.Consumers.MaintenanceNotificationConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration.GetSection("RabbitMQ:Host").Value ?? "localhost";
                var username = configuration.GetSection("RabbitMQ:Username").Value ?? "guest";
                var password = configuration.GetSection("RabbitMQ:Password").Value ?? "guest";

                cfg.Host(host, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });


        return services;
    }
}
