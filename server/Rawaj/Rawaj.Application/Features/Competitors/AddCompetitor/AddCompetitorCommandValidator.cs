using FluentValidation;

namespace Rawaj.Application.Features.Competitors.AddCompetitor;

public class AddCompetitorCommandValidator : AbstractValidator<AddCompetitorCommand>
{
    public AddCompetitorCommandValidator()
    {
        RuleFor(x => x.BrandProfileId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).Must(BeAValidUrl).When(x => !string.IsNullOrWhiteSpace(x.Url))
            .WithMessage("Url must be a valid absolute URL.");
    }

    private static bool BeAValidUrl(string? url) => Uri.TryCreate(url, UriKind.Absolute, out _);
}
