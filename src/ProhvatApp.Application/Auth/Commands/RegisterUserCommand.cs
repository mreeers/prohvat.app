using MediatR;
using ProhvatApp.Application.Auth.DTOs;

namespace ProhvatApp.Application.Auth.Commands;

public record RegisterUserCommand(string Name, string Email, string Password) : IRequest<AuthResultDto>;
