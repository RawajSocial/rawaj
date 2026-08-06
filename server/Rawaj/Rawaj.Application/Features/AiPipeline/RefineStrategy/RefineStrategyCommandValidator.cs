using FluentValidation;

namespace Rawaj.Application.Features.AiPipeline.RefineStrategy;

public class RefineStrategyCommandValidator : AbstractValidator<RefineStrategyCommand>
{
    public RefineStrategyCommandValidator()
    {
        RuleFor(x => x.Feedback).NotEmpty().MaximumLength(2000);
    }
}
