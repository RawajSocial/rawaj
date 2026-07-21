using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.TeamMembers.GetMyPendingInvites;

public class GetMyPendingInvitesQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<GetMyPendingInvitesQuery, Result<List<PendingInviteSummary>>>
{
    public async Task<Result<List<PendingInviteSummary>>> Handle(GetMyPendingInvitesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new InvalidOperationException("An authenticated user is required.");

        var invites = await dbContext.TenantMembers
            .Where(m => m.UserId == userId && m.InvitationStatus == InvitationStatus.Pending)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new PendingInviteSummary(
                m.Id,
                m.TenantId,
                m.Tenant.Name,
                m.Role,
                m.BrandAccesses.Select(a => a.BrandProfile.Name).ToList(),
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<PendingInviteSummary>>.Success(invites);
    }
}
