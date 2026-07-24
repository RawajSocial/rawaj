using MediatR;
using Rawaj.Application.Common.Interfaces;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.CreateCampaign;

public record CreateCampaignCommand(
    Guid BrandProfileId,
    string Name,
    string? Objective,
    List<string> TargetPlatforms,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal? BudgetAmount,
    string? BudgetCurrency,
    /// <summary>The onboarding wizard's collected brief/answers, as opaque JSON — see
    /// <see cref="Rawaj.Domain.Entities.Campaigns.MarketingCampaign.BriefJson"/>.</summary>
    string? BriefJson = null) : IRequest<Result<CreateCampaignResponse>>, IRequireTenantRole, IRequireBrandAccess
{
    public TenantMemberRole MinimumRole => TenantMemberRole.Editor;
}
