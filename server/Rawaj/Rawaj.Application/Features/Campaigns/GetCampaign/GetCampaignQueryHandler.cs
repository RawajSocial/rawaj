using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Features.Campaigns.GetCampaign;

public class GetCampaignQueryHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<GetCampaignQuery, Result<GetCampaignResponse>>
{
    public async Task<Result<GetCampaignResponse>> Handle(GetCampaignQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var response = await dbContext.MarketingCampaigns
            .Where(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId)
            .Select(c => new GetCampaignResponse(
                c.Id,
                c.BrandProfileId,
                c.Name,
                c.Objective,
                c.TargetPlatforms,
                c.StartDate,
                c.EndDate,
                c.BudgetAmount,
                c.BudgetCurrency,
                c.Status,
                c.AiPlanJson,
                c.AiGeneratedAt,
                c.BriefJson,
                c.CompetitorResearchJson,
                c.DiagnosisJson,
                c.PlanApprovedAt,
                c.CreatedAt,
                c.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return response is null
            ? Result<GetCampaignResponse>.Failure("Campaign not found.")
            : Result<GetCampaignResponse>.Success(response);
    }
}
