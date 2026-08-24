using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.AcceptInvite;

public class AcceptInviteCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<AcceptInviteCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
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

        // Mirrors the 7-day expiry TenantInvitation enforces on the no-account invite path — this
        // path (an existing user invited by TenantMember.Id) had no expiry at all before, so an
        // invite link from a year ago was still redeemable.
        if (tenantMember.CreatedAt.AddDays(InvitationTokenPolicy.ExpiryDays) < DateTime.UtcNow)
        {
            return Result<bool>.Failure("This invitation has expired. Ask for a new one.");
        }

        tenantMember.InvitationStatus = InvitationStatus.Accepted;
        tenantMember.JoinedAt = DateTime.UtcNow;

        AuditLogger.Log(
            dbContext, tenantMember.TenantId, userId, "team.invite_accepted",
            entityType: "tenant_member", entityId: tenantMember.Id);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
