using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;

namespace Rawaj.Application.Features.Auth.Login;

public class LoginCommandHandler(
    IIdentityService identityService, IJwtTokenGenerator jwtTokenGenerator, IApplicationDbContext dbContext)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(request.Email, cancellationToken);
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

        var accessToken = jwtTokenGenerator.GenerateToken(user);
        var refreshToken = RefreshTokenPolicy.Issue(dbContext, user.Id, jwtTokenGenerator.RefreshTokenExpiryDays);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(user.Id, user.Email, user.FullName, accessToken, refreshToken));
    }
}
