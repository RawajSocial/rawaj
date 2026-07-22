using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Common.Services;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Auth.Register;

public class RegisterCommandHandler(
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator,
    IApplicationDbContext dbContext,
    TenantProvisioningService tenantProvisioningService)
    : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await identityService.FindByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            return Result<RegisterResponse>.Failure("A user with this email already exists.");
        }
        
        var existingUsername = await identityService.FindByUsernameAsync(request.Username, cancellationToken);
        if (existingUsername is not null)
        {
            return Result<RegisterResponse>.Failure("A user with this username already exists.");
        }

        var registerResult = await identityService.CreateUserAsync(
            request.Email,
            request.Username,
            request.Password,
            request.FullName,
            request.PreferredLanguage,
            cancellationToken);

        if (!registerResult.Succeeded)
        {
            return Result<RegisterResponse>.Failure(string.Join(" ", registerResult.Errors));
        }

        var userDto = new ApplicationUserDto
        {
            Id = registerResult.UserId,
            Email = request.Email,
            Username = request.Username,
            FullName = request.FullName,
            PreferredLanguage = request.PreferredLanguage,
            IsActive = true
        };

        var subdomain = TenantProvisioningService.GenerateSubdomain(request.Email);
        await tenantProvisioningService.ProvisionAsync(
            userDto.Id, request.FullName, subdomain, TenantType.Business, cancellationToken, createDefaultBrandProfile: true);

        var accessToken = jwtTokenGenerator.GenerateToken(userDto);
        var refreshToken = RefreshTokenPolicy.Issue(dbContext, userDto.Id, jwtTokenGenerator.RefreshTokenExpiryDays);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<RegisterResponse>.Success(
            new RegisterResponse(userDto.Id, userDto.Email, userDto.FullName, accessToken, refreshToken));
    }
}
