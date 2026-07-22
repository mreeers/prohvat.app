using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProhvatApp.Application.Interactions.Queries.GetComments;

public record GetCommentsQuery(Guid TargetId, TargetType TargetType) : IRequest<List<CommentDto>>;

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, List<CommentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCommentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Where(c => c.TargetId == request.TargetId && c.TargetType == request.TargetType)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                UserId = c.UserId,
                Username = c.User.Username,
                Text = c.Text,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
