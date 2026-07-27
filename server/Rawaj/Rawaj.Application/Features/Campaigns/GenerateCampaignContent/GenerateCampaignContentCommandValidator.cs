using FluentValidation;

namespace Rawaj.Application.Features.Campaigns.GenerateCampaignContent;

public class GenerateCampaignContentCommandValidator : AbstractValidator<GenerateCampaignContentCommand>
{
    public GenerateCampaignContentCommandValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();

        // Explicit message rather than FluentValidation's default: the client translates these
        // exact strings into Arabic (see api-error.util.ts's KNOWN_MESSAGE_TRANSLATIONS), and the
        // default text interpolates the property name, which would never match a lookup.
        RuleFor(x => x.PostCount)
            .InclusiveBetween(1, 15)
            .WithMessage("Post count must be between 1 and 15.");
    }
}
