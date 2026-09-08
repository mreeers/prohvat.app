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
            _logger.LogInformation("Demo users already exist. Skipping demo seeding.");
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
                var logs = new List<VehicleLog>
                {
                    new VehicleLog
                    {
                        Id = logAlexKtmId,
                        VehicleId = alexKtm.Id,
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
                        Id = logDariaDriftId,
                        VehicleId = dariaVaz.Id,
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
                        Id = logSergeySnowId,
                        VehicleId = sergeySnow.Id,
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
                        VehicleId = ivanMoto.Id,
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
