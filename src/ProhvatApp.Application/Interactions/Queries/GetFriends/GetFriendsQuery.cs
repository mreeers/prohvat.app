using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProhvatApp.Application.Interactions.Queries.GetFriends;

public class GetFriendsQuery : IRequest<List<FriendDto>>
{
    public Guid UserId { get; set; }

    public GetFriendsQuery(Guid userId)
    {
        UserId = userId;
    }
}

public class FriendDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Status { get; set; } // 0 = Pending, 1 = Accepted
    public bool IsRequester { get; set; }
}

public class GetFriendsQueryHandler : IRequestHandler<GetFriendsQuery, List<FriendDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFriendsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FriendDto>> Handle(GetFriendsQuery request, CancellationToken cancellationToken)
    {
        var friendships = await _context.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => f.RequesterId == request.UserId || f.AddresseeId == request.UserId)
            .Where(f => f.Status != 2) // Exclude rejected/blocked
            .ToListAsync(cancellationToken);

        var result = new List<FriendDto>();
        foreach (var f in friendships)
        {
            var isRequester = f.RequesterId == request.UserId;
            var friendUser = isRequester ? f.Addressee : f.Requester;
            
            result.Add(new FriendDto
            {
                Id = friendUser.Id,
                Username = friendUser.Username,
                Status = f.Status,
                IsRequester = isRequester
            });
        }

        return result;
    }
}
