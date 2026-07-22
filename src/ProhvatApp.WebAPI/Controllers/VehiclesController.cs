using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Application.Vehicles.Commands.AddVehicleLog;
using ProhvatApp.Application.Vehicles.Commands.CreateVehicle;
using ProhvatApp.Application.Vehicles.Queries.GetMyVehicles;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileService _fileService;
    private readonly ILogger<VehiclesController> _logger;
    private readonly Microsoft.Extensions.Caching.Distributed.IDistributedCache _cache;

    public VehiclesController(IMediator mediator, IFileService fileService, ILogger<VehiclesController> logger, Microsoft.Extensions.Caching.Distributed.IDistributedCache cache)
    {
        _mediator = mediator;
        _fileService = fileService;
        _logger = logger;
        _cache = cache;
    }

    // ── Public endpoints ───────────────────────────────────────────────────────

    /// <summary>Returns vehicle categories (public)</summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        var cacheKey = "vehicle_categories";
        var cachedData = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<object[]>(cachedData));
        }

        var categories = new[] {
            new { Id = "11111111-1111-1111-1111-111111111111", Name = "Эндуро" },
            new { Id = "22222222-2222-2222-2222-222222222222", Name = "Снегоходы" },
            new { Id = "33333333-3333-3333-3333-333333333333", Name = "Боевая Классика" },
            new { Id = "44444444-4444-4444-4444-444444444444", Name = "Питбайк" },
            new { Id = "55555555-5555-5555-5555-555555555555", Name = "Квадроцикл" }
        };

        var options = new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(1));

        await _cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(categories), options);

        return Ok(categories);
    }

    /// <summary>Get vehicle by id (public — anyone can view a bike's logbook)</summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVehicle(Guid id)
    {
        // UserId = null means "any owner" — show to everyone
        var vehicle = await _mediator.Send(new ProhvatApp.Application.Vehicles.Queries.GetVehicleByIdQuery(id, null));
        if (vehicle == null) return NotFound();
        return Ok(vehicle);
    }

    // ── Authenticated endpoints ────────────────────────────────────────────────

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyVehicles()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var vehicles = await _mediator.Send(new GetMyVehiclesQuery(userId));
        return Ok(vehicles);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateVehicle([FromForm] CreateVehicleApiRequest request)
    {
        try
        {
            _logger.LogInformation("Creating vehicle {Brand} {Model}", request.Brand, request.Model);
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            string? imageUrl = null;
            if (request.Image != null && request.Image.Length > 0)
            {
                imageUrl = await _fileService.UploadFileAsync(request.Image.OpenReadStream(), request.Image.FileName, request.Image.ContentType);
            }

            var command = new CreateVehicleCommand(
                userId, 
                request.CategoryId, 
                request.Brand, 
                request.Model, 
                request.Year, 
                request.TechnicalConfigJson, 
                imageUrl,
                (ProhvatApp.Domain.Enums.OdometerType)request.OdometerType,
                request.MaintenanceInterval);
            var id = await _mediator.Send(command);
            return Ok(new { Id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>Add logbook entry — requires auth and ownership</summary>
    [HttpPost("{id}/logs")]
    [Authorize]
    public async Task<IActionResult> AddVehicleLog(Guid id, [FromForm] AddVehicleLogApiRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var imageUrls = new List<string>();
        if (request.Images != null)
        {
            foreach (var img in request.Images)
            {
                if (img.Length > 0)
                {
                    var url = await _fileService.UploadFileAsync(img.OpenReadStream(), img.FileName, img.ContentType);
                    imageUrls.Add(url);
                }
            }
        }

        var command = new AddVehicleLogCommand(id, userId, request.Title, request.Content, request.MetricsValue, imageUrls);
        var logId = await _mediator.Send(command);
        return Ok(new { Id = logId });
    }

    [HttpPut("{id}/config")]
    [Authorize]
    public async Task<IActionResult> UpdateConfig(Guid id, [FromBody] UpdateConfigRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new ProhvatApp.Application.Vehicles.Commands.UpdateVehicleConfigCommand(id, userId, request.TechnicalConfigJson);
        await _mediator.Send(command);
        return Ok();
    }
}

public class CreateVehicleApiRequest
{
    public Guid CategoryId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? TechnicalConfigJson { get; set; }
    public IFormFile? Image { get; set; }
    public int OdometerType { get; set; } = 1;
    public decimal MaintenanceInterval { get; set; }
}

public class AddVehicleLogApiRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public decimal MetricsValue { get; set; }
    public List<IFormFile>? Images { get; set; }
}

public class UpdateConfigRequest
{
    public string TechnicalConfigJson { get; set; } = "{}";
}
