using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.DeclineInvite;

public class DeclineInviteCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<DeclineInviteCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeclineInviteCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new InvalidOperationException("An authenticated user is required.");

        var tenantMember = await dbContext.TenantMembers
            .FirstOrDefaultAsync(m => m.Id == request.TenantMemberId && m.UserId == userId, cancellationToken);

        if (tenantMember is null)
        {
            return Result<bool>.Failure("Invitation not found.");
        }

        if (tenantMember.InvitationStatus != InvitationStatus.Pending)
        {
            return Result<bool>.Failure("This invitation is no longer pending.");
        }

        tenantMember.InvitationStatus = InvitationStatus.Declined;

        // Return the escrowed coins to the tenant pool — nobody will ever spend them now.
        if (tenantMember.AllocatedCoins > 0)
        {
            var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantMember.TenantId, cancellationToken);
            tenant.CoinBalance += tenantMember.AllocatedCoins;
            tenantMember.AllocatedCoins = 0;
        }

        AuditLogger.Log(
            dbContext, tenantMember.TenantId, userId, "team.invite_declined",
            entityType: "tenant_member", entityId: tenantMember.Id);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
