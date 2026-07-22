using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProhvatApp.Application.Rides.Commands;
using ProhvatApp.Application.Rides.Queries;
using ProhvatApp.Application.Rides.Queries.GetMyRides;
using ProhvatApp.Application.Rides.Queries.GetFeed;
using ProhvatApp.Application.Rides.Queries.GetMembers;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RidesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RidesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("bounds")]
    public async Task<IActionResult> GetRidesInBounds([FromQuery] double minLat, [FromQuery] double minLng, [FromQuery] double maxLat, [FromQuery] double maxLng, [FromQuery] SeasonType? season)
    {
        var query = new GetRidesInBoundsQuery(minLat, minLng, maxLat, maxLng, season);
        var rides = await _mediator.Send(query);
        return Ok(rides);
    }

    [HttpGet("feed")]
    public async Task<IActionResult> GetRidesFeed([FromQuery] Guid? cityId, [FromQuery] SeasonType? season, [FromQuery] int limit = 20)
    {
        var query = new GetRidesFeedQuery(cityId, season, limit);
        var rides = await _mediator.Send(query);
        return Ok(rides);
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyRides()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new GetMyRidesQuery(userId));
        return Ok(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateRide([FromBody] CreateRideRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new CreateRideCommand(
            userId,
            request.Title,
            request.Description,
            request.TargetCategoryId,
            request.EventDate,
            (ComplexityType)request.Complexity,
            (SeasonType)request.Season,
            request.Latitude,
            request.Longitude,
            request.CityId,
            request.MaxMembers
        );

        var id = await _mediator.Send(command);
        return Ok(new { Id = id });
    }

    [Authorize]
    [HttpPost("{id}/join")]
    public async Task<IActionResult> JoinRide(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new JoinRideCommand(id, userId);
        
        try 
        {
            await _mediator.Send(command);
            return Ok();
        }
        catch(Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("{id}/membership")]
    public async Task<IActionResult> CheckMembership(Guid id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var isMember = await _mediator.Send(new GetRideMembershipQuery(id, userId));
        return Ok(new { isMember });
    }

    [AllowAnonymous]
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        var members = await _mediator.Send(new GetRideMembersQuery(id));
        return Ok(members);
    }

    [Authorize]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateRideStatusRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var command = new CompleteRideCommand(id, userId, (RideStatus)request.Status, request.Report);
        try
        {
            await _mediator.Send(command);
            return Ok();
        }
        catch(UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}

public class UpdateRideStatusRequest
{
    public int Status { get; set; }
    public string? Report { get; set; }
}

public class CreateRideRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TargetCategoryId { get; set; }
    public DateTime EventDate { get; set; }
    public int Complexity { get; set; }
    public int Season { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Guid? CityId { get; set; }
    public int MaxMembers { get; set; } = 10;
}
