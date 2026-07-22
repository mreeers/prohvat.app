using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProhvatApp.Application.Interactions.Queries.GetLikesCount;

public record GetLikesCountQuery(Guid TargetId, TargetType TargetType) : IRequest<int>;

public class GetLikesCountQueryHandler : IRequestHandler<GetLikesCountQuery, int>
{
    private readonly IApplicationDbContext _context;

    public GetLikesCountQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(GetLikesCountQuery request, CancellationToken cancellationToken)
    {
        return await _context.Likes
            .CountAsync(l => l.TargetId == request.TargetId && l.TargetType == request.TargetType, cancellationToken);
    }
}
