using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure.Storage;

public class LocalFileStorageService(IOptions<StorageSettings> options) : IStorageService
{
    private readonly StorageSettings _settings = options.Value;

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid()}{extension}";

        var directory = Path.Combine(Directory.GetCurrentDirectory(), _settings.BasePath, folder);
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, storedFileName);

        content.Position = 0;
        await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var relativeUrl = string.Join('/', folder.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries).Append(storedFileName));
        return $"{_settings.PublicBaseUrl.TrimEnd('/')}/uploads/{relativeUrl}";
    }

    public Task DeleteAsync(string fileUrl, CancellationToken cancellationToken)
    {
        var uploadsMarker = "/uploads/";
        var markerIndex = fileUrl.IndexOf(uploadsMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return Task.CompletedTask;
        }

        var relativePath = fileUrl[(markerIndex + uploadsMarker.Length)..].Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), _settings.BasePath, relativePath);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
