using Rawaj.Domain.Enums;

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
    public string? Location { get; set; }
    public bool IsDefault { get; set; }

    /// <summary>Up to 5 tone words describing how the brand should sound — replaces the old
    /// single-value <c>BrandVoice</c> column (see migration <c>MoveBrandVoiceIntoBrandInfoTones</c>).
    /// Moved here (JSON-serialized) rather than kept as its own typed column since it's just one more
    /// brand-identity fact alongside the others already stored this way.</summary>
    public List<BrandVoice> Tones { get; set; } = [];

    public string? Instagram { get; set; }
    /// <summary>Bucketed range (e.g. "1 - 3 سنوات") — kept distinct from
    /// <see cref="BusinessEstablishDate"/>, which is an exact date; the two answer different
    /// questions (a rough sense of maturity vs. a precise founding date) and either can be present
    /// without the other.</summary>
    public string? BusinessAge { get; set; }
    public DateTime? BusinessEstablishDate { get; set; }
    public string? Stage { get; set; }
    public string? UniqueValue { get; set; }
    public string? PricePositioning { get; set; }
    public string? StorePresence { get; set; }
    public List<string> ExistingPlatforms { get; set; } = [];
    public string? AdmiredBrand1 { get; set; }
    public string? AdmiredBrand2 { get; set; }
    public string? AdmiredBrand3 { get; set; }
}
