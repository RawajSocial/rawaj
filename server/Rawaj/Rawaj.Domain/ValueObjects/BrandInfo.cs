namespace Rawaj.Domain.ValueObjects;

public class BrandInfo
{
    public string? Tagline { get; set; }
    public string? Industry { get; set; }
    public string? TargetAudience { get; set; }
    public List<string> Colors { get; set; } = [];
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public List<string> SupportedLanguages { get; set; } = [];
    public List<string> Keywords { get; set; } = [];
    public bool IsDefault { get; set; }
}
