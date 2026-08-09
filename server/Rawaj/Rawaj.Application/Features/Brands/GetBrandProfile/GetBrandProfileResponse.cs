using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.GetBrandProfile;

public record GetBrandProfileResponse(
    Guid BrandProfileId,
    string Name,
    string? Description,
    List<BrandVoice> Tones,
    BrandProfileStatus Status,
    bool IsDefault,
    string? Tagline,
    string? Industry,
    string? TargetAudience,
    List<string> Colors,
    string? LogoUrl,
    string? WebsiteUrl,
    List<string> SupportedLanguages,
    List<string> Keywords,
    string? Location,
    string? Instagram,
    string? BusinessAge,
    DateTime? BusinessEstablishDate,
    string? Stage,
    string? UniqueValue,
    string? PricePositioning,
    string? StorePresence,
    List<string> ExistingPlatforms,
    string? AdmiredBrand1,
    string? AdmiredBrand2,
    string? AdmiredBrand3);
