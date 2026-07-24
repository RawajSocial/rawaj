using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Email;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.AddTeamMember;

public class AddTeamMemberCommandHandler(
    IApplicationDbContext dbContext,
    IIdentityService identityService,
    ICurrentUserService currentUserService,
    ICurrentTenantContext currentTenantContext,
    IEmailService emailService,
    IFrontendUrlProvider frontendUrlProvider)
    : IRequestHandler<AddTeamMemberCommand, Result<AddTeamMemberResponse>>
{
    public async Task<Result<AddTeamMemberResponse>> Handle(AddTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;
        var inviterId = currentUserService.UserId!.Value;

        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant.CoinBalance < request.AllocatedCoins)
        {
            return Result<AddTeamMemberResponse>.Failure("Your organization doesn't have enough coins to allocate that amount.");
        }

        var now = DateTime.UtcNow;

        var maxUsers = await (
            from t in dbContext.Tenants
            join subscription in dbContext.Subscriptions on t.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where t.Id == tenantId
            select plan.MaxUsers
        ).FirstAsync(cancellationToken) + tenant.ExtraMarketeersPurchased;

        // Declined members/invitations don't hold a seat — otherwise a single decline would burn
        // that seat forever. Expired invitations don't hold one either, since they can no longer be
        // accepted and would otherwise sit blocking a re-invite indefinitely.
        var currentMemberCount = await dbContext.TenantMembers
            .CountAsync(m => m.TenantId == tenantId && m.InvitationStatus != InvitationStatus.Declined, cancellationToken);
        var pendingInvitationCount = await dbContext.TenantInvitations
            .CountAsync(i => i.TenantId == tenantId && i.Status == InvitationStatus.Pending && i.ExpiresAt > now, cancellationToken);

        if (currentMemberCount + pendingInvitationCount >= maxUsers)
        {
            return Result<AddTeamMemberResponse>.Failure(
                $"Your subscription plan allows a maximum of {maxUsers} team member(s). Upgrade to add more.");
        }

        var isBrandScopedRole = request.Role is TenantMemberRole.Editor or TenantMemberRole.Viewer;
        if (isBrandScopedRole)
        {
            var validBrandCount = await dbContext.TenantBrandProfiles
                .CountAsync(b => b.TenantId == tenantId && request.BrandProfileIds.Contains(b.Id), cancellationToken);

            if (validBrandCount != request.BrandProfileIds.Distinct().Count())
            {
                return Result<AddTeamMemberResponse>.Failure("One or more selected brand profiles are invalid.");
            }
        }

        var inviter = await identityService.FindByIdAsync(inviterId, cancellationToken);
        var inviterName = inviter?.FullName ?? "A team admin";
        var roleLabel = RoleLabel(request.Role);

        var targetUser = await identityService.FindByEmailAsync(request.Email, cancellationToken);

        if (targetUser is not null)
        {
            var alreadyMember = await dbContext.TenantMembers.AnyAsync(
                m => m.TenantId == tenantId && m.UserId == targetUser.Id && m.InvitationStatus != InvitationStatus.Declined,
                cancellationToken);
            if (alreadyMember)
            {
                return Result<AddTeamMemberResponse>.Failure("This user is already a member of your organization.");
            }

            // Cross-check the other invite path: this email may have been invited before they had
            // an account (a TenantInvitation row) and has since registered separately. Without this
            // check, inviting them again here would create a second TenantMember and escrow coins a
            // second time, while the original TenantInvitation rots forever (it can never be
            // consumed once an account with this email exists).
            var stalePendingInvitation = await dbContext.TenantInvitations.AnyAsync(
                i => i.TenantId == tenantId && i.Email == request.Email && i.Status == InvitationStatus.Pending && i.ExpiresAt > now,
                cancellationToken);
            if (stalePendingInvitation)
            {
                return Result<AddTeamMemberResponse>.Failure(
                    "An invitation is already pending for this email. Revoke it first, then invite again.");
            }

            var tenantMember = new TenantMember
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = targetUser.Id,
                Role = request.Role,
                InvitedBy = inviterId,
                InvitationStatus = InvitationStatus.Pending,
                JoinedAt = null,
                AllocatedCoins = request.AllocatedCoins,
                SpentCoins = 0,
                CreatedAt = now
            };
            dbContext.TenantMembers.Add(tenantMember);

            if (isBrandScopedRole)
            {
                foreach (var brandProfileId in request.BrandProfileIds.Distinct())
                {
                    dbContext.TenantMemberBrandAccesses.Add(new TenantMemberBrandAccess
                    {
                        Id = Guid.NewGuid(),
                        TenantMemberId = tenantMember.Id,
                        BrandProfileId = brandProfileId,
                    });
                }
            }

            tenant.CoinBalance -= request.AllocatedCoins;

            NotificationPublisher.Notify(
                dbContext, targetUser.Id, null, NotificationType.Info, NotificationCategory.TeamInvite,
                "You've been invited to join a team",
                $"{tenant.Name} invited you to join as {request.Role}. Accept to get access.",
                tenantMember.Id, "tenant_member");

            AuditLogger.Log(
                dbContext, tenantId, inviterId, "team.invite_sent",
                message: $"Invited {request.Email} as {request.Role} with {request.AllocatedCoins} coins.",
                entityType: "tenant_member", entityId: tenantMember.Id);

            await dbContext.SaveChangesAsync(cancellationToken);

            var inviteUrl = $"{frontendUrlProvider.BaseUrl}/invite?token={tenantMember.Id}";
            var email = EmailTemplates.TeamInvite(inviterName, tenant.Name, roleLabel, inviteUrl);
            await emailService.SendEmailAsync(request.Email, email.Subject, email.Html, email.PlainText, cancellationToken);

            return Result<AddTeamMemberResponse>.Success(
                new AddTeamMemberResponse(
                    tenantMember.Id, null, targetUser.Email, tenantMember.Role,
                    RequiresRegistration: false, EmailConfigured: emailService.IsConfigured));
        }

        var alreadyInvited = await dbContext.TenantInvitations.AnyAsync(
            i => i.TenantId == tenantId && i.Email == request.Email && i.Status == InvitationStatus.Pending && i.ExpiresAt > now,
            cancellationToken);
        if (alreadyInvited)
        {
            return Result<AddTeamMemberResponse>.Failure("An invitation is already pending for this email.");
        }

        var rawToken = InvitationTokenPolicy.GenerateRawToken();

        var invitation = new TenantInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = request.Email,
            Role = request.Role,
            BrandProfileIds = isBrandScopedRole ? request.BrandProfileIds.Distinct().ToList() : [],
            AllocatedCoins = request.AllocatedCoins,
            InvitedByUserId = inviterId,
            TokenHash = InvitationTokenPolicy.Hash(rawToken),
            ExpiresAt = now.AddDays(InvitationTokenPolicy.ExpiryDays),
            Status = InvitationStatus.Pending,
            CreatedAt = now
        };
        dbContext.TenantInvitations.Add(invitation);

        tenant.CoinBalance -= request.AllocatedCoins;

        AuditLogger.Log(
            dbContext, tenantId, inviterId, "team.invite_sent",
            message: $"Invited {request.Email} (no account yet) as {request.Role} with {request.AllocatedCoins} coins.",
            entityType: "tenant_invitation", entityId: invitation.Id);

        await dbContext.SaveChangesAsync(cancellationToken);

        var newUserInviteUrl = $"{frontendUrlProvider.BaseUrl}/invite?token={rawToken}";
        var newUserEmail = EmailTemplates.TeamInvite(inviterName, tenant.Name, roleLabel, newUserInviteUrl);
        await emailService.SendEmailAsync(request.Email, newUserEmail.Subject, newUserEmail.Html, newUserEmail.PlainText, cancellationToken);

        return Result<AddTeamMemberResponse>.Success(
            new AddTeamMemberResponse(
                null, invitation.Id, request.Email, request.Role,
                RequiresRegistration: true, EmailConfigured: emailService.IsConfigured));
    }

    private static string RoleLabel(TenantMemberRole role) => role switch
    {
        TenantMemberRole.Admin => "مدير",
        TenantMemberRole.Editor => "محرر",
        TenantMemberRole.Viewer => "مشاهد",
        _ => role.ToString()
    };
}
