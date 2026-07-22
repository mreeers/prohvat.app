using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProhvatApp.Application.Parts.Commands;
using ProhvatApp.Application.Parts.Queries;

namespace ProhvatApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PartsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PartsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetParts([FromQuery] Guid categoryId)
    {
        var query = new GetPartsByCategoryQuery(categoryId);
        var parts = await _mediator.Send(query);
        return Ok(parts);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePart([FromBody] CreatePartRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new CreatePartReviewCommand(
            request.VehicleCategoryId,
            request.PartName,
            request.VendorCode,
            request.MarketplaceLink,
            userId);

        var id = await _mediator.Send(command);
        return Ok(new { Id = id });
    }
}

public class CreatePartRequest
{
    public Guid VehicleCategoryId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? VendorCode { get; set; }
    public string? MarketplaceLink { get; set; }
}
