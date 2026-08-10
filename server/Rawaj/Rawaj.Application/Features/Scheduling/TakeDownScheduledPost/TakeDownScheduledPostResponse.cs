using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Scheduling.TakeDownScheduledPost;

public record TakeDownScheduledPostResponse(Guid ScheduledPostId, ScheduledPostStatus Status);
