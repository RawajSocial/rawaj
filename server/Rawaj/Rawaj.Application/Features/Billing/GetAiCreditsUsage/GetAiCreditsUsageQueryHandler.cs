using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Common.Policies;

namespace Rawaj.Application.Features.Billing.GetAiCreditsUsage;

public class GetAiCreditsUsageQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetAiCreditsUsageQuery, Result<AiCreditsUsage>>
{
    public async Task<Result<AiCreditsUsage>> Handle(GetAiCreditsUsageQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var usage = await AiCreditsPolicy.GetUsageAsync(dbContext, tenantId, cancellationToken);

        return Result<AiCreditsUsage>.Success(usage);
    }
}
