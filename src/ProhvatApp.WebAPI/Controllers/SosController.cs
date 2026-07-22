using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProhvatApp.Application.Sos.Commands;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SosController : ControllerBase
{
    private readonly IMediator _mediator;

    public SosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSosSignal([FromBody] CreateSosSignalRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var command = new CreateSosSignalCommand(userId, request.Latitude, request.Longitude, request.Message);
        var sosId = await _mediator.Send(command);

        return Ok(new { Id = sosId });
    }
}

public class CreateSosSignalRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Message { get; set; } = string.Empty;
}
