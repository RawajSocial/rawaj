using FluentValidation;

namespace Rawaj.Application.Features.Content.GenerateContentItem;

public class GenerateContentItemCommandValidator : AbstractValidator<GenerateContentItemCommand>
{
    public GenerateContentItemCommandValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.ContentType).IsInEnum();
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.Language).IsInEnum();
        RuleFor(x => x.Tone).MaximumLength(50);
        RuleFor(x => x.AdditionalInstructions).MaximumLength(500);
    }
}
