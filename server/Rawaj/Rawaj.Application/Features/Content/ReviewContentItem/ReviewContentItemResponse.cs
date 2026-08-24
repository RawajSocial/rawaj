using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.ReviewContentItem;

public record ReviewContentItemResponse(Guid ContentItemId, ContentStatus Status, DateTime ReviewedAt);
