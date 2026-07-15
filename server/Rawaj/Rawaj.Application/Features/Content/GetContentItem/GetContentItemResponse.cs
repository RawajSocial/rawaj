using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GetContentItem;

public record GetContentItemResponse(
    Guid ContentItemId,
    Guid? CampaignId,
    ContentType ContentType,
    SocialPlatform Platform,
    Language Language,
    string? Title,
    string Content,
    List<string> Hashtags,
    string? Cta,
    string? Tone,
    ContentStatus Status,
    Guid? ReviewedBy,
    DateTime? ReviewedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);
