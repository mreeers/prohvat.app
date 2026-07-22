using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProhvatApp.Application.Interactions.Commands.AddComment;
using ProhvatApp.Application.Interactions.Commands.DeleteComment;
using ProhvatApp.Application.Interactions.Commands.ToggleLike;
using ProhvatApp.Application.Interactions.Queries.GetComments;
using ProhvatApp.Application.Interactions.Queries.GetLikesCount;
using ProhvatApp.Domain.Enums;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InteractionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InteractionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("comments")]
    [Authorize]
    public async Task<IActionResult> AddComment([FromBody] AddCommentApiRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new AddCommentCommand(userId, request.TargetId, request.TargetType, request.Text);
        var id = await _mediator.Send(command);
        return Ok(new { Id = id });
    }

    [HttpDelete("comments/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new DeleteCommentCommand(id, userId);
        var result = await _mediator.Send(command);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpGet("comments/{targetType}/{targetId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(TargetType targetType, Guid targetId)
    {
        var query = new GetCommentsQuery(targetId, targetType);
        var comments = await _mediator.Send(query);
        return Ok(comments);
    }

    [HttpPost("likes/toggle")]
    [Authorize]
    public async Task<IActionResult> ToggleLike([FromBody] ToggleLikeApiRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new ToggleLikeCommand(userId, request.TargetId, request.TargetType);
        var isLiked = await _mediator.Send(command);
        return Ok(new { IsLiked = isLiked });
    }

    [HttpGet("likes/{targetType}/{targetId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLikesCount(TargetType targetType, Guid targetId)
    {
        var query = new GetLikesCountQuery(targetId, targetType);
        var count = await _mediator.Send(query);
        return Ok(new { Count = count });
    }
}

public class AddCommentApiRequest
{
    public Guid TargetId { get; set; }
    public TargetType TargetType { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class ToggleLikeApiRequest
{
    public Guid TargetId { get; set; }
    public TargetType TargetType { get; set; }
}
