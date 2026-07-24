using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.RescheduleScheduledPost;

public record RescheduleScheduledPostResponse(Guid ScheduledPostId, DateTime ScheduledAt, ScheduledPostStatus Status);
