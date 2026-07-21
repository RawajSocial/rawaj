using FluentValidation;

namespace Rawaj.Application.Features.Scheduling.SchedulePost;

public class SchedulePostCommandValidator : AbstractValidator<SchedulePostCommand>
{
    public SchedulePostCommandValidator()
    {
        RuleFor(x => x.ContentItemId).NotEmpty();
        RuleFor(x => x.SocialAccountId).NotEmpty();
        RuleFor(x => x.ScheduledAt)
            .GreaterThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(10))
            .WithMessage("Scheduled time must be at least 10 minutes in the future (required for native platform scheduling).");
    }
}
