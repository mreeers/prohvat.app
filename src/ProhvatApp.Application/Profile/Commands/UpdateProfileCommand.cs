using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Common.Interfaces;

namespace ProhvatApp.Application.Profile.Commands;

public record UpdateProfileCommand(
    Guid UserId,
    string? Username,
    string? Bio,
    string? AvatarUrl,
    Guid? CityId) : IRequest<bool>;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateProfileCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user == null) return false;

        // Check uniqueness if changing username
        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
        {
            var exists = await _context.Users.AnyAsync(u => u.Username == request.Username, cancellationToken);
            if (exists)
            {
                throw new Exception("Username is already taken.");
            }
            user.Username = request.Username;
        }

        user.Bio = request.Bio;
        if (!string.IsNullOrEmpty(request.AvatarUrl))
        {
            user.AvatarUrl = request.AvatarUrl;
        }
        // Only update CityId if city actually exists (or null to clear it)
        if (request.CityId.HasValue)
        {
            var cityExists = await _context.Cities.AnyAsync(c => c.Id == request.CityId.Value, cancellationToken);
            if (cityExists)
                user.CityId = request.CityId;
        }
        else
        {
            user.CityId = null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
