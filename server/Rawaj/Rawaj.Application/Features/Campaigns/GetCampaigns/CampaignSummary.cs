using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.GetCampaigns;

public record CampaignSummary(
    Guid CampaignId,
    Guid BrandProfileId,
    string Name,
    CampaignStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateTime CreatedAt);
