using FluentValidation;
using Rawaj.Application.Features.Scheduling.Common;

namespace Rawaj.Application.Features.Scheduling.SchedulePost;

public class SchedulePostCommandValidator : AbstractValidator<SchedulePostCommand>
{
    public SchedulePostCommandValidator()
    {
        RuleFor(x => x.ContentItemId).NotEmpty();
        RuleFor(x => x.SocialAccountId).NotEmpty();
        RuleFor(x => x.ScheduledAt)
            .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Add(SchedulingWindow.MinimumLead))
            .When(x => !x.PublishNow)
            .WithMessage("Scheduled time must be at least 10 minutes in the future (required for native platform scheduling).");
    }
}
