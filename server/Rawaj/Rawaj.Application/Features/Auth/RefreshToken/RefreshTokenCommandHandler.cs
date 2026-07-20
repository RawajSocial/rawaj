using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Auth.RefreshToken;

public class RefreshTokenCommandHandler(
    IRefreshTokenService refreshTokenService,
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var validation = await refreshTokenService.ValidateAndRotateAsync(request.RefreshToken, cancellationToken);

        if (validation.Outcome != RefreshTokenOutcome.Success)
        {
            return Result<RefreshTokenResponse>.Failure("Invalid or expired refresh token.");
        }

        var user = await identityService.FindByIdAsync(validation.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result<RefreshTokenResponse>.Failure("Invalid or expired refresh token.");
        }

        var accessToken = jwtTokenGenerator.GenerateToken(user, out var accessTokenExpiresAt);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(
            accessToken,
            validation.NewRawToken!,
            accessTokenExpiresAt,
            validation.NewExpiresAt));
    }
}
