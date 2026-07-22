using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;

namespace ProhvatApp.Application.Profile.Commands;

public record UnblockUserCommand(Guid BlockerId, Guid BlockedId) : IRequest<bool>;

public class UnblockUserCommandHandler : IRequestHandler<UnblockUserCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UnblockUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UnblockUserCommand request, CancellationToken cancellationToken)
    {
        var existingBlock = await _context.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerId == request.BlockerId && b.BlockedId == request.BlockedId, cancellationToken);

        if (existingBlock == null) return true;

        _context.UserBlocks.Remove(existingBlock);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
