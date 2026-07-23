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
        // Mirrors the ASP.NET Identity password policy configured in
        // Rawaj.Persistence/DependencyInjection.cs (RequiredLength=8, plus Identity's default
        // RequireDigit/RequireLowercase/RequireUppercase/RequireNonAlphanumeric) so a bad password
        // is caught here as a clean field error instead of leaking through as Identity's raw,
        // unstructured (and English-only) CreateUserAsync error.
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
