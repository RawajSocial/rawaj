namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Stores a user-uploaded image on local disk (under wwwroot, same-origin static files) and
/// returns a relative URL - unlike <see cref="IPublicImageHostingService"/>, this never requires an
/// externally reachable PublicBaseUrl, since the SPA and API share an origin for this purpose.
/// The <c>subfolder</c> selects the media bucket (e.g. "avatars", "brandprofiles/logourls").
/// </summary>
public interface ILocalImageStorageService
{
    Task<string> SaveAsync(byte[] imageBytes, string contentType, string subfolder, CancellationToken cancellationToken);

    Task DeleteAsync(string imageUrl, string subfolder, CancellationToken cancellationToken);
}
