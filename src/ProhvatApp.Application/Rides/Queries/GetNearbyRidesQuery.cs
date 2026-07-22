using System.Collections.Generic;
using MediatR;
using ProhvatApp.Application.Rides.DTOs;

namespace ProhvatApp.Application.Rides.Queries;

public record GetNearbyRidesQuery(double Lat, double Lng, double RadiusInKm) : IRequest<List<RideDto>>;
