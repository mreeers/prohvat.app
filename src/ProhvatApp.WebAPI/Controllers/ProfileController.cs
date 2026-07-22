using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Application.Profile.Commands;
using ProhvatApp.Application.Profile.Queries;
using ProhvatApp.Application.Interactions.Commands.AddFriend;
using ProhvatApp.Application.Interactions.Queries.GetFriends;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileService _fileService;

    public ProfileController(IMediator mediator, IFileService fileService)
    {
        _mediator = mediator;
        _fileService = fileService;
    }

    [HttpGet("{username}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProfile(string username)
    {
        var query = new GetUserProfileQuery(username);
        var profile = await _mediator.Send(query);
        
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileApiRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        string? avatarUrl = null;
        if (request.AvatarFile != null && request.AvatarFile.Length > 0)
        {
            avatarUrl = await _fileService.UploadFileAsync(request.AvatarFile.OpenReadStream(), request.AvatarFile.FileName, request.AvatarFile.ContentType);
        }

        var command = new UpdateProfileCommand(
            userId,
            request.Username,
            request.Bio,
            avatarUrl,
            request.CityId
        );
        
        try
        {
            var success = await _mediator.Send(command);
            if (!success) return NotFound();
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPut("location")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationApiRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var command = new UpdateLocationCommand(userId, request.Latitude, request.Longitude);
        await _mediator.Send(command);
        return Ok();
    }

    [HttpPost("friends/{addresseeId}")]
    public async Task<IActionResult> AddFriend(Guid addresseeId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var success = await _mediator.Send(new AddFriendCommand(userId, addresseeId));
        return Ok(new { success });
    }

    [HttpGet("{id}/friends")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFriends(Guid id)
    {
        var friends = await _mediator.Send(new GetFriendsQuery(id));
        return Ok(friends);
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> BlockUser(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _mediator.Send(new BlockUserCommand(userId, id));
        return Ok(new { Success = success });
    }

    [HttpPost("{id}/unblock")]
    public async Task<IActionResult> UnblockUser(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _mediator.Send(new UnblockUserCommand(userId, id));
        return Ok(new { Success = success });
    }

    [HttpPut("visibility")]
    public async Task<IActionResult> ToggleVisibility([FromBody] ToggleVisibilityApiRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var success = await _mediator.Send(new ToggleMapVisibilityCommand(userId, request.IsVisible));
        return Ok(new { success });
    }

    [HttpGet("active-riders")]
    public async Task<IActionResult> GetActiveRiders()
    {
        var riders = await _mediator.Send(new GetActiveRidersQuery());
        return Ok(riders);
    }
}

public class UpdateLocationApiRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class UpdateProfileApiRequest
{
    public string? Username { get; set; }
    public string? Bio { get; set; }
    public IFormFile? AvatarFile { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? CityId { get; set; }
}

public class ToggleVisibilityApiRequest
{
    public bool IsVisible { get; set; }
}
