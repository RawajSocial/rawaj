namespace Rawaj.Application.Common.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken cancellationToken);

    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken);
}
