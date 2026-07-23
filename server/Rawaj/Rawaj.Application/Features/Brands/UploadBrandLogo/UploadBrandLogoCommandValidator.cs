using FluentValidation;

namespace Rawaj.Application.Features.Brands.UploadBrandLogo;

public class UploadBrandLogoCommandValidator : AbstractValidator<UploadBrandLogoCommand>
{
    private const int MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".svg"];
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp", "image/svg+xml"];

    public UploadBrandLogoCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Logo file is required.")
            .Must(content => content.Length <= MaxFileSizeBytes).WithMessage("Logo file must not exceed 5 MB.");

        RuleFor(x => x.ContentType)
            .Must(contentType => AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
            .WithMessage("Logo must be a JPEG, PNG, WEBP, or SVG image.");

        RuleFor(x => x.FileName)
            .Must(fileName => AllowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
            .WithMessage("Logo must have a .jpg, .jpeg, .png, .webp, or .svg extension.");
    }
}
