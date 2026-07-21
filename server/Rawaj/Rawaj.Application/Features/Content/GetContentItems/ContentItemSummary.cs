using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GetContentItems;

public record ContentItemSummary(
    Guid ContentItemId,
    ContentType ContentType,
    SocialPlatform Platform,
    Language Language,
    string Content,
    ContentStatus Status,
    DateTime CreatedAt,
    DateTime? SuggestedPostAt,
    Guid? VisualAssetId,
    string? ImageUrl);
