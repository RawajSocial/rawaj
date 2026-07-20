using MediatR;
using Rawaj.Application.Common.Exceptions;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Auth.Register;

public class RegisterCommandHandler(
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator,
    ITenantProvisioningService tenantProvisioningService,
    IRefreshTokenService refreshTokenService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await identityService.FindByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            return Result<RegisterResponse>.Failure("A user with this email already exists.");
        }

        var existingUserName = await identityService.FindByUserNameAsync(request.UserName, cancellationToken);
        if (existingUserName is not null)
        {
            return Result<RegisterResponse>.Failure("This username is already taken.");
        }

        Guid createdUserId = default;
        Guid tenantId = default;

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var registerResult = await identityService.CreateUserAsync(
                    request.Email,
                    request.UserName,
                    request.Password,
                    request.FullName,
                    request.PreferredLanguage,
                    ct);

                if (!registerResult.Succeeded)
                {
                    throw new RegistrationFailedException(string.Join(" ", registerResult.Errors));
                }

                createdUserId = registerResult.UserId;
                tenantId = await tenantProvisioningService.ProvisionPrivateTenantAsync(createdUserId, request.UserName, ct);
            }, cancellationToken);
        }
        catch (RegistrationFailedException ex)
        {
            return Result<RegisterResponse>.Failure(ex.Message);
        }

        var userDto = new ApplicationUserDto
        {
            Id = createdUserId,
            Email = request.Email,
            UserName = request.UserName,
            FullName = request.FullName,
            PreferredLanguage = request.PreferredLanguage,
            IsActive = true
        };

        var accessToken = jwtTokenGenerator.GenerateToken(userDto, out var accessTokenExpiresAt);
        var refreshToken = await refreshTokenService.IssueAsync(userDto.Id, cancellationToken);

        return Result<RegisterResponse>.Success(new RegisterResponse(
            userDto.Id, userDto.Email, userDto.UserName, userDto.FullName, accessToken, tenantId,
            refreshToken.RawToken, accessTokenExpiresAt, refreshToken.ExpiresAt));
    }
}
