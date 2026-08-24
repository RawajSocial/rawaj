using FluentValidation;

namespace Rawaj.Application.Features.AiTrial.GenerateTrialImage;

public class GenerateTrialImageCommandValidator : AbstractValidator<GenerateTrialImageCommand>
{
    public GenerateTrialImageCommandValidator()
    {
        RuleFor(x => x.Prompt).NotEmpty().MaximumLength(500);
    }
}
