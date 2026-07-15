using FluentValidation;

namespace Rawaj.Application.Features.SocialAccounts.ConnectSocialAccount;

public class ConnectSocialAccountCommandValidator : AbstractValidator<ConnectSocialAccountCommand>
{
    public ConnectSocialAccountCommandValidator()
    {
        RuleFor(x => x.BrandProfileId).NotEmpty();
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.AccountIdExternal).NotEmpty().MaximumLength(255);
        RuleFor(x => x.AccessToken).NotEmpty();
    }
}
