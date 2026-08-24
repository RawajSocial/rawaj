using FluentValidation;
using Rawaj.Application.Features.Scheduling.Common;

namespace Rawaj.Application.Features.Scheduling.RescheduleScheduledPost;

public class RescheduleScheduledPostCommandValidator : AbstractValidator<RescheduleScheduledPostCommand>
{
    public RescheduleScheduledPostCommandValidator()
    {
        RuleFor(x => x.ScheduledPostId).NotEmpty();
        RuleFor(x => x.ScheduledAt)
            .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Add(SchedulingWindow.MinimumLead))
            .WithMessage("Scheduled time must be at least 10 minutes in the future (required for native platform scheduling).");
    }
}
