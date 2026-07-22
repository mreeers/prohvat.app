using MediatR;
using System;
using System.Collections.Generic;

namespace ProhvatApp.Application.Rides.Queries.GetMembers;

public class GetRideMembersQuery : IRequest<List<RideMemberDto>>
{
    public Guid RideId { get; set; }
    
    public GetRideMembersQuery(Guid rideId)
    {
        RideId = rideId;
    }
}

public class RideMemberDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
}
