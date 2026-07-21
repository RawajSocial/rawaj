using FluentValidation;

namespace Rawaj.Application.Features.Users.UpdateMyProfile;

public class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PreferredLanguage).IsInEnum();
        RuleFor(x => x.AvatarUrl).MaximumLength(2048);
    }
}
