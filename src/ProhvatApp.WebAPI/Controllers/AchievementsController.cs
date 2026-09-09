using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProhvatApp.Application.Achievements.Queries.GetUserAchievements;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AchievementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AchievementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("user/{username}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserAchievements(string username)
    {
        var result = await _mediator.Send(new GetUserAchievementsQuery(username));
        return Ok(result);
    }
}
