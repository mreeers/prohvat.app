using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Threading.Tasks;
using ProhvatApp.Application.Spots.Commands;
using ProhvatApp.Application.Spots.Queries;
using System.Security.Claims;
using System;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpotsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpotsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("bounds")]
    public async Task<IActionResult> GetInBounds([FromQuery] double minLat, [FromQuery] double minLng, [FromQuery] double maxLat, [FromQuery] double maxLng, [FromQuery] SeasonType? season)
    {
        var result = await _mediator.Send(new GetSpotsInBoundsQuery(minLat, minLng, maxLat, maxLng, season));
        return Ok(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateSpotRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new CreateSpotCommand(
            userId,
            request.Title,
            request.Description,
            request.Latitude,
            request.Longitude,
            (ComplexityType)request.Complexity,
            (SeasonType)request.Season,
            request.CityId
        );
        var spotId = await _mediator.Send(command);
        return Ok(new { Id = spotId });
    }
}

public class CreateSpotRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Complexity { get; set; }
    public int Season { get; set; }
    public Guid? CityId { get; set; }
}
