using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure.Media;

public class LocalFilePublicImageHostingService(IOptions<PublicImageHostingSettings> settings) : IPublicImageHostingService
{
    private readonly PublicImageHostingSettings _settings = settings.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.PublicBaseUrl);

    public async Task<string> HostImageAsync(byte[] imageBytes, string contentType, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("PublicImageHosting:PublicBaseUrl is not configured, so generated images have no publicly fetchable URL.");
        }

        var extension = contentType.Split('/').LastOrDefault() ?? "jpg";
        var fileName = $"{Guid.NewGuid()}.{extension}";

        var rootPath = Path.Combine(Directory.GetCurrentDirectory(), _settings.MediaRootPath);
        Directory.CreateDirectory(rootPath);

        var filePath = Path.Combine(rootPath, fileName);
        await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);

        return $"{_settings.PublicBaseUrl!.TrimEnd('/')}/media/{fileName}";
    }
}
