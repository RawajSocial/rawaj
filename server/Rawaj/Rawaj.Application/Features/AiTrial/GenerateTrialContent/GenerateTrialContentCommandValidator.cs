using FluentValidation;

namespace Rawaj.Application.Features.AiTrial.GenerateTrialContent;

public class GenerateTrialContentCommandValidator : AbstractValidator<GenerateTrialContentCommand>
{
    public GenerateTrialContentCommandValidator()
    {
        RuleFor(x => x.Prompt).NotEmpty().MaximumLength(500);
    }
}
