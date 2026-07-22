using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProhvatApp.Application.Rides.Queries.GetMembers;

public class GetRideMembersQueryHandler : IRequestHandler<GetRideMembersQuery, List<RideMemberDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRideMembersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RideMemberDto>> Handle(GetRideMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await _context.RideMembers
            .Include(m => m.User)
            .Where(m => m.RideId == request.RideId)
            .Select(m => new RideMemberDto
            {
                UserId = m.UserId,
                Username = m.User.Username
            })
            .ToListAsync(cancellationToken);

        return members;
    }
}
