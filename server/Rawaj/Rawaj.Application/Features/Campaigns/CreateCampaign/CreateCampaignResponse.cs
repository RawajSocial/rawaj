using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Campaigns.CreateCampaign;

public record CreateCampaignResponse(
    Guid CampaignId,
    Guid BrandProfileId,
    string Name,
    CampaignStatus Status);
