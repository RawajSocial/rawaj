using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GetVisualAssets;

public record VisualAssetSummary(
    Guid VisualAssetId,
    Guid? ContentItemId,
    VisualAssetType Type,
    string FileUrl,
    bool IsApproved,
    DateTime CreatedAt);
