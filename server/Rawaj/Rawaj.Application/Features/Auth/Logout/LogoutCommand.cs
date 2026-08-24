using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Auth.Logout;

public record LogoutCommand(string RefreshToken) : IRequest<Result<bool>>;
