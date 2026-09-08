using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProhvatApp.Application.Interactions.Commands.RemoveFriend;

public class RemoveFriendCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid TargetUserId { get; set; }

    public RemoveFriendCommand(Guid userId, Guid targetUserId)
    {
        UserId = userId;
        TargetUserId = targetUserId;
    }
}

public class RemoveFriendCommandHandler : IRequestHandler<RemoveFriendCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public RemoveFriendCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(RemoveFriendCommand request, CancellationToken cancellationToken)
    {
        var friendship = await _context.Friendships
            .FirstOrDefaultAsync(f => 
                (f.RequesterId == request.UserId && f.AddresseeId == request.TargetUserId) ||
                (f.RequesterId == request.TargetUserId && f.AddresseeId == request.UserId), 
                cancellationToken);

        if (friendship == null)
            return false;

        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
