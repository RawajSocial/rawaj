using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.GetInvitationDetails;

public class GetInvitationDetailsQueryHandler(IApplicationDbContext dbContext, IIdentityService identityService)
    : IRequestHandler<GetInvitationDetailsQuery, Result<InvitationDetailsResponse>>
{
    public async Task<Result<InvitationDetailsResponse>> Handle(GetInvitationDetailsQuery request, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(request.Token, out var tenantMemberId))
        {
            var member = await dbContext.TenantMembers
                .Where(m => m.Id == tenantMemberId && m.InvitationStatus == InvitationStatus.Pending)
                .Select(m => new
                {
                    m.Id,
                    m.UserId,
                    m.Role,
                    m.TenantId,
                    TenantName = m.Tenant.Name,
                    m.InvitedBy,
                    BrandAccesses = m.BrandAccesses.Select(a => new InvitationBrandSummary(
                        a.BrandProfile.Id, a.BrandProfile.Name, a.BrandProfile.BrandInfo!.LogoUrl, a.BrandProfile.BrandInfo!.Industry)).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (member is not null)
            {
                var invitedUser = await identityService.FindByIdAsync(member.UserId, cancellationToken);
                var inviter = member.InvitedBy is not null
                    ? await identityService.FindByIdAsync(member.InvitedBy.Value, cancellationToken)
                    : null;

                var fullTenantAccess = member.Role is TenantMemberRole.Owner or TenantMemberRole.Admin;
                var brandProfiles = fullTenantAccess
                    ? await GetAllTenantBrandsAsync(dbContext, member.TenantId, cancellationToken)
                    : member.BrandAccesses;

                return Result<InvitationDetailsResponse>.Success(new InvitationDetailsResponse(
                    member.TenantName,
                    inviter?.FullName ?? "فريق العمل",
                    invitedUser?.Email ?? string.Empty,
                    member.Role,
                    RequiresRegistration: false,
                    TenantMemberId: member.Id,
                    InvitationId: null,
                    FullTenantAccess: fullTenantAccess,
                    BrandProfiles: brandProfiles));
            }
        }

        var tokenHash = InvitationTokenPolicy.Hash(request.Token);
        var now = DateTime.UtcNow;

        var invitation = await dbContext.TenantInvitations
            .Where(i => i.TokenHash == tokenHash && i.Status == InvitationStatus.Pending && i.ExpiresAt > now)
            .Select(i => new { i.Id, i.Email, i.Role, i.TenantId, TenantName = i.Tenant.Name, i.InvitedByUserId, i.BrandProfileIds })
            .FirstOrDefaultAsync(cancellationToken);

        if (invitation is null)
        {
            return Result<InvitationDetailsResponse>.Failure("This invitation was not found or has expired.");
        }

        var invitingUser = await identityService.FindByIdAsync(invitation.InvitedByUserId, cancellationToken);

        var invitationFullTenantAccess = invitation.Role is TenantMemberRole.Owner or TenantMemberRole.Admin;
        var invitationBrandProfiles = invitationFullTenantAccess
            ? await GetAllTenantBrandsAsync(dbContext, invitation.TenantId, cancellationToken)
            : await dbContext.TenantBrandProfiles
                .Where(b => invitation.BrandProfileIds.Contains(b.Id))
                .Select(b => new InvitationBrandSummary(b.Id, b.Name, b.BrandInfo!.LogoUrl, b.BrandInfo!.Industry))
                .ToListAsync(cancellationToken);

        return Result<InvitationDetailsResponse>.Success(new InvitationDetailsResponse(
            invitation.TenantName,
            invitingUser?.FullName ?? "فريق العمل",
            invitation.Email,
            invitation.Role,
            RequiresRegistration: true,
            TenantMemberId: null,
            InvitationId: invitation.Id,
            FullTenantAccess: invitationFullTenantAccess,
            BrandProfiles: invitationBrandProfiles));
    }

    private static async Task<List<InvitationBrandSummary>> GetAllTenantBrandsAsync(
        IApplicationDbContext dbContext, Guid tenantId, CancellationToken cancellationToken)
        => await dbContext.TenantBrandProfiles
            .Where(b => b.TenantId == tenantId)
            .Select(b => new InvitationBrandSummary(b.Id, b.Name, b.BrandInfo!.LogoUrl, b.BrandInfo!.Industry))
            .ToListAsync(cancellationToken);
}
