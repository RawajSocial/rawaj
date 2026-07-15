using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.CancelScheduledPost;

public record CancelScheduledPostResponse(Guid ScheduledPostId, ScheduledPostStatus Status);
