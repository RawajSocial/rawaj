using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Auth.Login;

public class LoginCommandHandler(
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenService refreshTokenService)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(request.EmailOrUserName, cancellationToken)
            ?? await identityService.FindByUserNameAsync(request.EmailOrUserName, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result<LoginResponse>.Failure("Invalid email or password.");
        }

        var passwordValid = await identityService.CheckPasswordAsync(user.Id, request.Password, cancellationToken);
        if (!passwordValid)
        {
            return Result<LoginResponse>.Failure("Invalid email or password.");
        }

        await identityService.UpdateLastLoginAsync(user.Id, cancellationToken);

        var accessToken = jwtTokenGenerator.GenerateToken(user, out var accessTokenExpiresAt);
        var refreshToken = await refreshTokenService.IssueAsync(user.Id, cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            user.Id, user.Email, user.UserName, user.FullName,
            accessToken, refreshToken.RawToken, accessTokenExpiresAt, refreshToken.ExpiresAt));
    }
}
