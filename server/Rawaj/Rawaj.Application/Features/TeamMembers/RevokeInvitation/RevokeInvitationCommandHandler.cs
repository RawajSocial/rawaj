using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.RevokeInvitation;

public class RevokeInvitationCommandHandler(
    IApplicationDbContext dbContext, ICurrentUserService currentUserService, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<RevokeInvitationCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RevokeInvitationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var invitation = await dbContext.TenantInvitations
            .FirstOrDefaultAsync(i => i.Id == request.InvitationId && i.TenantId == tenantId, cancellationToken);
        if (invitation is null)
        {
            return Result<bool>.Failure("Invitation not found.");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            return Result<bool>.Failure("This invitation is no longer pending.");
        }

        invitation.Status = InvitationStatus.Declined;

        if (invitation.AllocatedCoins > 0)
        {
            var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
            tenant.CoinBalance += invitation.AllocatedCoins;
        }

        AuditLogger.Log(
            dbContext, tenantId, currentUserService.UserId, "team.invite_revoked",
            message: $"Revoked invitation to {invitation.Email}.",
            entityType: "tenant_invitation", entityId: invitation.Id);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
