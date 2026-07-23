using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Tenants.GetMyMemberships;

public class GetMyMembershipsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<GetMyMembershipsQuery, Result<List<MembershipSummary>>>
{
    public async Task<Result<List<MembershipSummary>>> Handle(GetMyMembershipsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Result<List<MembershipSummary>>.Failure("Unauthorized.");
        }

        var rows = await (
            from member in dbContext.TenantMembers
            join tenant in dbContext.Tenants on member.TenantId equals tenant.Id
            where member.UserId == userId && member.InvitationStatus == InvitationStatus.Accepted
            select new
            {
                tenant.Id,
                tenant.Name,
                tenant.TenantType,
                tenant.OwnerUserId,
                tenant.IsActivated,
                tenant.CoinBalance,
                member.Role,
                member.AllocatedCoins,
                member.SpentCoins,
            }
        ).ToListAsync(cancellationToken);

        var summaries = rows
            .Select(r => new MembershipSummary(
                r.Id,
                r.Name,
                r.TenantType,
                r.Role,
                r.OwnerUserId == userId,
                r.IsActivated,
                r.Role.HasAtLeast(TenantMemberRole.Admin) ? r.CoinBalance : r.AllocatedCoins - r.SpentCoins))
            .OrderByDescending(s => s.IsOwner)
            .ThenBy(s => s.Name)
            .ToList();

        return Result<List<MembershipSummary>>.Success(summaries);
    }
}
