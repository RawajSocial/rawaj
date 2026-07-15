using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
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

        var now = DateTime.UtcNow;

        var tenantMember = new TenantMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = targetUser.Id,
            Role = request.Role,
            InvitedBy = currentUserService.UserId,
            InvitationStatus = InvitationStatus.Accepted,
            JoinedAt = now,
            CreatedAt = now
        };

        dbContext.TenantMembers.Add(tenantMember);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AddTeamMemberResponse>.Success(
            new AddTeamMemberResponse(tenantMember.Id, targetUser.Id, targetUser.Email, tenantMember.Role));
    }
}
