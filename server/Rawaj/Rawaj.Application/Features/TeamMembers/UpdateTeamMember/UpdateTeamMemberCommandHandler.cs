using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.UpdateTeamMember;

public class UpdateTeamMemberCommandHandler(
    IApplicationDbContext dbContext, ICurrentUserService currentUserService, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<UpdateTeamMemberCommand, Result<UpdateTeamMemberResponse>>
{
    public async Task<Result<UpdateTeamMemberResponse>> Handle(UpdateTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var tenantMember = await dbContext.TenantMembers
            .FirstOrDefaultAsync(m => m.Id == request.TenantMemberId && m.TenantId == tenantId, cancellationToken);
        if (tenantMember is null)
        {
            return Result<UpdateTeamMemberResponse>.Failure("Team member not found.");
        }

        if (tenantMember.Role == TenantMemberRole.Owner)
        {
            return Result<UpdateTeamMemberResponse>.Failure("The tenant owner's role cannot be changed.");
        }

        var isBrandScopedRole = request.Role is TenantMemberRole.Editor or TenantMemberRole.Viewer;

        if (isBrandScopedRole)
        {
            var validBrandCount = await dbContext.TenantBrandProfiles
                .CountAsync(b => b.TenantId == tenantId && request.BrandProfileIds.Contains(b.Id), cancellationToken);

            if (validBrandCount != request.BrandProfileIds.Distinct().Count())
            {
                return Result<UpdateTeamMemberResponse>.Failure("One or more selected brand profiles are invalid.");
            }
        }

        var previousRole = tenantMember.Role;
        var previousBrandProfileIds = await dbContext.TenantMemberBrandAccesses
            .Where(a => a.TenantMemberId == tenantMember.Id)
            .Select(a => a.BrandProfileId)
            .ToListAsync(cancellationToken);

        var existingAccesses = await dbContext.TenantMemberBrandAccesses
            .Where(a => a.TenantMemberId == tenantMember.Id)
            .ToListAsync(cancellationToken);
        foreach (var access in existingAccesses)
        {
            dbContext.TenantMemberBrandAccesses.Remove(access);
        }

        tenantMember.Role = request.Role;

        // Promoting an Editor/Viewer to Admin moves their spending onto the tenant pool (see
        // CoinPolicy) — without this, whatever was left in their escrowed wallet becomes
        // permanently stranded, since Admins have no wallet and AllocateCoins refuses to touch them.
        if (request.Role == TenantMemberRole.Admin && previousRole is TenantMemberRole.Editor or TenantMemberRole.Viewer)
        {
            var unspentAllocation = tenantMember.AllocatedCoins - tenantMember.SpentCoins;
            if (unspentAllocation > 0)
            {
                var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
                tenant.CoinBalance += unspentAllocation;
            }
            tenantMember.AllocatedCoins = 0;
            tenantMember.SpentCoins = 0;
        }

        var brandProfileIds = isBrandScopedRole ? request.BrandProfileIds.Distinct().ToList() : [];
        foreach (var brandProfileId in brandProfileIds)
        {
            dbContext.TenantMemberBrandAccesses.Add(new TenantMemberBrandAccess
            {
                Id = Guid.NewGuid(),
                TenantMemberId = tenantMember.Id,
                BrandProfileId = brandProfileId,
            });
        }

        var userId = currentUserService.UserId;
        if (previousRole != request.Role)
        {
            AuditLogger.Log(
                dbContext, tenantId, userId, "team.role_changed",
                message: $"Changed role from {previousRole} to {request.Role}.",
                entityType: "tenant_member", entityId: tenantMember.Id,
                oldValue: previousRole.ToString(), newValue: request.Role.ToString());
        }
        if (!previousBrandProfileIds.OrderBy(x => x).SequenceEqual(brandProfileIds.OrderBy(x => x)))
        {
            AuditLogger.Log(
                dbContext, tenantId, userId, "team.brand_access_changed",
                entityType: "tenant_member", entityId: tenantMember.Id,
                oldValue: string.Join(",", previousBrandProfileIds), newValue: string.Join(",", brandProfileIds));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateTeamMemberResponse>.Success(
            new UpdateTeamMemberResponse(tenantMember.Id, tenantMember.Role, brandProfileIds));
    }
}
