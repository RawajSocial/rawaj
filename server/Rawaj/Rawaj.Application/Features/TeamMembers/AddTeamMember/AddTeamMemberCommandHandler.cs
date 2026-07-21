using MediatR;
using Microsoft.EntityFrameworkCore;
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
    ICurrentTenantContext currentTenantContext)
    : IRequestHandler<AddTeamMemberCommand, Result<AddTeamMemberResponse>>
{
    public async Task<Result<AddTeamMemberResponse>> Handle(AddTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var targetUser = await identityService.FindByEmailAsync(request.Email, cancellationToken);
        if (targetUser is null)
        {
            return Result<AddTeamMemberResponse>.Failure("No user found with this email. They must register first.");
        }

        var alreadyMember = await dbContext.TenantMembers
            .AnyAsync(m => m.TenantId == tenantId && m.UserId == targetUser.Id, cancellationToken);
        if (alreadyMember)
        {
            return Result<AddTeamMemberResponse>.Failure("This user is already a member of your organization.");
        }

        var maxUsers = await (
            from tenant in dbContext.Tenants
            join subscription in dbContext.Subscriptions on tenant.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans on subscription.SubscriptionPlanId equals plan.Id
            where tenant.Id == tenantId
            select plan.MaxUsers
        ).FirstAsync(cancellationToken);

        var currentMemberCount = await dbContext.TenantMembers
            .CountAsync(m => m.TenantId == tenantId, cancellationToken);

        if (currentMemberCount >= maxUsers)
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

        var tenantName = await dbContext.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.Name)
            .FirstAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var tenantMember = new TenantMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = targetUser.Id,
            Role = request.Role,
            InvitedBy = currentUserService.UserId,
            InvitationStatus = InvitationStatus.Pending,
            JoinedAt = null,
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

        NotificationPublisher.Notify(
            dbContext,
            targetUser.Id,
            null,
            NotificationType.Info,
            NotificationCategory.TeamInvite,
            "You've been invited to join a team",
            $"{tenantName} invited you to join as {request.Role}. Accept to get access.",
            tenantMember.Id,
            "tenant_member");

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AddTeamMemberResponse>.Success(
            new AddTeamMemberResponse(tenantMember.Id, targetUser.Id, targetUser.Email, tenantMember.Role, tenantMember.InvitationStatus));
    }
}
