using System.Net;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure.Media;

/// <summary>
/// The one <see cref="IMediaStorageService"/> implementation shipped today. Application code never
/// references this class or the Cloudinary SDK directly — everything goes through the interface,
/// so swapping in an S3/Azure Blob implementation later needs no change outside this file and DI.
/// </summary>
public class CloudinaryMediaStorageService : IMediaStorageService
{
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif",
    };

    private static readonly HashSet<string> AllowedVideoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/quicktime", "video/webm",
    };

    private readonly CloudinarySettings _settings;
    private readonly Lazy<Cloudinary> _cloudinary;

    public CloudinaryMediaStorageService(IOptions<CloudinarySettings> settings)
    {
        _settings = settings.Value;
        _cloudinary = new Lazy<Cloudinary>(() =>
            new Cloudinary(new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret)));
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.CloudName)
        && !string.IsNullOrWhiteSpace(_settings.ApiKey)
        && !string.IsNullOrWhiteSpace(_settings.ApiSecret);

    public async Task<MediaUploadResult> UploadImageAsync(
        byte[] imageBytes, string contentType, string folder, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        ValidateImage(imageBytes, contentType);

        await using var stream = new MemoryStream(imageBytes);
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(Guid.NewGuid().ToString(), stream),
            Folder = folder,
            UniqueFilename = true,
            Overwrite = false,
        };

        var result = await _cloudinary.Value.UploadAsync(uploadParams, cancellationToken);
        EnsureSucceeded(result);
        return ToResult(result);
    }

    public async Task<MediaUploadResult> UploadImageWithFixedIdAsync(
        byte[] imageBytes, string contentType, string publicId, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        ValidateImage(imageBytes, contentType);

        await using var stream = new MemoryStream(imageBytes);
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(publicId, stream),
            PublicId = publicId,
            Overwrite = true,
            Invalidate = true,
        };

        var result = await _cloudinary.Value.UploadAsync(uploadParams, cancellationToken);
        EnsureSucceeded(result);
        return ToResult(result);
    }

    public async Task<MediaUploadResult> UploadVideoAsync(
        byte[] videoBytes, string contentType, string folder, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        ValidateVideo(videoBytes, contentType);

        await using var stream = new MemoryStream(videoBytes);
        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(Guid.NewGuid().ToString(), stream),
            Folder = folder,
            UniqueFilename = true,
            Overwrite = false,
        };

        var result = await _cloudinary.Value.UploadAsync(uploadParams, cancellationToken);
        EnsureSucceeded(result);
        return ToResult(result);
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var result = await _cloudinary.Value.DestroyAsync(new DeletionParams(publicId));

        // "not found" isn't a failure for a delete — the goal state (asset gone) already holds.
        if (result.Result is not ("ok" or "not found"))
        {
            throw new InvalidOperationException($"Cloudinary deletion of '{publicId}' failed: {result.Result}");
        }
    }

    /// <summary>Reserved for future private/unapproved-asset delivery — not wired to any caller
    /// yet. Cloudinary's own time-limited signed delivery requires the account's "strict transformations"/
    /// authenticated delivery type to be enabled; this only adds a request signature today.</summary>
    public string GetSignedUrl(string publicId, TimeSpan expiry)
    {
        EnsureConfigured();
        return _cloudinary.Value.Api.UrlImgUp.Signed(true).BuildUrl(publicId);
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Cloudinary is not configured (Cloudinary:CloudName/ApiKey/ApiSecret) — set real credentials before uploading media.");
        }
    }

    private void ValidateImage(byte[] imageBytes, string contentType)
    {
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException($"Unsupported image content type: '{contentType}'.");
        }
        if (imageBytes.LongLength > _settings.MaxFileSizeBytes)
        {
            throw new InvalidOperationException("Image exceeds the maximum allowed upload size.");
        }
    }

    private void ValidateVideo(byte[] videoBytes, string contentType)
    {
        if (!AllowedVideoContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException($"Unsupported video content type: '{contentType}'.");
        }
        if (videoBytes.LongLength > _settings.MaxFileSizeBytes)
        {
            throw new InvalidOperationException("Video exceeds the maximum allowed upload size.");
        }
    }

    private static void EnsureSucceeded(BaseResult result)
    {
        if (result.Error is not null || result.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error?.Message ?? result.StatusCode.ToString()}");
        }
    }

    private static MediaUploadResult ToResult(ImageUploadResult result) => new(
        result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? throw new InvalidOperationException("Cloudinary upload returned no URL."),
        result.PublicId,
        result.Width > 0 ? result.Width : null,
        result.Height > 0 ? result.Height : null,
        result.Bytes > 0 ? result.Bytes : null,
        result.Format is null ? null : $"image/{result.Format}");

    private static MediaUploadResult ToResult(VideoUploadResult result) => new(
        result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? throw new InvalidOperationException("Cloudinary upload returned no URL."),
        result.PublicId,
        result.Width > 0 ? result.Width : null,
        result.Height > 0 ? result.Height : null,
        result.Bytes > 0 ? result.Bytes : null,
        result.Format is null ? null : $"video/{result.Format}");
}
