using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.SocialAccounts.GetSocialAccounts;

public class GetSocialAccountsQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetSocialAccountsQuery, Result<List<SocialAccountSummary>>>
{
    public async Task<Result<List<SocialAccountSummary>>> Handle(GetSocialAccountsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var brandProfileBelongsToTenant = await dbContext.TenantBrandProfiles
            .AnyAsync(b => b.Id == request.BrandProfileId && b.TenantId == tenantId, cancellationToken);
        if (!brandProfileBelongsToTenant)
        {
            return Result<List<SocialAccountSummary>>.Failure("Brand profile not found.");
        }

        var accounts = await dbContext.SocialAccounts
            .Where(s => s.BrandProfileId == request.BrandProfileId)
            .OrderBy(s => s.Platform)
            .Select(s => new SocialAccountSummary(s.Id, s.Platform, s.AccountName, s.IsActive, s.TokenExpiresAt, s.LastVerifiedAt))
            .ToListAsync(cancellationToken);

        return Result<List<SocialAccountSummary>>.Success(accounts);
    }
}
