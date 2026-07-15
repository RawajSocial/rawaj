using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Auth.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;
