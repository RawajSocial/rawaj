using FluentValidation;

namespace Rawaj.Application.Features.Content.GenerateVisualAsset;

public class GenerateVisualAssetCommandValidator : AbstractValidator<GenerateVisualAssetCommand>
{
    public GenerateVisualAssetCommandValidator()
    {
        RuleFor(x => x.BrandProfileId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Prompt).NotEmpty().MaximumLength(500);
    }
}
