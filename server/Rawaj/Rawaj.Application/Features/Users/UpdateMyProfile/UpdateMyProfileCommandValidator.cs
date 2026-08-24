using FluentValidation;
using Rawaj.Application.Common.Validation;

namespace Rawaj.Application.Features.Users.UpdateMyProfile;

public class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(200).When(x => x.FullName is not null);

        RuleFor(x => x.Username)
            .Length(UsernameRules.MinLength, UsernameRules.MaxLength)
            .Matches(UsernameRules.Pattern)
            .WithMessage(UsernameRules.PatternErrorMessage)
            .When(x => x.Username is not null);

        RuleFor(x => x.PreferredLanguage!.Value).IsInEnum().When(x => x.PreferredLanguage.HasValue);
    }
}
