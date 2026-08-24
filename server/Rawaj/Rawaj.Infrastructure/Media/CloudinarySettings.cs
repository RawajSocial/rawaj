namespace Rawaj.Infrastructure.Media;

public class CloudinarySettings
{
    public const string SectionName = "Cloudinary";

    public string? CloudName { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }

    /// <summary>Max upload size accepted before Cloudinary is even called — 15 MB covers every
    /// AI-generated image/avatar/logo this app produces today with headroom, while still catching
    /// a runaway upload before it burns bandwidth/quota.</summary>
    public long MaxFileSizeBytes { get; set; } = 15 * 1024 * 1024;
}
