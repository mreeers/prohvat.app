using MediatR;
using ProhvatApp.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProhvatApp.Application.Profile.Commands;

public class ToggleMapVisibilityCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public bool IsVisible { get; set; }

    public ToggleMapVisibilityCommand(Guid userId, bool isVisible)
    {
        UserId = userId;
        IsVisible = isVisible;
    }
}

public class ToggleMapVisibilityCommandHandler : IRequestHandler<ToggleMapVisibilityCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ToggleMapVisibilityCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ToggleMapVisibilityCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user == null) return false;

        user.IsVisibleOnMap = request.IsVisible;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
