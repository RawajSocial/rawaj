using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.GenerateContentItem;

public record GenerateContentItemResponse(
    Guid ContentItemId,
    Guid? CampaignId,
    string Content,
    ContentStatus Status);
