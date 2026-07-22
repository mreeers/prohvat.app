using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Rides.Commands;

public record CompleteRideCommand(Guid RideId, Guid OrganizerId, RideStatus Status, string? Report) : IRequest<bool>;

public class CompleteRideCommandHandler : IRequestHandler<CompleteRideCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public CompleteRideCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(CompleteRideCommand request, CancellationToken cancellationToken)
    {
        var ride = await _context.Rides.FirstOrDefaultAsync(r => r.Id == request.RideId && r.OrganizerId == request.OrganizerId, cancellationToken);
        if (ride == null) throw new UnauthorizedAccessException("Ride not found or you are not the organizer.");

        ride.Status = request.Status;
        ride.Report = request.Report;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
