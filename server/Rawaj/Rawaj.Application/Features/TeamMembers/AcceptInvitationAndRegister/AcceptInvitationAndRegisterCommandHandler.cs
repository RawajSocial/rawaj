using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Email;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Application.Common.Services;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.AcceptInvitationAndRegister;

public class AcceptInvitationAndRegisterCommandHandler(
    IApplicationDbContext dbContext,
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator,
    TenantProvisioningService tenantProvisioningService,
    IEmailService emailService)
    : IRequestHandler<AcceptInvitationAndRegisterCommand, Result<AcceptInvitationAndRegisterResponse>>
{
    public async Task<Result<AcceptInvitationAndRegisterResponse>> Handle(
        AcceptInvitationAndRegisterCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = InvitationTokenPolicy.Hash(request.Token);
        var now = DateTime.UtcNow;

        var invitation = await dbContext.TenantInvitations
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash && i.Status == InvitationStatus.Pending && i.ExpiresAt > now, cancellationToken);
        if (invitation is null)
        {
            return Result<AcceptInvitationAndRegisterResponse>.Failure("This invitation was not found or has expired.");
        }

        var existingUser = await identityService.FindByEmailAsync(invitation.Email, cancellationToken);
        if (existingUser is not null)
        {
            return Result<AcceptInvitationAndRegisterResponse>.Failure(
                "An account with this email already exists — sign in and accept the invitation from your notifications instead.");
        }

        var existingUsername = await identityService.FindByUsernameAsync(request.Username, cancellationToken);
        if (existingUsername is not null)
        {
            return Result<AcceptInvitationAndRegisterResponse>.Failure("A user with this username already exists.");
        }

        var registerResult = await identityService.CreateUserAsync(
            invitation.Email, request.Username, request.Password, request.FullName, request.PreferredLanguage, cancellationToken);
        if (!registerResult.Succeeded)
        {
            return Result<AcceptInvitationAndRegisterResponse>.Failure(string.Join(" ", registerResult.Errors));
        }

        var userDto = new ApplicationUserDto
        {
            Id = registerResult.UserId,
            Email = invitation.Email,
            Username = request.Username,
            FullName = request.FullName,
            PreferredLanguage = request.PreferredLanguage,
            IsActive = true
        };

        // Every user gets their own tenant regardless of how they signed up, matching
        // RegisterCommandHandler — this one starts unactivated until they fill in their own
        // business info, same as any other brand owner.
        var subdomain = TenantProvisioningService.GenerateSubdomain(invitation.Email);
        await tenantProvisioningService.ProvisionAsync(
            userDto.Id, request.FullName, subdomain, TenantType.Business, cancellationToken, createDefaultBrandProfile: false);

        var tenantMember = new TenantMember
        {
            Id = Guid.NewGuid(),
            TenantId = invitation.TenantId,
            UserId = userDto.Id,
            Role = invitation.Role,
            InvitedBy = invitation.InvitedByUserId,
            InvitationStatus = InvitationStatus.Accepted,
            JoinedAt = now,
            // The coins were already escrowed out of the tenant pool when the invitation was
            // created — accepting just activates the wallet, it never touches the pool again.
            AllocatedCoins = invitation.AllocatedCoins,
            SpentCoins = 0,
            CreatedAt = now
        };
        dbContext.TenantMembers.Add(tenantMember);

        foreach (var brandProfileId in invitation.BrandProfileIds)
        {
            dbContext.TenantMemberBrandAccesses.Add(new TenantMemberBrandAccess
            {
                Id = Guid.NewGuid(),
                TenantMemberId = tenantMember.Id,
                BrandProfileId = brandProfileId,
            });
        }

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = now;

        AuditLogger.Log(
            dbContext, invitation.TenantId, userDto.Id, "team.invite_accepted",
            message: $"{invitation.Email} registered and joined as {invitation.Role}.",
            entityType: "tenant_member", entityId: tenantMember.Id);

        var accessToken = jwtTokenGenerator.GenerateToken(userDto);
        var refreshToken = RefreshTokenPolicy.Issue(dbContext, userDto.Id, jwtTokenGenerator.RefreshTokenExpiryDays);

        await dbContext.SaveChangesAsync(cancellationToken);

        var welcomeEmail = EmailTemplates.Welcome(userDto.FullName);
        await emailService.SendEmailAsync(
            userDto.Email, welcomeEmail.Subject, welcomeEmail.Html, welcomeEmail.PlainText, cancellationToken);

        return Result<AcceptInvitationAndRegisterResponse>.Success(
            new AcceptInvitationAndRegisterResponse(userDto.Id, userDto.Email, userDto.FullName, accessToken, refreshToken));
    }
}
