using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.GetCampaign;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaign;

public class UpdateCampaignCommandHandler(IApplicationDbContext dbContext, ICurrentTenantContext currentTenantContext)
    : IRequestHandler<UpdateCampaignCommand, Result<GetCampaignResponse>>
{
    public async Task<Result<GetCampaignResponse>> Handle(UpdateCampaignCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantContext.TenantId!.Value;

        var campaign = await dbContext.MarketingCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId && c.BrandProfile.TenantId == tenantId, cancellationToken);
        if (campaign is null)
        {
            return Result<GetCampaignResponse>.Failure("Campaign not found.");
        }

        if (request.Name is not null) campaign.Name = request.Name;
        if (request.Status is not null) campaign.Status = request.Status.Value;
        if (request.StartDate is not null) campaign.StartDate = request.StartDate;
        if (request.EndDate is not null) campaign.EndDate = request.EndDate;
        if (request.BudgetAmount is not null) campaign.BudgetAmount = request.BudgetAmount;
        campaign.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new GetCampaignResponse(
            campaign.Id,
            campaign.BrandProfileId,
            campaign.Name,
            campaign.Objective,
            campaign.TargetPlatforms,
            campaign.StartDate,
            campaign.EndDate,
            campaign.BudgetAmount,
            campaign.BudgetCurrency,
            campaign.Status,
            campaign.AiPlanJson,
            campaign.AiGeneratedAt,
            campaign.CreatedAt,
            campaign.UpdatedAt);

        return Result<GetCampaignResponse>.Success(response);
    }
}
