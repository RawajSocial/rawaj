using FluentValidation;
using Rawaj.Application.Common.Validation;

namespace Rawaj.Application.Features.TeamMembers.AcceptInvitationAndRegister;

public class AcceptInvitationAndRegisterCommandValidator : AbstractValidator<AcceptInvitationAndRegisterCommand>
{
    public AcceptInvitationAndRegisterCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();

        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(UsernameRules.MinLength, UsernameRules.MaxLength)
            .Matches(UsernameRules.Pattern)
            .WithMessage(UsernameRules.PatternErrorMessage);

        // Mirrors RegisterCommandValidator's password policy — see that file for why.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PreferredLanguage).IsInEnum();
    }
}
