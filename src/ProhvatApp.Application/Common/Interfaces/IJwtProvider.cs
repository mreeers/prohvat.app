using ProhvatApp.Domain.Entities;

namespace ProhvatApp.Application.Common.Interfaces;

public interface IJwtProvider
{
    string GenerateToken(User user);
}
