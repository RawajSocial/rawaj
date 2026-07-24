using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Application.Features.Campaigns.GetCampaign;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.UpdateCampaign;

public record UpdateCampaignCommand(
    Guid CampaignId,
    string? Name,
    CampaignStatus? Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal? BudgetAmount,
    string? Objective = null,
    List<string>? TargetPlatforms = null,
    string? BudgetCurrency = null,
    string? BriefJson = null)
    : IRequest<Result<GetCampaignResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.MarketingCampaigns.Where(c => c.Id == CampaignId).Select(c => (Guid?)c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
