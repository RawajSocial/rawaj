using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Auth.Logout;

public class LogoutCommandHandler(IRefreshTokenService refreshTokenService)
    : IRequestHandler<LogoutCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        return Result<Unit>.Success(Unit.Value);
    }
}
