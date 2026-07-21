using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.GetScheduledPosts;

public record ScheduledPostSummary(
    Guid ScheduledPostId,
    Guid ContentItemId,
    SocialPlatform Platform,
    string AccountName,
    DateTime ScheduledAt,
    ScheduledPostStatus Status,
    DateTime? PublishedAt,
    string? ErrorMessage);
