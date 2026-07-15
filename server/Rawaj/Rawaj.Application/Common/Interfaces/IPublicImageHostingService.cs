namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Publishers that require a fetchable image URL (Instagram's Content Publishing API takes
/// image_url, not raw bytes) go through this to get one. The local disk implementation only
/// produces a URL that's actually reachable by Meta's servers once PublicBaseUrl points at a
/// publicly accessible host - on localhost the URL is well-formed but unreachable from the
/// internet, so Instagram image publishing can only be verified once deployed or tunneled.
/// </summary>
public interface IPublicImageHostingService
{
    bool IsConfigured { get; }

    Task<string> HostImageAsync(byte[] imageBytes, string contentType, CancellationToken cancellationToken);
}
