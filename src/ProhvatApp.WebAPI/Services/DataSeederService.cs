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
using NetTopologySuite.Geometries;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;
using ProhvatApp.Domain.Enums;
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
        var logRepository = scope.ServiceProvider.GetService<IVehicleLogRepository>();

        try
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Migration exception during startup: {ex.Message}");
        }

        // 1. Seed Cities and Regions if needed
        await SeedRegionsAndCitiesAsync(context, cancellationToken);

        // 2. Seed Vehicle Categories if needed
        await SeedVehicleCategoriesAsync(context, cancellationToken);

        // 3. Seed Realistic Users, Garages, Posts, Friends and Interactions
        await SeedUsersAndSocialContentAsync(context, logRepository, cancellationToken);
    }

    private async Task SeedRegionsAndCitiesAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Regions.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already seeded with regions.");
            return;
        }

        _logger.LogInformation("Seeding regions and cities from JSON...");

        var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "russian-cities.json");
        if (!File.Exists(jsonPath))
        {
            jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "ProhvatApp.Infrastructure", "Persistence", "russian-cities.json");
        }

        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning($"russian-cities.json not found at {jsonPath}. Skipping city seed.");
            return;
        }

        try
        {
            var jsonStr = await File.ReadAllTextAsync(jsonPath, cancellationToken);
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
            _logger.LogInformation("Regions and cities seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while seeding cities");
        }
    }

    private async Task SeedVehicleCategoriesAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var requiredCategories = new List<(Guid Id, string Name, SeasonType Season, MetricType Metric)>
        {
            (Guid.Parse("11111111-0000-0000-0000-000000000001"), "Эндуро / Кросс", SeasonType.Summer, MetricType.Hours),
            (Guid.Parse("11111111-0000-0000-0000-000000000002"), "Боевая Классика / Дрифт", SeasonType.Winter, MetricType.Kilometers),
            (Guid.Parse("11111111-0000-0000-0000-000000000003"), "Снегоходы", SeasonType.Winter, MetricType.Hours),
            (Guid.Parse("11111111-0000-0000-0000-000000000004"), "Дорожный мотоцикл / Стрит", SeasonType.Summer, MetricType.Kilometers),
            (Guid.Parse("11111111-0000-0000-0000-000000000005"), "Квадроциклы / ATV", SeasonType.Summer, MetricType.Hours)
        };

        foreach (var cat in requiredCategories)
        {
            if (!await context.VehicleCategories.AnyAsync(c => c.Name == cat.Name, cancellationToken))
            {
                context.VehicleCategories.Add(new VehicleCategory
                {
                    Id = cat.Id,
                    Name = cat.Name,
                    Season = cat.Season,
                    Metric = cat.Metric
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAndSocialContentAsync(ApplicationDbContext context, IVehicleLogRepository? logRepo, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(u => u.Email == "alex@prohvat.app", cancellationToken))
        {
            await EnsureMongoLogsSeededAsync(logRepo, cancellationToken);
            await EnsureAchievementsSeededAsync(context, cancellationToken);
            await EnsureRideGpxAndTuningSeededAsync(context, cancellationToken);
            _logger.LogInformation("Demo users already exist. Skipping Postgres demo seeding.");
            return;
        }

        _logger.LogInformation("Seeding realistic demo riders, vehicles, feed posts, and interactions...");

        var ekbCity = await context.Cities.FirstOrDefaultAsync(c => c.Name.Contains("Екатеринбург"), cancellationToken);
        var mskCity = await context.Cities.FirstOrDefaultAsync(c => c.Name.Contains("Москва"), cancellationToken);
        var sochiCity = await context.Cities.FirstOrDefaultAsync(c => c.Name.Contains("Сочи"), cancellationToken);

        var enduroCat = await context.VehicleCategories.FirstOrDefaultAsync(c => c.Name.Contains("Эндуро"), cancellationToken);
        var driftCat = await context.VehicleCategories.FirstOrDefaultAsync(c => c.Name.Contains("Классика"), cancellationToken);
        var snowCat = await context.VehicleCategories.FirstOrDefaultAsync(c => c.Name.Contains("Снегоход"), cancellationToken);
        var streetCat = await context.VehicleCategories.FirstOrDefaultAsync(c => c.Name.Contains("Дорожный"), cancellationToken);

        // 1. Users
        var userAlex = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Алексей Гончаров",
            Username = "rider_alex",
            Email = "alex@prohvat.app",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("0102"),
            AvatarUrl = "https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?w=300&auto=format&fit=crop&q=80",
            Bio = "Катаю хард-эндуро на Урале и зимний дрифт на Жиге. Всегда готов к прохвату!",
            CityId = ekbCity?.Id,
            IsVisibleOnMap = true,
            LastKnownLocation = new Point(60.6057, 56.8389) { SRID = 4326 }
        };

        var userDaria = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Дарья Ветрова",
            Username = "daria_drift",
            Email = "daria@prohvat.app",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=300&auto=format&fit=crop&q=80",
            Bio = "Зимний дрифт на ВАЗ-2107 и городские прохваты на Ninja 650.",
            CityId = mskCity?.Id,
            IsVisibleOnMap = true,
            LastKnownLocation = new Point(37.6173, 55.7558) { SRID = 4326 }
        };

        var userIvan = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Иван Черепанов",
            Username = "ivan_enduro",
            Email = "ivan@prohvat.app",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=300&auto=format&fit=crop&q=80",
            Bio = "Husqvarna TE 300. Лес, бревна, каменные подъемы и сосновый бор.",
            CityId = ekbCity?.Id,
            IsVisibleOnMap = true,
            LastKnownLocation = new Point(60.6120, 56.8450) { SRID = 4326 }
        };

        var userSergey = new User
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Name = "Сергей Морозов",
            Username = "sergey_snow",
            Email = "sergey@prohvat.app",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            AvatarUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=300&auto=format&fit=crop&q=80",
            Bio = "Горный снегоходчик и путешественник. Красная Поляна.",
            CityId = sochiCity?.Id,
            IsVisibleOnMap = true,
            LastKnownLocation = new Point(40.2678, 43.6844) { SRID = 4326 }
        };

        context.Users.AddRange(userAlex, userDaria, userIvan, userSergey);

        // 2. Friendships (Mutual)
        context.Friendships.AddRange(
            new Friendship { Id = Guid.NewGuid(), RequesterId = userAlex.Id, AddresseeId = userDaria.Id, Status = 1 },
            new Friendship { Id = Guid.NewGuid(), RequesterId = userAlex.Id, AddresseeId = userIvan.Id, Status = 1 },
            new Friendship { Id = Guid.NewGuid(), RequesterId = userAlex.Id, AddresseeId = userSergey.Id, Status = 1 },
            new Friendship { Id = Guid.NewGuid(), RequesterId = userDaria.Id, AddresseeId = userIvan.Id, Status = 1 }
        );

        var existingOtherUsers = await context.Users
            .Where(u => u.Id != userAlex.Id && u.Id != userDaria.Id && u.Id != userIvan.Id && u.Id != userSergey.Id)
            .ToListAsync(cancellationToken);

        foreach (var other in existingOtherUsers)
        {
            context.Friendships.Add(new Friendship { Id = Guid.NewGuid(), RequesterId = other.Id, AddresseeId = userAlex.Id, Status = 1 });
            context.Friendships.Add(new Friendship { Id = Guid.NewGuid(), RequesterId = other.Id, AddresseeId = userDaria.Id, Status = 1 });
        }

        // 3. Vehicles
        var alexKtm = new Vehicle
        {
            Id = Guid.Parse("aaaa0001-0000-0000-0000-000000000001"),
            UserId = userAlex.Id,
            CategoryId = enduroCat!.Id,
            Brand = "KTM",
            Model = "300 EXC TPI",
            Year = 2023,
            CurrentMetricsValue = 42,
            OdometerType = OdometerType.MotoHours,
            MaintenanceInterval = 15,
            ImageUrl = "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=800&auto=format&fit=crop&q=80"
        };

        var alexVaz = new Vehicle
        {
            Id = Guid.Parse("aaaa0002-0000-0000-0000-000000000002"),
            UserId = userAlex.Id,
            CategoryId = driftCat!.Id,
            Brand = "ВАЗ",
            Model = "2105 Боевая Классика",
            Year = 2004,
            CurrentMetricsValue = 85400,
            OdometerType = OdometerType.Kilometers,
            MaintenanceInterval = 5000,
            TechnicalConfigJson = "{\"suspension\":\"Красноярский выворот We Ride JL\",\"diff\":\"Заварка пары 4.3\",\"brakes\":\"Гидроручник горизонтальный\",\"engine\":\"1.6 карбюратор Солекс\"}",
            ImageUrl = "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&auto=format&fit=crop&q=80"
        };

        var dariaVaz = new Vehicle
        {
            Id = Guid.Parse("bbbb0001-0000-0000-0000-000000000001"),
            UserId = userDaria.Id,
            CategoryId = driftCat!.Id,
            Brand = "ВАЗ",
            Model = "2107 Дрифт-пушка",
            Year = 2008,
            CurrentMetricsValue = 92000,
            OdometerType = OdometerType.Kilometers,
            MaintenanceInterval = 5000,
            TechnicalConfigJson = "{\"suspension\":\"Рычаги Турботема Дрифт\",\"diff\":\"Заварка 4.1\",\"engine\":\"1.7 инжектор, чип-тюнинг\"}",
            ImageUrl = "https://images.unsplash.com/photo-1617814076367-b759c7d7e738?w=800&auto=format&fit=crop&q=80"
        };

        var dariaMoto = new Vehicle
        {
            Id = Guid.Parse("bbbb0002-0000-0000-0000-000000000002"),
            UserId = userDaria.Id,
            CategoryId = streetCat!.Id,
            Brand = "Kawasaki",
            Model = "Ninja 650",
            Year = 2022,
            CurrentMetricsValue = 14200,
            OdometerType = OdometerType.Kilometers,
            MaintenanceInterval = 6000,
            ImageUrl = "https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?w=800&auto=format&fit=crop&q=80"
        };

        var ivanMoto = new Vehicle
        {
            Id = Guid.Parse("cccc0001-0000-0000-0000-000000000001"),
            UserId = userIvan.Id,
            CategoryId = enduroCat!.Id,
            Brand = "Husqvarna",
            Model = "TE 300 Heritage",
            Year = 2024,
            CurrentMetricsValue = 18,
            OdometerType = OdometerType.MotoHours,
            MaintenanceInterval = 15,
            ImageUrl = "https://images.unsplash.com/photo-1558980394-4c7c9299fe96?w=800&auto=format&fit=crop&q=80"
        };

        var sergeySnow = new Vehicle
        {
            Id = Guid.Parse("dddd0001-0000-0000-0000-000000000001"),
            UserId = userSergey.Id,
            CategoryId = snowCat!.Id,
            Brand = "BRP Ski-Doo",
            Model = "Summit Expert 850",
            Year = 2023,
            CurrentMetricsValue = 35,
            OdometerType = OdometerType.MotoHours,
            MaintenanceInterval = 25,
            ImageUrl = "https://images.unsplash.com/photo-1517055729445-fa7d27394b48?w=800&auto=format&fit=crop&q=80"
        };

        context.Vehicles.AddRange(alexKtm, alexVaz, dariaVaz, dariaMoto, ivanMoto, sergeySnow);

        // 4. Rides
        var rideEnduro = new Ride
        {
            Id = Guid.Parse("eeee0001-0000-0000-0000-000000000001"),
            OrganizerId = userAlex.Id,
            Title = "Субботний хард-эндуро сбор на карьере",
            Description = "Собираемся в субботу в 11:00 на карьере. План: разминка на бревнах, проезд по каменному ручью и штурм крутого подъема к вышке. Защита радиаторов и запасные свечи строго обязательны!",
            TargetCategoryId = enduroCat.Id,
            EventDate = DateTime.UtcNow.AddDays(2).Date.AddHours(11),
            Complexity = ComplexityType.Hard,
            Type = RideType.Friendly,
            Season = SeasonType.Summer,
            Status = RideStatus.Planned,
            MaxMembers = 10,
            CityId = ekbCity?.Id,
            StartPoint = new Point(60.6057, 56.8389) { SRID = 4326 }
        };

        var rideDrift = new Ride
        {
            Id = Guid.Parse("eeee0002-0000-0000-0000-000000000002"),
            OrganizerId = userDaria.Id,
            Title = "Зимний ночной дрифт-прохват по ледовому кольцу",
            Description = "Катаем парный дрифт на озере. Конфиг расчищен трактором, зацеп предсказуемый. Вход только на боевой классике или заднем приводе с заваркой. Чай в термосах берем с собой!",
            TargetCategoryId = driftCat.Id,
            EventDate = DateTime.UtcNow.AddDays(3).Date.AddHours(20),
            Complexity = ComplexityType.Middle,
            Type = RideType.Friendly,
            Season = SeasonType.Winter,
            Status = RideStatus.Planned,
            MaxMembers = 15,
            CityId = mskCity?.Id,
            StartPoint = new Point(37.6173, 55.7558) { SRID = 4326 }
        };

        context.Rides.AddRange(rideEnduro, rideDrift);

        // Add ride members
        context.RideMembers.AddRange(
            new RideMember { RideId = rideEnduro.Id, UserId = userAlex.Id, Status = MemberStatus.Approved },
            new RideMember { RideId = rideEnduro.Id, UserId = userIvan.Id, Status = MemberStatus.Approved },
            new RideMember { RideId = rideDrift.Id, UserId = userDaria.Id, Status = MemberStatus.Approved }
        );

        // 5. Part Reviews
        var pr1 = new PartReview
        {
            Id = Guid.NewGuid(),
            VehicleCategoryId = driftCat.Id,
            PartName = "Красноярский выворот We Ride JL",
            VendorCode = "WR-JL-2105",
            MarketplaceLink = "https://clubturbo.ru",
            AuthorId = userAlex.Id,
            LogId = null
        };

        var pr2 = new PartReview
        {
            Id = Guid.NewGuid(),
            VehicleCategoryId = enduroCat.Id,
            PartName = "Усиленная защита радиаторов Арма",
            VendorCode = "ARMA-KTM-300",
            MarketplaceLink = "https://armamoto.ru",
            AuthorId = userAlex.Id,
            LogId = null
        };

        context.PartReviews.AddRange(pr1, pr2);

        // 6. Comments & Likes
        var logAlexKtmId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var logDariaDriftId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var logSergeySnowId = Guid.Parse("10000000-0000-0000-0000-000000000003");

        context.Likes.AddRange(
            new Like { Id = Guid.NewGuid(), UserId = userDaria.Id, TargetId = logAlexKtmId, TargetType = TargetType.VehicleLog },
            new Like { Id = Guid.NewGuid(), UserId = userIvan.Id, TargetId = logAlexKtmId, TargetType = TargetType.VehicleLog },
            new Like { Id = Guid.NewGuid(), UserId = userSergey.Id, TargetId = logAlexKtmId, TargetType = TargetType.VehicleLog },
            new Like { Id = Guid.NewGuid(), UserId = userAlex.Id, TargetId = logDariaDriftId, TargetType = TargetType.VehicleLog },
            new Like { Id = Guid.NewGuid(), UserId = userIvan.Id, TargetId = logDariaDriftId, TargetType = TargetType.VehicleLog },
            new Like { Id = Guid.NewGuid(), UserId = userAlex.Id, TargetId = rideEnduro.Id, TargetType = TargetType.Ride },
            new Like { Id = Guid.NewGuid(), UserId = userIvan.Id, TargetId = rideEnduro.Id, TargetType = TargetType.Ride }
        );

        context.Comments.AddRange(
            new Comment
            {
                Id = Guid.NewGuid(),
                UserId = userIvan.Id,
                TargetId = logAlexKtmId,
                TargetType = TargetType.VehicleLog,
                Text = "Огонь кадры! В субботу обязательно повторим, я новые колодки поставил.",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Comment
            {
                Id = Guid.NewGuid(),
                UserId = userDaria.Id,
                TargetId = logAlexKtmId,
                TargetType = TargetType.VehicleLog,
                Text = "Нереальные скалы! Надо к вам на Урал выбраться покатать.",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new Comment
            {
                Id = Guid.NewGuid(),
                UserId = userAlex.Id,
                TargetId = logDariaDriftId,
                TargetType = TargetType.VehicleLog,
                Text = "На каком озере катали? Лед ровный или с торосами?",
                CreatedAt = DateTime.UtcNow.AddHours(-4)
            },
            new Comment
            {
                Id = Guid.NewGuid(),
                UserId = userSergey.Id,
                TargetId = logDariaDriftId,
                TargetType = TargetType.VehicleLog,
                Text = "Зимний дрифт на классике — топ! У нас в горах пока только пухляк валит.",
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            }
        );

        await context.SaveChangesAsync(cancellationToken);

        // 7. Seed VehicleLogs in Mongo
        if (logRepo != null)
        {
            try
            {
                var logs = GetDemoVehicleLogs();
                foreach (var log in logs)
                {
                    await logRepo.AddAsync(log, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not seed Mongo logs: {ex.Message}");
            }
        }

        _logger.LogInformation("Realistic demo riders and feed seeded successfully!");
    }

    private async Task EnsureMongoLogsSeededAsync(IVehicleLogRepository? logRepo, CancellationToken cancellationToken)
    {
        if (logRepo == null) return;
        try
        {
            var existingLogs = await logRepo.GetRecentLogsAsync(null, 1, cancellationToken);
            if (existingLogs != null && existingLogs.Any()) return;

            _logger.LogInformation("Seeding Mongo vehicle logs for demo vehicles...");
            var logs = GetDemoVehicleLogs();
            foreach (var log in logs)
            {
                await logRepo.AddAsync(log, cancellationToken);
            }
            _logger.LogInformation("Mongo demo logs seeded successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not seed Mongo logs in EnsureMongoLogsSeededAsync: {ex.Message}");
        }
    }

    private static List<VehicleLog> GetDemoVehicleLogs()
    {
        return new List<VehicleLog>
        {
            new VehicleLog
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                VehicleId = Guid.Parse("aaaa0001-0000-0000-0000-000000000001"),
                Title = "Хард-эндуро прохват на Чертово Городище: камни, бревна и грязевой подъем",
                Content = "Отлично закатили на выходных! Собрались вчетвером у карьера и пошли через сосновый бор на скалы. Новая резина Mitas 754 гребет феноменально. На подъеме с бревнами пришлось попотеть — один раз положил мот на бок, но защита радиаторов отработала на все 100%!",
                MetricsValueAtLog = 42,
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                LikeCount = 3,
                ImageUrls = new List<string>
                {
                    "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?w=800&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1558980394-4c7c9299fe96?w=800&auto=format&fit=crop&q=80"
                },
                VideoUrls = new List<string> { "https://www.youtube.com/embed/ScMzIvxBSi4" }
            },
            new VehicleLog
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                VehicleId = Guid.Parse("aaaa0002-0000-0000-0000-000000000002"),
                Title = "Первый ледовый заезд на озере: парный дрифт удался!",
                Content = "Лед встал, толщина 25 см — открыли зимний сезон! Выворот We Ride показал себя отлично, машина ставится в глубокий угол и легко контролируется газом. Заварили редуктор на 4.1, теперь третья передача крутится намного бодрее.",
                MetricsValueAtLog = 92000,
                CreatedAt = DateTime.UtcNow.AddHours(-6),
                LikeCount = 2,
                ImageUrls = new List<string>
                {
                    "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1617814076367-b759c7d7e738?w=800&auto=format&fit=crop&q=80"
                },
                VideoUrls = new List<string>()
            },
            new VehicleLog
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                VehicleId = Guid.Parse("dddd0001-0000-0000-0000-000000000001"),
                Title = "Свежий пухляк на Красной Поляне: открыли горный сезон 2026",
                Content = "Выпало почти 80 см свежего снега! Summit 850 в своей стихии: сайдхиллинг, прыжки с надувов и подъем по кулуарам. Температура -6°C, видимость отличная. Все системы работают штатно.",
                MetricsValueAtLog = 35,
                CreatedAt = DateTime.UtcNow.AddHours(-14),
                LikeCount = 1,
                ImageUrls = new List<string>
                {
                    "https://images.unsplash.com/photo-1517055729445-fa7d27394b48?w=800&auto=format&fit=crop&q=80"
                },
                VideoUrls = new List<string>()
            },
            new VehicleLog
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                VehicleId = Guid.Parse("cccc0001-0000-0000-0000-000000000001"),
                Title = "Обкатка новой Хаски: первые впечатления и замена масла на 15 м/ч",
                Content = "Сменил транспортировочное масло на Motorex Cross Power 2T. По подвеске WP XACT — пока самая мягкая и энергоемкая из всего, на чем доводилось катать. Поставил толстую камеру Michelin UHD 4mm назад, чтобы не бояться пробоев на острых камнях.",
                MetricsValueAtLog = 18,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                LikeCount = 0,
                ImageUrls = new List<string>
                {
                    "https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?w=800&auto=format&fit=crop&q=80"
                },
                VideoUrls = new List<string>()
            }
        };
    }

    private async Task EnsureAchievementsSeededAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (!await context.Achievements.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Seeding default achievements...");
                var achievements = new List<Achievement>
                {
                    new Achievement
                    {
                        Id = Guid.Parse("f0000001-0000-0000-0000-000000000001"),
                        Title = "Железная задница",
                        Description = "Преодолел более 150 км за один эпичный рейд без сходов",
                        Icon = "🏍️",
                        Category = "General",
                        Points = 50
                    },
                    new Achievement
                    {
                        Id = Guid.Parse("f0000002-0000-0000-0000-000000000002"),
                        Title = "Утопленник",
                        Description = "Затопил технику в броду, успешно эвакуировал и завел прямо в лесу",
                        Icon = "🌊",
                        Category = "Enduro",
                        Points = 30
                    },
                    new Achievement
                    {
                        Id = Guid.Parse("f0000003-0000-0000-0000-000000000003"),
                        Title = "Первый лед",
                        Description = "Открыл зимний сезон парным дрифтом по ледовому кольцу",
                        Icon = "❄️",
                        Category = "Drift",
                        Points = 40
                    },
                    new Achievement
                    {
                        Id = Guid.Parse("f0000004-0000-0000-0000-000000000004"),
                        Title = "Покоритель вершин",
                        Description = "Успешный подъем на скалы или горный кулуар в хард-эндуро или на снегоходе",
                        Icon = "🏔️",
                        Category = "Enduro",
                        Points = 50
                    },
                    new Achievement
                    {
                        Id = Guid.Parse("f0000005-0000-0000-0000-000000000005"),
                        Title = "Царь гаража",
                        Description = "Собрал боевой гараж из 2+ единиц техники с детальным тюнинг-конфигом",
                        Icon = "🔧",
                        Category = "General",
                        Points = 35
                    },
                    new Achievement
                    {
                        Id = Guid.Parse("f0000006-0000-0000-0000-000000000006"),
                        Title = "Лидер стаи",
                        Description = "Организовал групповой заезд и собрал команду проверенных райдеров",
                        Icon = "👥",
                        Category = "General",
                        Points = 45
                    }
                };

                context.Achievements.AddRange(achievements);
                await context.SaveChangesAsync(cancellationToken);
            }

            if (!await context.UserAchievements.AnyAsync(cancellationToken))
            {
                var users = await context.Users.ToListAsync(cancellationToken);
                var alex = users.FirstOrDefault(u => u.Email == "alex@prohvat.app");
                var daria = users.FirstOrDefault(u => u.Email == "daria@prohvat.app");
                var ivan = users.FirstOrDefault(u => u.Email == "ivan@prohvat.app");
                var sergey = users.FirstOrDefault(u => u.Email == "sergey@prohvat.app");

                var userAchievements = new List<UserAchievement>();
                if (alex != null)
                {
                    userAchievements.Add(new UserAchievement { UserId = alex.Id, AchievementId = Guid.Parse("f0000005-0000-0000-0000-000000000005"), EarnedAt = DateTime.UtcNow.AddDays(-10) });
                    userAchievements.Add(new UserAchievement { UserId = alex.Id, AchievementId = Guid.Parse("f0000006-0000-0000-0000-000000000006"), EarnedAt = DateTime.UtcNow.AddDays(-5) });
                    userAchievements.Add(new UserAchievement { UserId = alex.Id, AchievementId = Guid.Parse("f0000004-0000-0000-0000-000000000004"), EarnedAt = DateTime.UtcNow.AddDays(-2) });
                    userAchievements.Add(new UserAchievement { UserId = alex.Id, AchievementId = Guid.Parse("f0000003-0000-0000-0000-000000000003"), EarnedAt = DateTime.UtcNow.AddDays(-1) });
                }
                if (daria != null)
                {
                    userAchievements.Add(new UserAchievement { UserId = daria.Id, AchievementId = Guid.Parse("f0000003-0000-0000-0000-000000000003"), EarnedAt = DateTime.UtcNow.AddDays(-4) });
                    userAchievements.Add(new UserAchievement { UserId = daria.Id, AchievementId = Guid.Parse("f0000005-0000-0000-0000-000000000005"), EarnedAt = DateTime.UtcNow.AddDays(-8) });
                }
                if (ivan != null)
                {
                    userAchievements.Add(new UserAchievement { UserId = ivan.Id, AchievementId = Guid.Parse("f0000002-0000-0000-0000-000000000002"), EarnedAt = DateTime.UtcNow.AddDays(-14) });
                    userAchievements.Add(new UserAchievement { UserId = ivan.Id, AchievementId = Guid.Parse("f0000004-0000-0000-0000-000000000004"), EarnedAt = DateTime.UtcNow.AddDays(-3) });
                }
                if (sergey != null)
                {
                    userAchievements.Add(new UserAchievement { UserId = sergey.Id, AchievementId = Guid.Parse("f0000001-0000-0000-0000-000000000001"), EarnedAt = DateTime.UtcNow.AddDays(-20) });
                    userAchievements.Add(new UserAchievement { UserId = sergey.Id, AchievementId = Guid.Parse("f0000004-0000-0000-0000-000000000004"), EarnedAt = DateTime.UtcNow.AddDays(-6) });
                }

                context.UserAchievements.AddRange(userAchievements);
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Demo achievements assigned successfully!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not seed achievements: {ex.Message}");
        }
    }

    private async Task EnsureRideGpxAndTuningSeededAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Update rides with GPX tracks
            var rideEnduro = await context.Rides.FirstOrDefaultAsync(r => r.Id == Guid.Parse("eeee0001-0000-0000-0000-000000000001"), cancellationToken);
            if (rideEnduro != null && string.IsNullOrWhiteSpace(rideEnduro.GpxTrackPath))
            {
                rideEnduro.GpxTrackPath = GenerateEnduroGpx();
            }

            var rideDrift = await context.Rides.FirstOrDefaultAsync(r => r.Id == Guid.Parse("eeee0002-0000-0000-0000-000000000002"), cancellationToken);
            if (rideDrift != null && string.IsNullOrWhiteSpace(rideDrift.GpxTrackPath))
            {
                rideDrift.GpxTrackPath = GenerateDriftGpx();
            }

            // Update vehicles with tuning configs
            var vehicles = await context.Vehicles.ToListAsync(cancellationToken);
            foreach (var v in vehicles)
            {
                if (v.Brand == "KTM" && (string.IsNullOrWhiteSpace(v.TechnicalConfigJson) || v.TechnicalConfigJson == "{}"))
                {
                    v.TechnicalConfigJson = "{\"suspension\":\"WP XACT Pro (ревавлинг клапанов)\",\"protection\":\"Комплект Арма (радиаторы + картер)\",\"tires\":\"Mitas 754 + Tubliss\",\"exhaust\":\"FMF Titanium Powercore\",\"carburetor\":\"TPI инжектор + прошивка TSP\"}";
                }
                else if (v.Brand == "ВАЗ" && v.Model.Contains("2105") && (string.IsNullOrWhiteSpace(v.TechnicalConfigJson) || !v.TechnicalConfigJson.Contains("steering")))
                {
                    v.TechnicalConfigJson = "{\"steering\":\"Красноярский выворот\",\"differential\":\"Заварка 4.1\",\"handbrake\":\"Гидроручник в контур с регулятором\",\"engine\":\"16v 1.6 Шеснарь (130 л.с.)\",\"chassis\":\"Болтовой каркас, распорки стаканов\"}";
                }
                else if (v.Brand == "ВАЗ" && v.Model.Contains("2107") && (string.IsNullOrWhiteSpace(v.TechnicalConfigJson) || !v.TechnicalConfigJson.Contains("steering")))
                {
                    v.TechnicalConfigJson = "{\"steering\":\"Рычаги Турботема Дрифт\",\"differential\":\"Заварка 4.3\",\"handbrake\":\"Гидроручник Create Tech\",\"engine\":\"1.6 Карбюратор Спорт\",\"chassis\":\"Ковши Sparco, пружины Нива -50\"}";
                }
                else if (v.Brand.Contains("Ski-Doo") && (string.IsNullOrWhiteSpace(v.TechnicalConfigJson) || v.TechnicalConfigJson == "{}"))
                {
                    v.TechnicalConfigJson = "{\"track\":\"Зацеп 3.0 дюйма PowderMax\",\"skis\":\"Blade DS+ горные\",\"bumpers\":\"Усиленный бампер Voevoda\",\"riser\":\"Проставка руля 165 мм\"}";
                }
                else if (v.Brand == "Husqvarna" && (string.IsNullOrWhiteSpace(v.TechnicalConfigJson) || v.TechnicalConfigJson == "{}"))
                {
                    v.TechnicalConfigJson = "{\"suspension\":\"WP XACT закрытый картридж\",\"protection\":\"Защита радиаторов Hard Glide + картер Moose\",\"tires\":\"Michelin Enduro Medium + Mousse\",\"exhaust\":\"Сток Akrapovic\"}";
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Ride GPX tracks and tuning configs updated successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not update GPX and tuning configs: {ex.Message}");
        }
    }

    private static string GenerateEnduroGpx()
    {
        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<gpx version=""1.1"" creator=""Prohvat.app"" xmlns=""http://www.topografix.com/GPX/1/1"">
  <trk>
    <name>Хард-эндуро сбор на карьере</name>
    <trkseg>
      <trkpt lat=""56.8389"" lon=""60.6057""><ele>260</ele></trkpt>
      <trkpt lat=""56.8402"" lon=""60.6085""><ele>264</ele></trkpt>
      <trkpt lat=""56.8425"" lon=""60.6120""><ele>275</ele></trkpt>
      <trkpt lat=""56.8450"" lon=""60.6175""><ele>290</ele></trkpt>
      <trkpt lat=""56.8485"" lon=""60.6230""><ele>310</ele></trkpt>
      <trkpt lat=""56.8520"" lon=""60.6300""><ele>335</ele></trkpt>
      <trkpt lat=""56.8560"" lon=""60.6380""><ele>360</ele></trkpt>
      <trkpt lat=""56.8590"" lon=""60.6430""><ele>385</ele></trkpt>
      <trkpt lat=""56.8625"" lon=""60.6490""><ele>392</ele></trkpt>
      <trkpt lat=""56.8650"" lon=""60.6550""><ele>410</ele></trkpt>
      <trkpt lat=""56.8680"" lon=""60.6620""><ele>425</ele></trkpt>
      <trkpt lat=""56.8710"" lon=""60.6690""><ele>435</ele></trkpt>
      <trkpt lat=""56.8740"" lon=""60.6750""><ele>420</ele></trkpt>
      <trkpt lat=""56.8780"" lon=""60.6820""><ele>440</ele></trkpt>
    </trkseg>
  </trk>
</gpx>";
    }

    private static string GenerateDriftGpx()
    {
        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<gpx version=""1.1"" creator=""Prohvat.app"" xmlns=""http://www.topografix.com/GPX/1/1"">
  <trk>
    <name>Зимний ночной дрифт по ледовому кольцу</name>
    <trkseg>
      <trkpt lat=""55.7558"" lon=""37.6173""><ele>150</ele></trkpt>
      <trkpt lat=""55.7570"" lon=""37.6195""><ele>150</ele></trkpt>
      <trkpt lat=""55.7585"" lon=""37.6230""><ele>150</ele></trkpt>
      <trkpt lat=""55.7592"" lon=""37.6275""><ele>150</ele></trkpt>
      <trkpt lat=""55.7588"" lon=""37.6320""><ele>150</ele></trkpt>
      <trkpt lat=""55.7572"" lon=""37.6350""><ele>150</ele></trkpt>
      <trkpt lat=""55.7550"" lon=""37.6355""><ele>150</ele></trkpt>
      <trkpt lat=""55.7530"" lon=""37.6330""><ele>150</ele></trkpt>
      <trkpt lat=""55.7522"" lon=""37.6280""><ele>150</ele></trkpt>
      <trkpt lat=""55.7528"" lon=""37.6230""><ele>150</ele></trkpt>
      <trkpt lat=""55.7542"" lon=""37.6190""><ele>150</ele></trkpt>
      <trkpt lat=""55.7558"" lon=""37.6173""><ele>150</ele></trkpt>
    </trkseg>
  </trk>
</gpx>";
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
