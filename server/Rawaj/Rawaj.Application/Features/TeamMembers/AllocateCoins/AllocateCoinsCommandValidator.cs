using FluentValidation;

namespace Rawaj.Application.Features.TeamMembers.AllocateCoins;

public class AllocateCoinsCommandValidator : AbstractValidator<AllocateCoinsCommand>
{
    public AllocateCoinsCommandValidator()
    {
        RuleFor(x => x.NewAllocation).GreaterThanOrEqualTo(0);
    }
}
