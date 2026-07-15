using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GenerateMarketingPlan;

public record GenerateMarketingPlanCommand(Guid CampaignId)
    : IRequest<Result<GenerateMarketingPlanResponse>>, IRequireTenantRole
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
