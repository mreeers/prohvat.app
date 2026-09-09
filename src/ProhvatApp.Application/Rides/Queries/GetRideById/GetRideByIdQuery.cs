using System;
using MediatR;
using ProhvatApp.Application.Rides.DTOs;

namespace ProhvatApp.Application.Rides.Queries.GetRideById;

public record GetRideByIdQuery(Guid Id, Guid? CurrentUserId) : IRequest<RideDetailDto?>;
