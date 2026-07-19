using FluentValidation;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Options;

namespace Rawaj.Application.Features.Tenants.UploadBrandImage;

public class UploadBrandImageCommandValidator : AbstractValidator<UploadBrandImageCommand>
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".svg"];
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp", "image/svg+xml"];

    public UploadBrandImageCommandValidator(IOptions<BrandImageUploadOptions> options)
    {
        var maxSizeBytes = options.Value.MaxSizeBytes;

        RuleFor(x => x.BrandProfileId).NotEmpty();

        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(HaveAnAllowedExtension)
            .WithMessage($"Only {string.Join(", ", AllowedExtensions)} images are allowed.");

        RuleFor(x => x.ContentType)
            .Must(BeAnAllowedContentType)
            .WithMessage($"File content type must be one of: {string.Join(", ", AllowedContentTypes)}.");

        RuleFor(x => x.Length)
            .InclusiveBetween(1, maxSizeBytes)
            .WithMessage($"Image must not exceed {maxSizeBytes / (1024 * 1024)}MB.");

        RuleFor(x => x)
            .MustAsync((command, cancellationToken) => HasValidSignatureAsync(command.Content, command.FileName, cancellationToken))
            .WithMessage("The file content does not match its declared image type.")
            .WithName("Content");
    }

    private static bool HaveAnAllowedExtension(string fileName) =>
        AllowedExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);

    private static bool BeAnAllowedContentType(string? contentType) =>
        contentType is not null && AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);

    private static async Task<bool> HasValidSignatureAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var buffer = new byte[256];

        content.Position = 0;
        var bytesRead = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        content.Position = 0;

        return extension switch
        {
            ".png" => bytesRead >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47,
            ".jpg" or ".jpeg" => bytesRead >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
            ".webp" => bytesRead >= 12
                && buffer[0] == 'R' && buffer[1] == 'I' && buffer[2] == 'F' && buffer[3] == 'F'
                && buffer[8] == 'W' && buffer[9] == 'E' && buffer[10] == 'B' && buffer[11] == 'P',
            ".svg" => LooksLikeSvg(buffer, bytesRead),
            _ => false,
        };
    }

    private static bool LooksLikeSvg(byte[] buffer, int bytesRead)
    {
        var text = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
        return text.Contains("<svg", StringComparison.OrdinalIgnoreCase)
            || text.Contains("<?xml", StringComparison.OrdinalIgnoreCase);
    }
}
