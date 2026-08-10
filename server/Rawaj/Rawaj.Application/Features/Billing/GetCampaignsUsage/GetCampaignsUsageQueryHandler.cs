using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;

namespace Rawaj.Application.Features.Billing.GetCampaignsUsage;

public class GetCampaignsUsageQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetCampaignsUsageQuery, Result<CampaignsUsage>>
{
    public async Task<Result<CampaignsUsage>> Handle(GetCampaignsUsageQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var usage = await CampaignsUsagePolicy.GetUsageAsync(dbContext, tenantId, cancellationToken);

        return Result<CampaignsUsage>.Success(usage);
    }
}
