using System.Collections.Generic;
using MediatR;
using ProhvatApp.Application.Achievements.DTOs;

namespace ProhvatApp.Application.Achievements.Queries.GetUserAchievements;

public record GetUserAchievementsQuery(string Username) : IRequest<List<UserAchievementDto>>;
