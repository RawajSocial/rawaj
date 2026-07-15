namespace Rawaj.Application.Features.Content.GenerateVisualAsset;

public record GenerateVisualAssetResponse(Guid VisualAssetId, Guid CampaignId, string FileUrl);
