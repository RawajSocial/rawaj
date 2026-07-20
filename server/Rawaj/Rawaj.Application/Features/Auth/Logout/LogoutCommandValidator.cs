using FluentValidation;

namespace Rawaj.Application.Features.Auth.Logout;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
