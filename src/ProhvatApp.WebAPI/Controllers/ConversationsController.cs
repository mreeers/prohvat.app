using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProhvatApp.Application.Chat.Commands;
using ProhvatApp.Application.Chat.Queries;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConversationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetConversations()
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var result = await _mediator.Send(new GetConversationsQuery(userId));
        return Ok(result);
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(Guid id)
    {
        var result = await _mediator.Send(new GetChatMessagesQuery(id));
        return Ok(result);
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var result = await _mediator.Send(new SendChatMessageCommand(id, userId, dto.Text));
        
        if (result == null)
            return BadRequest(new { Message = "Cannot send message to this conversation." });
            
        return Ok(result);
    }
}

public class SendMessageDto
{
    public string Text { get; set; } = string.Empty;
}
