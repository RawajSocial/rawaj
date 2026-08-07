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
    string? BriefJson = null,
    /// <summary>Stamps <see cref="Rawaj.Domain.Entities.Campaigns.MarketingCampaign.OnboardingCompletedAt"/>
    /// the first time it's sent — the onboarding wizard sends this exactly once, in the same update
    /// call it already makes before advancing from step 7 into strategy review. Idempotent in the
    /// handler, so a retried autosave can't stamp it twice or move it later.</summary>
    bool MarkOnboardingCompleted = false)
    : IRequest<Result<GetCampaignResponse>>, IRequireTenantRole, IRequireResolvedBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;

    public Task<Guid?> ResolveBrandProfileIdAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.MarketingCampaigns.Where(c => c.Id == CampaignId).Select(c => (Guid?)c.BrandProfileId).FirstOrDefaultAsync(cancellationToken);
}
