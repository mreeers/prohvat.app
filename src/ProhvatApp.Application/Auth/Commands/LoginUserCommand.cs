using MediatR;
using ProhvatApp.Application.Auth.DTOs;

namespace ProhvatApp.Application.Auth.Commands;

public record LoginUserCommand(string Email, string Password) : IRequest<AuthResultDto>;
