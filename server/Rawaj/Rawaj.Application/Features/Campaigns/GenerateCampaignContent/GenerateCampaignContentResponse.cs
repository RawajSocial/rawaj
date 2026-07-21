namespace Rawaj.Application.Features.Campaigns.GenerateCampaignContent;

public record GenerateCampaignContentResponse(
    Guid CampaignId, int GeneratedCount, int ImagesGenerated, int ImagesSkippedForCredits);
