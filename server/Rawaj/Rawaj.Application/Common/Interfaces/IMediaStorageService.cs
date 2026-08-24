namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Result of a media upload: the publicly fetchable URL plus everything needed to later delete
/// or replace the asset, and metadata the storage provider already computed for free so callers
/// never need a second round-trip just to learn the image's dimensions/size.
/// </summary>
public record MediaUploadResult(
    string Url,
    string PublicId,
    int? WidthPx,
    int? HeightPx,
    long? FileSizeBytes,
    string? MimeType);

/// <summary>
/// Provider-agnostic media storage boundary — the Application layer never references Cloudinary
/// (or any other provider) directly. <see cref="Rawaj.Infrastructure.Media.CloudinaryMediaStorageService"/>
/// is the one implementation shipped today; S3/Azure Blob implementations of this same interface
/// are the intended path if a second provider is ever needed.
/// </summary>
public interface IMediaStorageService
{
    /// <summary>True once the provider has real (non-placeholder) credentials configured.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Uploads an image under <paramref name="folder"/>, with a fresh random public id so
    /// concurrent/repeated uploads never collide or silently overwrite each other.
    /// </summary>
    Task<MediaUploadResult> UploadImageAsync(byte[] imageBytes, string contentType, string folder, CancellationToken cancellationToken);

    /// <summary>
    /// Uploads (or overwrites, if an asset already exists at that id) an image at a caller-chosen,
    /// deterministic public id — for entities that only ever have one current image, like a user's
    /// avatar or a brand's logo, so re-uploading naturally replaces the previous asset with no
    /// separate delete step and no orphaned asset left behind.
    /// </summary>
    Task<MediaUploadResult> UploadImageWithFixedIdAsync(byte[] imageBytes, string contentType, string publicId, CancellationToken cancellationToken);

    Task<MediaUploadResult> UploadVideoAsync(byte[] videoBytes, string contentType, string folder, CancellationToken cancellationToken);

    Task DeleteAsync(string publicId, CancellationToken cancellationToken);

    /// <summary>Not yet wired to any caller — reserved for when a signed/expiring URL is needed
    /// (e.g. private/unapproved assets). Present so the interface won't need a breaking change later.</summary>
    string GetSignedUrl(string publicId, TimeSpan expiry);
}
