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
                .Select(m => new { m.Id, m.UserId, m.Role, m.TenantId, TenantName = m.Tenant.Name, m.InvitedBy })
                .FirstOrDefaultAsync(cancellationToken);

            if (member is not null)
            {
                var invitedUser = await identityService.FindByIdAsync(member.UserId, cancellationToken);
                var inviter = member.InvitedBy is not null
                    ? await identityService.FindByIdAsync(member.InvitedBy.Value, cancellationToken)
                    : null;

                return Result<InvitationDetailsResponse>.Success(new InvitationDetailsResponse(
                    member.TenantName,
                    inviter?.FullName ?? "فريق العمل",
                    invitedUser?.Email ?? string.Empty,
                    member.Role,
                    RequiresRegistration: false,
                    TenantMemberId: member.Id,
                    InvitationId: null));
            }
        }

        var tokenHash = InvitationTokenPolicy.Hash(request.Token);
        var now = DateTime.UtcNow;

        var invitation = await dbContext.TenantInvitations
            .Where(i => i.TokenHash == tokenHash && i.Status == InvitationStatus.Pending && i.ExpiresAt > now)
            .Select(i => new { i.Id, i.Email, i.Role, i.TenantId, TenantName = i.Tenant.Name, i.InvitedByUserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (invitation is null)
        {
            return Result<InvitationDetailsResponse>.Failure("This invitation was not found or has expired.");
        }

        var invitingUser = await identityService.FindByIdAsync(invitation.InvitedByUserId, cancellationToken);

        return Result<InvitationDetailsResponse>.Success(new InvitationDetailsResponse(
            invitation.TenantName,
            invitingUser?.FullName ?? "فريق العمل",
            invitation.Email,
            invitation.Role,
            RequiresRegistration: true,
            TenantMemberId: null,
            InvitationId: invitation.Id));
    }
}
