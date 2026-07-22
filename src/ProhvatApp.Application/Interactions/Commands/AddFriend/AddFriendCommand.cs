using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProhvatApp.Application.Interactions.Commands.AddFriend;

public class AddFriendCommand : IRequest<bool>
{
    public Guid RequesterId { get; set; }
    public Guid AddresseeId { get; set; }

    public AddFriendCommand(Guid requesterId, Guid addresseeId)
    {
        RequesterId = requesterId;
        AddresseeId = addresseeId;
    }
}

public class AddFriendCommandHandler : IRequestHandler<AddFriendCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public AddFriendCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(AddFriendCommand request, CancellationToken cancellationToken)
    {
        if (request.RequesterId == request.AddresseeId) return false;

        var existing = await _context.Friendships
            .FirstOrDefaultAsync(f => 
                (f.RequesterId == request.RequesterId && f.AddresseeId == request.AddresseeId) ||
                (f.RequesterId == request.AddresseeId && f.AddresseeId == request.RequesterId), 
                cancellationToken);

        if (existing != null)
        {
            if (existing.Status == 0 && existing.AddresseeId == request.RequesterId)
            {
                // Accept friend request
                existing.Status = 1;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false; // Already requested or accepted
        }

        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            RequesterId = request.RequesterId,
            AddresseeId = request.AddresseeId,
            Status = 0 // Pending
        };

        _context.Friendships.Add(friendship);
        await _context.SaveChangesAsync(cancellationToken);
        
        return true;
    }
}
