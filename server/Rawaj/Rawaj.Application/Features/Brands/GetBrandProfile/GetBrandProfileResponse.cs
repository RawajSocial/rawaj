using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Brands.GetBrandProfile;

public record GetBrandProfileResponse(
    Guid BrandProfileId,
    string Name,
    string? Description,
    BrandVoice? BrandVoice,
    BrandProfileStatus Status,
    bool IsDefault,
    string? Tagline,
    string? Industry,
    string? TargetAudience,
    List<string> Colors,
    string? LogoUrl,
    string? WebsiteUrl,
    List<string> SupportedLanguages,
    List<string> Keywords);
