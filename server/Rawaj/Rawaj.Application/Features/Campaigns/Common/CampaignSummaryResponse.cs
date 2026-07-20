using Rawaj.Domain.Entities.Campaigns;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.Common;

public record CampaignSummaryResponse(
    Guid Id,
    Guid BrandProfileId,
    string? Name,
    string CampaignType,
    CampaignStatus Status,
    List<string> TargetPlatforms,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsOnboardingComplete,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static CampaignSummaryResponse FromEntity(MarketingCampaign c) => new(c.Id, c.BrandProfileId, c.Name, c.CampaignType, c.Status, c.TargetPlatforms, c.StartDate, c.EndDate, c.IsOnboardingComplete, c.CreatedAt, c.UpdatedAt);
}
