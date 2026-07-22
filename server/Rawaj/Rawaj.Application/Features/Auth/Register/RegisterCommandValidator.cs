using FluentValidation;
using Rawaj.Application.Common.Validation;

namespace Rawaj.Application.Features.Auth.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(UsernameRules.MinLength, UsernameRules.MaxLength)
            .Matches(UsernameRules.Pattern)
            .WithMessage(UsernameRules.PatternErrorMessage);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PreferredLanguage).IsInEnum();
    }
}
