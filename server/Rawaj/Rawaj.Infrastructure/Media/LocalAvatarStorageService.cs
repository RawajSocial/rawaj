using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure.Media;

public class LocalAvatarStorageService(IOptions<PublicImageHostingSettings> settings) : IAvatarStorageService
{
    private readonly PublicImageHostingSettings _settings = settings.Value;
    public async Task<string> SaveAvatarAsync(byte[] imageBytes, string contentType, CancellationToken cancellationToken)
    {
        var extension = contentType.Split('/').LastOrDefault() ?? "jpg";
        var fileName = $"{Guid.NewGuid()}.{extension}";

        var rootPath = Path.Combine(Directory.GetCurrentDirectory(), _settings.MediaRootPath, "avatars");
        Directory.CreateDirectory(rootPath);

        var filePath = Path.Combine(rootPath, fileName);
        await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);

        return $"/media/avatars/{fileName}";
    }

    public Task DeleteAvatarAsync(string avatarUrl, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(avatarUrl);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), _settings.MediaRootPath, "avatars", fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
