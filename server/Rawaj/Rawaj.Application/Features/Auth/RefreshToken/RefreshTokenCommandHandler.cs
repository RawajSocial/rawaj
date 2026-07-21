using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;

namespace Rawaj.Application.Features.Auth.RefreshToken;

public class RefreshTokenCommandHandler(
    IApplicationDbContext dbContext, IIdentityService identityService, IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var rotation = await RefreshTokenPolicy.RotateAsync(
            dbContext, request.RefreshToken, cancellationToken, jwtTokenGenerator.RefreshTokenExpiryDays);
        if (!rotation.Succeeded)
        {
            return Result<RefreshTokenResponse>.Failure(rotation.ErrorMessage!);
        }

        var (userId, newRawToken) = rotation.Data;

        var users = await identityService.FindByIdsAsync([userId], cancellationToken);
        var user = users.FirstOrDefault();
        if (user is null || !user.IsActive)
        {
            return Result<RefreshTokenResponse>.Failure("Account is no longer active.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = jwtTokenGenerator.GenerateToken(user);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(accessToken, newRawToken));
    }
}
