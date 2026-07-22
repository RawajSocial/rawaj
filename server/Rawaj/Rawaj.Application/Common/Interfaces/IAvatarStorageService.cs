namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Stores a user-uploaded avatar image on local disk (under wwwroot, same-origin static files) and
/// returns a relative URL - unlike <see cref="IPublicImageHostingService"/>, this never requires an
/// externally reachable PublicBaseUrl, since the SPA and API share an origin for this purpose.
/// </summary>
public interface IAvatarStorageService
{
    Task<string> SaveAvatarAsync(byte[] imageBytes, string contentType, CancellationToken cancellationToken);

    Task DeleteAvatarAsync(string avatarUrl, CancellationToken cancellationToken);
}
