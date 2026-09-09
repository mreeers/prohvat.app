using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProhvatApp.Application.Achievements.DTOs;
using ProhvatApp.Application.Common.Interfaces;

namespace ProhvatApp.Application.Achievements.Queries.GetUserAchievements;

public class GetUserAchievementsQueryHandler : IRequestHandler<GetUserAchievementsQuery, List<UserAchievementDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserAchievementsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserAchievementDto>> Handle(GetUserAchievementsQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        var allAchievements = await _context.Achievements
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var userAchievements = user != null
            ? await _context.UserAchievements
                .AsNoTracking()
                .Where(ua => ua.UserId == user.Id)
                .ToDictionaryAsync(ua => ua.AchievementId, ua => ua.EarnedAt, cancellationToken)
            : new Dictionary<System.Guid, System.DateTime>();

        return allAchievements.Select(a =>
        {
            var isEarned = userAchievements.ContainsKey(a.Id);
            return new UserAchievementDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                Icon = a.Icon,
                Category = a.Category,
                Points = a.Points,
                IsEarned = isEarned,
                EarnedAt = isEarned ? userAchievements[a.Id] : null
            };
        }).OrderByDescending(a => a.IsEarned)
          .ThenBy(a => a.Title)
          .ToList();
    }
}
