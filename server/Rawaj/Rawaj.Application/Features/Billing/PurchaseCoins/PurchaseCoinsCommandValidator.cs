using FluentValidation;

namespace Rawaj.Application.Features.Billing.PurchaseCoins;

public class PurchaseCoinsCommandValidator : AbstractValidator<PurchaseCoinsCommand>
{
    public PurchaseCoinsCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.CoinPackageId.HasValue ^ x.CustomCoins.HasValue)
            .WithMessage("Provide either a coin package or a custom coin amount, not both or neither.");

        RuleFor(x => x.CustomCoins)
            .GreaterThan(0)
            .When(x => x.CustomCoins.HasValue)
            .WithMessage("Custom coin amount must be greater than zero.");
    }
}
