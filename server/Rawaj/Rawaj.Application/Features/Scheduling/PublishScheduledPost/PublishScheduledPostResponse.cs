using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.PublishScheduledPost;

public record PublishScheduledPostResponse(Guid ScheduledPostId, ScheduledPostStatus Status, string? ExternalPostId);
