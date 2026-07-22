using FluentValidation;

namespace Rawaj.Application.Features.Users.UpdateMyAvatar;

public class UpdateMyAvatarCommandValidator : AbstractValidator<UpdateMyAvatarCommand>
{
    private const int MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public UpdateMyAvatarCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Avatar file is required.")
            .Must(content => content.Length <= MaxFileSizeBytes).WithMessage("Avatar file must not exceed 5 MB.");

        RuleFor(x => x.ContentType)
            .Must(contentType => AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
            .WithMessage("Avatar must be a JPEG, PNG, or WEBP image.");

        RuleFor(x => x.FileName)
            .Must(fileName => AllowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
            .WithMessage("Avatar must have a .jpg, .jpeg, .png, or .webp extension.");
    }
}
