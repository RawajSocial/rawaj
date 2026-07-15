using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Content.RegenerateContentItem;

public record RegenerateContentItemResponse(Guid ContentItemId, string Content, ContentStatus Status, int RevisionNumber);
