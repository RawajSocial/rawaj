using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.SchedulePost;

public record SchedulePostResponse(Guid ScheduledPostId, DateTime ScheduledAt, ScheduledPostStatus Status, string? ExternalPostId);
