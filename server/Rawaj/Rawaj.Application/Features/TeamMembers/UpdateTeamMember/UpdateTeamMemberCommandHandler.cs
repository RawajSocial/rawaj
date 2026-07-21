using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Entities.Tenants;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.UpdateTeamMember;

public class UpdateTeamMemberCommandHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
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

        var existingAccesses = await dbContext.TenantMemberBrandAccesses
            .Where(a => a.TenantMemberId == tenantMember.Id)
            .ToListAsync(cancellationToken);
        foreach (var access in existingAccesses)
        {
            dbContext.TenantMemberBrandAccesses.Remove(access);
        }

        tenantMember.Role = request.Role;

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

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateTeamMemberResponse>.Success(
            new UpdateTeamMemberResponse(tenantMember.Id, tenantMember.Role, brandProfileIds));
    }
}
