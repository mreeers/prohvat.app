using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProhvatApp.Application.Feed.Queries.GetFeed;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeedController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeed(
        [FromQuery] bool onlyFriends = false,
        [FromQuery] SeasonType? season = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        Guid? currentUserId = null;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var parsedId))
        {
            currentUserId = parsedId;
        }

        var query = new GetFeedQuery(onlyFriends, season, currentUserId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
