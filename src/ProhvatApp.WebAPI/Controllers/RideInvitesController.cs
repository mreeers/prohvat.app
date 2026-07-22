using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProhvatApp.Application.Rides.Commands;
using ProhvatApp.Application.Rides.Queries;
using ProhvatApp.Application.Rides.Queries.GetMyInvites;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RideInvitesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RideInvitesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("ride/{rideId}")]
    public async Task<IActionResult> GetRideInvites(Guid rideId)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var result = await _mediator.Send(new GetRideInvitesQuery(rideId, userId));
        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyInvites()
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var result = await _mediator.Send(new GetMyInvitesQuery(userId));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> InviteToRide([FromBody] InviteUserDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var result = await _mediator.Send(new InviteUserToRideCommand(dto.RideId, userId, dto.InviteeId));
        return Ok(new { Success = result });
    }

    [HttpPost("{id}/respond")]
    public async Task<IActionResult> RespondToInvite(Guid id, [FromBody] RespondInviteDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var result = await _mediator.Send(new RespondToRideInviteCommand(id, userId, dto.Accept));
        return Ok(new { Success = result });
    }
}

public class InviteUserDto
{
    public Guid RideId { get; set; }
    public Guid InviteeId { get; set; }
}

public class RespondInviteDto
{
    public bool Accept { get; set; }
}
