using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Application.Parts.Commands;

public record CreatePartReviewCommand(
    Guid VehicleCategoryId,
    string PartName,
    string? VendorCode,
    string? MarketplaceLink,
    Guid AuthorId) : IRequest<Guid>;

public class CreatePartReviewCommandHandler : IRequestHandler<CreatePartReviewCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreatePartReviewCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreatePartReviewCommand request, CancellationToken cancellationToken)
    {
        var partReview = new PartReview
        {
            Id = Guid.NewGuid(),
            VehicleCategoryId = request.VehicleCategoryId,
            PartName = request.PartName,
            VendorCode = request.VendorCode,
            MarketplaceLink = request.MarketplaceLink,
            AuthorId = request.AuthorId,
            CreatedAt = DateTime.UtcNow
        };

        _context.PartReviews.Add(partReview);
        await _context.SaveChangesAsync(cancellationToken);

        return partReview.Id;
    }
}
