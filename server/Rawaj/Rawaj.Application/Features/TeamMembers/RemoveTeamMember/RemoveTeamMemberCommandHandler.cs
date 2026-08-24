using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.RemoveTeamMember;

public class RemoveTeamMemberCommandHandler(
    IApplicationDbContext dbContext, ICurrentUserService currentUserService, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<RemoveTeamMemberCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var tenantMember = await dbContext.TenantMembers
            .FirstOrDefaultAsync(m => m.Id == request.TenantMemberId && m.TenantId == tenantId, cancellationToken);
        if (tenantMember is null)
        {
            return Result<bool>.Failure("Team member not found.");
        }

        if (tenantMember.Role == TenantMemberRole.Owner)
        {
            return Result<bool>.Failure("The tenant owner cannot be removed.");
        }

        if (tenantMember.UserId == currentUserService.UserId)
        {
            return Result<bool>.Failure("You cannot remove yourself from the team.");
        }

        // Return whatever coins were escrowed to this member but never spent back to the tenant
        // pool — otherwise every removal would leak coins out of the tenant's balance for good.
        var unspentAllocation = tenantMember.AllocatedCoins - tenantMember.SpentCoins;
        if (unspentAllocation > 0)
        {
            var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
            tenant.CoinBalance += unspentAllocation;
        }

        var accesses = await dbContext.TenantMemberBrandAccesses
            .Where(a => a.TenantMemberId == tenantMember.Id)
            .ToListAsync(cancellationToken);
        dbContext.TenantMemberBrandAccesses.RemoveRange(accesses);

        dbContext.TenantMembers.Remove(tenantMember);

        AuditLogger.Log(
            dbContext, tenantId, currentUserService.UserId, "team.member_removed",
            message: unspentAllocation > 0 ? $"Returned {unspentAllocation} unspent coins to the organization." : null,
            entityType: "tenant_member", entityId: tenantMember.Id);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
