using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Application.Profile.Commands;

public record BlockUserCommand(Guid BlockerId, Guid BlockedId) : IRequest<bool>;

public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public BlockUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        var existingBlock = await _context.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerId == request.BlockerId && b.BlockedId == request.BlockedId, cancellationToken);

        if (existingBlock != null) return true;

        _context.UserBlocks.Add(new UserBlock
        {
            BlockerId = request.BlockerId,
            BlockedId = request.BlockedId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
