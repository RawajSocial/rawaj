using MediatR;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Auth.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<RefreshTokenResponse>>;
