using FluentValidation;

namespace Rawaj.Application.Features.Content.GenerateVisualAsset;

public class GenerateVisualAssetCommandValidator : AbstractValidator<GenerateVisualAssetCommand>
{
    public GenerateVisualAssetCommandValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Prompt).NotEmpty().MaximumLength(500);
    }
}
