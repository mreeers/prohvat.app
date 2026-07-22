using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProhvatApp.Application.Chat.Queries;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("messages/{rideId}")]
    public async Task<IActionResult> GetChatMessages(Guid rideId)
    {
        var query = new GetChatMessagesQuery(rideId);
        var messages = await _mediator.Send(query);
        return Ok(messages);
    }
}
