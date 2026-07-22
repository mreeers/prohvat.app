using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Threading.Tasks;
using ProhvatApp.Application.Location.Queries;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly IMediator _mediator;

    public LocationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities([FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetCitiesQuery(search));
        return Ok(result);
    }
}
