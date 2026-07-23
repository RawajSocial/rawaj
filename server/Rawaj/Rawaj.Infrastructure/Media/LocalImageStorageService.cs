using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure.Media;

public class LocalImageStorageService(IOptions<PublicImageHostingSettings> settings) : ILocalImageStorageService
{
    private readonly PublicImageHostingSettings _settings = settings.Value;

    public async Task<string> SaveAsync(byte[] imageBytes, string contentType, string subfolder, CancellationToken cancellationToken)
    {
        var extension = contentType.Split('/').LastOrDefault()?.Split('+')[0] ?? "jpg";
        var fileName = $"{Guid.NewGuid()}.{extension}";

        var rootPath = Path.Combine(Directory.GetCurrentDirectory(), _settings.MediaRootPath, subfolder);
        Directory.CreateDirectory(rootPath);

        var filePath = Path.Combine(rootPath, fileName);
        await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);

        return $"/media/{subfolder}/{fileName}";
    }

    public Task DeleteAsync(string imageUrl, string subfolder, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(imageUrl);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), _settings.MediaRootPath, subfolder, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
