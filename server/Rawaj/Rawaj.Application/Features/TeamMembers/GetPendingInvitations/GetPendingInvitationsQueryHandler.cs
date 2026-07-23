using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.GetPendingInvitations;

public class GetPendingInvitationsQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetPendingInvitationsQuery, Result<List<PendingInvitationSummary>>>
{
    public async Task<Result<List<PendingInvitationSummary>>> Handle(GetPendingInvitationsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var invitations = await dbContext.TenantInvitations
            .Where(i => i.TenantId == tenantId && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new PendingInvitationSummary(i.Id, i.Email, i.Role, i.AllocatedCoins, i.CreatedAt, i.ExpiresAt))
            .ToListAsync(cancellationToken);

        return Result<List<PendingInvitationSummary>>.Success(invitations);
    }
}
