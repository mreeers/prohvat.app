using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Application.Parts.Queries;

public record GetPartsByCategoryQuery(Guid CategoryId) : IRequest<List<PartReviewDto>>;

public class PartReviewDto
{
    public Guid Id { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? VendorCode { get; set; }
    public string? MarketplaceLink { get; set; }
    public Guid AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetPartsByCategoryQueryHandler : IRequestHandler<GetPartsByCategoryQuery, List<PartReviewDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPartsByCategoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PartReviewDto>> Handle(GetPartsByCategoryQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PartReviews.AsNoTracking();
        
        if (request.CategoryId != Guid.Empty)
        {
            query = query.Where(p => p.VehicleCategoryId == request.CategoryId);
        }

        var parts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PartReviewDto
            {
                Id = p.Id,
                PartName = p.PartName,
                VendorCode = p.VendorCode,
                MarketplaceLink = p.MarketplaceLink,
                AuthorId = p.AuthorId,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return parts;
    }
}
