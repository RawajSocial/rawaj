using FluentValidation;

namespace Rawaj.Application.Features.SocialAccounts.GetAuthorizationUrl;

public class GetAuthorizationUrlCommandValidator : AbstractValidator<GetAuthorizationUrlCommand>
{
    public GetAuthorizationUrlCommandValidator()
    {
        RuleFor(x => x.BrandProfileId).NotEmpty();
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.RedirectUri).NotEmpty().Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))
            .WithMessage("RedirectUri must be a valid absolute URL.");
    }
}
