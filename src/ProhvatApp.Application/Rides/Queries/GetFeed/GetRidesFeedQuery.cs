using MediatR;
using ProhvatApp.Application.Rides.Queries;
using ProhvatApp.Application.Rides.DTOs;
using ProhvatApp.Domain.Enums;
using System;
using System.Collections.Generic;

namespace ProhvatApp.Application.Rides.Queries.GetFeed;

public record GetRidesFeedQuery(
    Guid? CityId,
    SeasonType? Season,
    int Limit = 20
) : IRequest<List<RideDto>>;
