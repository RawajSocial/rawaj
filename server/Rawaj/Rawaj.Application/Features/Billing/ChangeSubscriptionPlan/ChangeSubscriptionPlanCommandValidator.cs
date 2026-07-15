using FluentValidation;

namespace Rawaj.Application.Features.Billing.ChangeSubscriptionPlan;

public class ChangeSubscriptionPlanCommandValidator : AbstractValidator<ChangeSubscriptionPlanCommand>
{
    public ChangeSubscriptionPlanCommandValidator()
    {
        RuleFor(x => x.SubscriptionPlanId).NotEmpty();
    }
}
