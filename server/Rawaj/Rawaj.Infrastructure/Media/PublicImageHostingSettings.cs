namespace Rawaj.Infrastructure.Media;

public class PublicImageHostingSettings
{
    public const string SectionName = "PublicImageHosting";

    /// <summary>
    /// The externally reachable base URL for this API (e.g. an ngrok tunnel or a deployed
    /// domain). Left empty in local dev, where Meta's servers cannot reach localhost anyway -
    /// IsConfigured reports false so callers can fail honestly instead of building a dead URL.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    public string MediaRootPath { get; set; } = "wwwroot/media";
}
