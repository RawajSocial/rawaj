using FluentValidation;

namespace Rawaj.Application.Features.Users.VerifyEmailOtp;

public class VerifyEmailOtpCommandValidator : AbstractValidator<VerifyEmailOtpCommand>
{
    public VerifyEmailOtpCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$").WithMessage("Code must be a 6-digit number.");
    }
}
